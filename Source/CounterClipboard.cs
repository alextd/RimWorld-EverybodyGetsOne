using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using HarmonyLib;
using TD_Find_Lib;

namespace Everybody_Gets_One
{
	// Bill copy/paste is cloned into clipboard which is not tied to a map 
	// and is not entirely clear whether the clone is a copy or a paste


	// Copy the bill's Search when the clipboard is set from this bill
	[HarmonyPatch(typeof(Bill), nameof(Bill.DoInterface))]
	public static class CounterClipboard
	{
		public static QuerySearch Clipboard;

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo ClipboardInfo = AccessTools.Field(typeof(BillUtility), nameof(BillUtility.Clipboard));

			foreach (var inst in instructions)
			{
				yield return inst;

				if(inst.StoresField(ClipboardInfo))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);// Bill this
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CounterClipboard), nameof(CopyCounterFor)));//CopyCounterFor(this);
				}
			}
		}

		public static void CopyCounterFor(Bill bill)
		{
			if (bill is Bill_Production billP && (bill.Map?.HasPersonCounter(billP) ?? false))
				Clipboard = bill.Map.GetPersonCounter(billP).CloneInactive();
			else
				Clipboard = null;
			Log.Message($"Setting Clipboard to {Clipboard}");
		}
	}


	// Paste the search when the bill is added (needs map set is why it's so late into the process)

	// Bill bill = BillUtility.Clipboard.Clone();
	// bill.InitializeAfterClone();
	// SelTable.billStack.AddBill(bill);
	[HarmonyPatch(typeof(ITab_Bills), nameof(ITab_Bills.FillTab))]
	public static class CounterClipboardPaste
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo AddBillInfo = AccessTools.Method(typeof(BillStack), nameof(BillStack.AddBill));


			foreach (var inst in instructions)
			{
				if(inst.Calls(AddBillInfo))
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CounterClipboardPaste), nameof(AddBillAndPasteCounter)));
				}
				else
					yield return inst;
			}
		}


		public static void AddBillAndPasteCounter(BillStack stack, Bill bill)
		{
			// Do it as normal:
			stack.AddBill(bill);
			Log.Message($"Added bill {bill}");

			// Also do this:
			if(CounterClipboard.Clipboard is QuerySearch clip && bill is Bill_Production billP)
			{
				Map map = billP.Map;

				Log.Message($"Setting Search to Clipboard");
				map.SetPersonCounter(billP, clip.CloneForUseSingle(map));
			}
		}
	}
}
