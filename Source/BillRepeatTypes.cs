using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using TD.Utilities;

namespace Everybody_Gets_One
{
	[DefOf]
	public static class RepeatModeDefOf
	{
		public static BillRepeatModeDef TD_ColonistCount;
		public static BillRepeatModeDef TD_XPerColonist;
		public static BillRepeatModeDef TD_WithSurplusIng;
	}

	public static class Extensions
	{
		public static int TargetCount(this Bill_Production bill)
		{
			return 
				bill.repeatMode == RepeatModeDefOf.TD_ColonistCount ? bill.Map.mapPawns.PermanentColonistCount() + bill.targetCount :
				bill.repeatMode == RepeatModeDefOf.TD_XPerColonist ? bill.Map.mapPawns.PermanentColonistCount() * bill.targetCount : bill.targetCount;
		}
		public static int UnpauseWhenYouHave(this Bill_Production bill)
		{
			return 
				bill.repeatMode == RepeatModeDefOf.TD_ColonistCount ? bill.Map.mapPawns.PermanentColonistCount() + bill.unpauseWhenYouHave :
				bill.repeatMode == RepeatModeDefOf.TD_XPerColonist ? bill.Map.mapPawns.PermanentColonistCount() * bill.unpauseWhenYouHave : bill.unpauseWhenYouHave;
		}
		public static int IngredientCount(this Bill_Production bill)
		{
			if(bill.recipe.ingredients.First() is IngredientCount ic && ic.IsFixedIngredient)
				return bill.Map.resourceCounter.GetCount(bill.recipe.ingredients.First().FixedIngredient);
			return bill.Map.resourceCounter.GetCountFor(bill.ingredientFilter);
		}

		public static int GetCountFor(this ResourceCounter res, ThingFilter filter)
		{
			return filter.AllowedThingDefs.Sum(def => res.GetCount(def));
		}
	}

	[HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.RepeatInfoText), MethodType.Getter)]
	class RepeatInfoText_Patch
	{
		//public string RepeatInfoText
		public static bool Prefix(ref string __result, Bill_Production __instance)
		{
			if (__instance.repeatMode == RepeatModeDefOf.TD_ColonistCount)
			{
				__result = $"{__instance.recipe.WorkerCounter.CountProducts(__instance)}/({__instance.Map.mapPawns.PermanentColonistCount()}+{__instance.targetCount})";
				return false;
			}
			if (__instance.repeatMode == RepeatModeDefOf.TD_XPerColonist)
			{
				__result = $"{__instance.recipe.WorkerCounter.CountProducts(__instance)}/{__instance.Map.mapPawns.PermanentColonistCount() * __instance.targetCount} ({__instance.targetCount})";
				return false;
			}
			if (__instance.repeatMode == RepeatModeDefOf.TD_WithSurplusIng)
			{
				__result = $"{__instance.IngredientCount()} > {__instance.targetCount}";
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.ShouldDoNow))]
	class ShouldDoNow_Patch
	{
		//public override bool ShouldDoNow()
		public static bool Prefix(ref bool __result, Bill_Production __instance)
		{
			if (__instance.repeatMode == RepeatModeDefOf.TD_ColonistCount || 
				__instance.repeatMode == RepeatModeDefOf.TD_XPerColonist)
			{
				if (__instance.suspended)
				{
					__result = false;
					return false;
				}

				//Same as TargetCount mode but with .TargetCount instead of .targetCount
				int products = __instance.recipe.WorkerCounter.CountProducts(__instance);
				int targetCount = __instance.TargetCount();
				if (__instance.pauseWhenSatisfied && products >= targetCount)
				{
					__instance.paused = true;
				}
				if (products <= __instance.UnpauseWhenYouHave() || !__instance.pauseWhenSatisfied)
				{
					__instance.paused = false;
				}
				__result = !__instance.paused && products < targetCount;
				return false;
			}
			if (__instance.repeatMode == RepeatModeDefOf.TD_WithSurplusIng)
			{
				if (__instance.suspended)
				{
					__result = false;
				}

				//Should finish if there's unfinished thing, that might've just made the ingrdient count drop below targetCount
				else if (__instance is Bill_ProductionWithUft bill_UFT && bill_UFT.BoundUft != null)
				{
					__result = true;
				}
				else
					__result = __instance.IngredientCount() > __instance.targetCount;

				return false;
			}
			return true;
		}
	}
	
	[HarmonyPatch(typeof(Bill_Production), "DoConfigInterface")]
	class DoConfigInterface_Patch
	{
		//protected override void DoConfigInterface(Rect baseRect, Color baseColor)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			instructions = OrColonistCount_Transpiler.Transpiler(instructions);

			//For One-Per + X, don't set a minumum
			FieldInfo repeatModeInfo = AccessTools.Field(typeof(Bill_Production), nameof(Bill_Production.repeatMode));
			//public static int Max(int a, int b)
			MethodInfo MaxInfo = AccessTools.Method(typeof(UnityEngine.Mathf), nameof(UnityEngine.Mathf.Max), new Type[] { typeof(int), typeof(int) });

			foreach(var i in instructions)
			{
				if(i.Calls(MaxInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//this.
					yield return new CodeInstruction(OpCodes.Ldfld, repeatModeInfo);//this.repeatMode

					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DoConfigInterface_Patch), nameof(ActuallyMax)));
				}
				else
					yield return i;
			}
		}

		public static int ActuallyMax(int a, int b, BillRepeatModeDef def)
		{
			if (def == RepeatModeDefOf.TD_ColonistCount) return b;

			return UnityEngine.Mathf.Max(a, b);
		}
	}

	[HarmonyPatch(typeof(Bill_Production), "CanUnpause")]
	class CanUnpause_Patch
	{
		//private bool CanUnpause()
		public static bool Prefix(ref bool __result, Bill_Production __instance)
		{
			if (__instance.repeatMode == RepeatModeDefOf.TD_ColonistCount ||
				__instance.repeatMode == RepeatModeDefOf.TD_XPerColonist)
			{
				__result = __instance.paused && __instance.pauseWhenSatisfied && __instance.recipe.WorkerCounter.CountProducts(__instance) < __instance.TargetCount();
				return false;
			}
			return true;
		}
	}


	[HarmonyPatch(typeof(BillRepeatModeUtility), nameof(BillRepeatModeUtility.MakeConfigFloatMenu))]
	public static class MakeConfigFloatMenu_Patch
	{
		//public static void MakeConfigFloatMenu(Bill_Production bill)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			ConstructorInfo ListCtorInfo = AccessTools.Constructor(typeof(List<>).MakeGenericType(typeof(FloatMenuOption)), new Type[] { });

			foreach (CodeInstruction i in instructions)
			{
				yield return i;
				if (i.opcode == OpCodes.Newobj && i.operand.Equals(ListCtorInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//Bill_Production bill
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MakeConfigFloatMenu_Patch), nameof(InsertMode)));
				}
			}
		}

		public static List<FloatMenuOption> InsertMode(List<FloatMenuOption> options, Bill_Production bill)
		{
			FloatMenuOption item = new FloatMenuOption(RepeatModeDefOf.TD_ColonistCount.LabelCap, delegate
			{
				if (!bill.recipe.WorkerCounter.CanCountProducts(bill))
				{
					Messages.Message("RecipeCannotHaveTargetCount".Translate(), MessageTypeDefOf.RejectInput, false);
				}
				else
				{
					bill.repeatMode = RepeatModeDefOf.TD_ColonistCount;
				}
			});
			options.Add(item);

			item = new FloatMenuOption(RepeatModeDefOf.TD_XPerColonist.LabelCap, delegate
			{
				if (!bill.recipe.WorkerCounter.CanCountProducts(bill))
				{
					Messages.Message("RecipeCannotHaveTargetCount".Translate(), MessageTypeDefOf.RejectInput, false);
				}
				else
				{
					bill.repeatMode = RepeatModeDefOf.TD_XPerColonist;
				}
			});
			options.Add(item);

			item = new FloatMenuOption(RepeatModeDefOf.TD_WithSurplusIng.LabelCap, delegate
			{
				if (bill.recipe.ingredients.Count() != 1)
				{
					Messages.Message("TD.RecipeCannotHaveSurplus".Translate(), MessageTypeDefOf.RejectInput, false);
				}
				else
				{
					bill.repeatMode = RepeatModeDefOf.TD_WithSurplusIng;
				}
			});
			options.Add(item);

			return options;//pass-thru
		}
	}

	[HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
	public static class Dialog_BillConfig_Patch
	{
		//public override void DoWindowContents(Rect inRect)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			instructions = OrColonistCount_Transpiler.Transpiler(instructions);
			instructions = Transpilers.MethodReplacer(instructions,
				AccessTools.Method(typeof(RecipeWorkerCounter), nameof(RecipeWorkerCounter.CountProducts)),
				AccessTools.Method(typeof(Dialog_BillConfig_Patch), nameof(CountTracked)));

			//IL_0203: ldfld int32 RimWorld.Bill_Production::targetCount
			FieldInfo targetCountInfo = AccessTools.Field(typeof(Bill_Production), nameof(Bill_Production.targetCount));
			FieldInfo unpauseWhenYouHaveInfo = AccessTools.Field(typeof(Bill_Production), nameof(Bill_Production.unpauseWhenYouHave));

			int todoTCByValue = 1;//first 2 counts of targetCount is displayed count, not X, so use Extensions.TargetCount instead to count colonists
			int todoTCByRef = 1;//but the second is actually ldflda which means the replacement function can't be used and TargetCountRef needs to be created AUGH.
			int todoUnpause = 1; //first ldflda unpauseWhenYouHave is the displayed count
			foreach (CodeInstruction i in instructions)
			{
				if (i.LoadsField(targetCountInfo) && todoTCByValue-- > 0)
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Extensions), nameof(Extensions.TargetCount)));
				}
				else if (i.LoadsField(targetCountInfo, true) && todoTCByRef-- > 0)
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Dialog_BillConfig_Patch), nameof(Dialog_BillConfig_Patch.TargetCountRef)));
				}
				else if (i.LoadsField(unpauseWhenYouHaveInfo, true) && todoUnpause-- > 0)
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Dialog_BillConfig_Patch), nameof(Dialog_BillConfig_Patch.UnpauseWhenYouHaveRef)));
				}
				else
					yield return i;
			}
		}

		//public virtual int CountProducts(Bill_Production bill)
		public static int CountTracked(RecipeWorkerCounter counter, Bill_Production bill)
		{
			return bill.repeatMode == RepeatModeDefOf.TD_WithSurplusIng ? bill.IngredientCount() : counter.CountProducts(bill);
		}

		public static int returnValue;
		public static ref int TargetCountRef(this Bill_Production bill)
		{
			returnValue = bill.TargetCount();
			return ref returnValue;
		}
		public static ref int UnpauseWhenYouHaveRef(this Bill_Production bill)
		{
			returnValue = bill.UnpauseWhenYouHave();
			return ref returnValue;
		}
	}
	public static class OrColonistCount_Transpiler
	{
		//Once upon a time only the first method should be replaced
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
			TranspilerC(instructions, 9999);
		public static IEnumerable<CodeInstruction> TranspilerC(IEnumerable<CodeInstruction> instructions, int count) =>
			TranspilerB(instructions, 9999, false);
		public static IEnumerable<CodeInstruction> TranspilerB(IEnumerable<CodeInstruction> instructions, int count, bool noSurplus)
		{
			FieldInfo repeatModeInfo = AccessTools.Field(typeof(Bill_Production), nameof(Bill_Production.repeatMode));
			FieldInfo TargetCountInfo = AccessTools.Field(typeof(BillRepeatModeDefOf), nameof(BillRepeatModeDefOf.TargetCount));

			int done = 0;
			List <CodeInstruction> instList = instructions.ToList();
			for (int i = 0; i < instList.Count; i++)
			{
				CodeInstruction inst = instList[i];

				//IL_04b4: ldarg.0      // this
				//IL_04b5: ldfld class RimWorld.Bill_Production RimWorld.Dialog_BillConfig::bill
				//IL_04ba: ldfld        class RimWorld.BillRepeatModeDef RimWorld.Bill_Production::repeatMode
				//IL_04bf: ldsfld       class RimWorld.BillRepeatModeDef RimWorld.BillRepeatModeDefOf::TargetCount
				//IL_04c4: bne.un IL_059d

				if (done < count &&
					(inst.opcode == OpCodes.Bne_Un || inst.opcode == OpCodes.Beq || inst.opcode == OpCodes.Bne_Un_S || inst.opcode == OpCodes.Beq_S) &&
					instList[i - 2].LoadsField(repeatModeInfo) &&
					instList[i - 1].LoadsField(TargetCountInfo))
				{
					done++;

					//To distinguish between TargetCount / PauseWhenSatisfied, find what the next string loaded is
					string context = "";
					for(int k = i + 1;k< instList.Count;k++)
					{
						if (instList[k].opcode == OpCodes.Ldstr)
						{
							context = instList[k].operand as string;
							break;
						}
					}

					//Stack is: this.bill.repeatMode, BillRepeatModeDefOf.TargetCount
					//Replacing if(repeatMode == TargetCount) with 
					//(repeatMode == TargetCount || repeatMode == TD_ColonistCount ) via method call

					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OrColonistCount_Transpiler), ModeFor(context, noSurplus)));
					yield return new CodeInstruction(
						inst.opcode == OpCodes.Bne_Un ? OpCodes.Brfalse :
						inst.opcode == OpCodes.Bne_Un_S ? OpCodes.Brfalse_S :
						inst.opcode == OpCodes.Beq ? OpCodes.Brtrue :
						OpCodes.Brtrue_S,
						inst.operand);
				}
				else
					yield return inst;
			}
		}

		public static string ModeFor(string context, bool noSurplus) =>
			noSurplus || context == "PauseWhenSatisfied" ? nameof(IsAnyPauseMode) : nameof(IsAnyTargetMode);
		public static bool IsAnyTargetMode(BillRepeatModeDef repeatMode, BillRepeatModeDef targetCountMode)
		{
			return repeatMode == targetCountMode ||
				repeatMode == RepeatModeDefOf.TD_ColonistCount ||
				repeatMode == RepeatModeDefOf.TD_XPerColonist ||
				repeatMode == RepeatModeDefOf.TD_WithSurplusIng;
		}

		public static bool IsAnyPauseMode(BillRepeatModeDef repeatMode, BillRepeatModeDef targetCountMode)
		{
			return repeatMode == targetCountMode ||
				repeatMode == RepeatModeDefOf.TD_ColonistCount ||
				repeatMode == RepeatModeDefOf.TD_XPerColonist;
		}
	}

	public static class JobDriver_DoBill_Patch
	{
		static JobDriver_DoBill_Patch()
		{
			Harmony harmony = new Harmony("Uuugggg.rimworld.Everybody_Gets_One.main");

			FieldInfo TargetCountInfo = AccessTools.Field(typeof(BillRepeatModeDefOf), nameof(BillRepeatModeDefOf.TargetCount));

			PatchCompilerGenerated.PatchGeneratedMethod(harmony, typeof(Verse.AI.JobDriver_DoBill),
				delegate (MethodInfo method)
				{
					//Here's a probably accurate way to find the initAction for the recount Toil
					return method.DeclaringType is Type generatedType
					&& generatedType.IsSealed
					&& generatedType.GetFields().Length == 2
					&& generatedType.GetFields().Any(f => f.FieldType == typeof(Toil))
					&& generatedType.GetFields().Any(f => f.FieldType == typeof(JobDriver_DoBill));
				}, transpiler: new HarmonyMethod(typeof(JobDriver_DoBill_Patch), nameof(Transpiler)));
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return OrColonistCount_Transpiler.Transpiler(instructions);
		}
	}


	//Mod support: Better Workbench Management
	[StaticConstructorOnStartup]
	public static class ImprovedWorkbenches_Patch
	{
		static ImprovedWorkbenches_Patch()
		{
			if(AccessTools.TypeByName("BillConfig_DoWindowContents_Patch") is Type patchType &&
				AccessTools.Method(patchType, "DrawFilters") is MethodInfo patchMethod)
			{
				Harmony harmony = new Harmony("Uuugggg.rimworld.Everybody_Gets_One.main");
				harmony.Patch(patchMethod, transpiler: new HarmonyMethod(typeof(ImprovedWorkbenches_Patch), "Transpiler"));
			}
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return OrColonistCount_Transpiler.TranspilerB(instructions, 9999, true);
		}

	}
}
