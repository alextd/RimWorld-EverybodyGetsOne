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

	[HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoIngredientConfigPane))]
	public static class InsertFindLibEditorButton
	{
		//protected virtual void DoIngredientConfigPane(float x, ref float y, float width, float height)
		public static void Prefix(Dialog_BillConfig __instance, float x, ref float y, float width, ref float height)
		{
			Bill_Production bill = __instance.bill;
			if (bill.repeatMode == RepeatModeDefOf.TD_PersonCount || bill.repeatMode == RepeatModeDefOf.TD_XPerPerson)
			{
				Rect rect = new(x, y, width, 30);
				if (Widgets.ButtonText(rect, "TD.EditWhoCountsAsAPerson".Translate()))
					bill.Map.OpenPersonCounter(bill);

				y += 30 + 6;
				height -= 30 + 6;
			}
		}
	}
}
