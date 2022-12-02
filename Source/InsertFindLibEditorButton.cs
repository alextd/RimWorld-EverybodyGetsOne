using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using TD_Find_Lib;

namespace Everybody_Gets_One
{

	[HarmonyPatch(typeof(Dialog_BillConfig), "DoIngredientConfigPane")]
	public static class InsertFindLibEditorButton
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo DoThingFilterConfigWindowInfo = AccessTools.Method(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow));


			foreach (var inst in instructions)
			{
				if (inst.Calls(DoThingFilterConfigWindowInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);//this
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Dialog_BillConfig), "bill"));//this.bill
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InsertFindLibEditorButton), nameof(DoThingFilterConfigWindowAfterMyButton)));
					//DoThingFilterConfigWindow( ... ) => DoThingFilterConfigWindowAfterMyWinow(..., bill)
				}
				else
					yield return inst;
			}
		}

		//public static void DoThingFilterConfigWindow(Rect rect, UIState state, ThingFilter filter, ThingFilter parentFilter = null, int openMask = 1, IEnumerable<ThingDef> forceHiddenDefs = null, IEnumerable<SpecialThingFilterDef> forceHiddenFilters = null, bool forceHideHitPointsConfig = false, List<ThingDef> suppressSmallVolumeTags = null, Map map = null)
		public static void DoThingFilterConfigWindowAfterMyButton(Rect rect, ThingFilterUI.UIState state, ThingFilter filter, ThingFilter parentFilter = null, int openMask = 1, IEnumerable<ThingDef> forceHiddenDefs = null, IEnumerable<SpecialThingFilterDef> forceHiddenFilters = null, bool forceHideHitPointsConfig = false, List<ThingDef> suppressSmallVolumeTags = null, Map map = null, Bill_Production bill = null)
		{
			if (bill.repeatMode == RepeatModeDefOf.TD_PersonCount || bill.repeatMode == RepeatModeDefOf.TD_XPerPerson)
			{
				Rect myRect = rect.TopPartPixels(30);
				if (Widgets.ButtonText(myRect, "TD.EditWhoCountsAsAPerson".Translate()))
				{
					map.OpenPersonCounter(bill);
				}


				rect.yMin += 36;
			}
			ThingFilterUI.DoThingFilterConfigWindow(rect, state, filter, parentFilter, openMask, forceHiddenDefs, forceHiddenFilters, forceHideHitPointsConfig, suppressSmallVolumeTags, map);
		}
	}
}
