using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using TD_Find_Lib;
using ImprovedWorkbenches;
using Everybody_Gets_One;
using HarmonyLib;

namespace SupportImprovedWorkbenches
{
	[StaticConstructorOnStartup]
	public static class PatchAll
	{

		//public static Settings settings;
		static PatchAll()
		{
			// initialize settings
			// settings = GetSettings<Settings>();
#if DEBUG
			Harmony.DEBUG = true;
#endif

			Harmony harmony = new Harmony("Uuugggg.rimworld.Everybody_Gets_One.SupportImprovedWorkbenches");

			harmony.PatchAll();
		}
	}

	[HarmonyPatch(typeof(ExtendedBillDataStorage), nameof(ExtendedBillDataStorage.MirrorBills))]
	public static class NewFeature
	{
		//public void MirrorBills(Bill_Production sourceBill, Bill_Production destinationBill, bool preserveTargetProduct)
		public static void Postfix(Bill_Production sourceBill, Bill_Production destinationBill)
		{
			PersonCounterEditor.ImportInto(sourceBill.GetPersonCounter().CloneInactive(), destinationBill.GetPersonCounter());
		}
	}
}
