using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;
using UnityEngine;
using TD_Find_Lib;

namespace Everybody_Gets_One
{
	public class PersonCountMapComp : MapComponent
	{
		//cache these counts each update
		public Dictionary<Bill_Production, QuerySearch> billPersonCounters = new();

		public PersonCountMapComp(Map map) : base(map) { }

		private List<Bill_Production> scribeBills;
		private List<QuerySearch> scribeSearches;
		public override void ExposeData()
		{
			Scribe_Collections.Look(ref billPersonCounters, "billPersonCounters", LookMode.Reference, LookMode.Deep, ref scribeBills, ref scribeSearches);
			if (billPersonCounters == null)
				billPersonCounters = new();
		}

		public bool HasPersonCounter(Bill_Production bill)
		{
			return billPersonCounters.ContainsKey(bill);
		}

		public void SetPersonCounter(Bill_Production bill, QuerySearch search)
		{
			billPersonCounters[bill] = search;

		}

		public QuerySearch GetPersonCounter(Bill_Production bill)
		{
			QuerySearch personCounter;
			if (!billPersonCounters.TryGetValue(bill, out personCounter))
			{
				personCounter = MakePersonCounter(bill);
				billPersonCounters[bill] = personCounter;
			}
			return personCounter;
		}

		public void RemovePersonCounter(Bill_Production bill)
		{
			billPersonCounters.Remove(bill);
		}


		public int CountFor(Bill_Production bill)
		{
			QuerySearch personCounter = GetPersonCounter(bill);

			personCounter.RemakeList();

			return personCounter.result.allThings.Sum(t => t.stackCount);
		}

		public QuerySearch MakePersonCounter(Bill_Production bill)
		{
			QuerySearch search = new(map);
			search.SetListType(SearchListType.Everyone, false);

			ThingQueryBasicProperty queryColonist = ThingQueryMaker.MakeQuery<ThingQueryBasicProperty>();
			queryColonist.sel = QueryPawnProperty.IsColonist;
			search.Children.Add(queryColonist, remake: false);

			ThingQueryBasicProperty querySlave = ThingQueryMaker.MakeQuery<ThingQueryBasicProperty>();
			querySlave.sel = QueryPawnProperty.IsSlaveOfColony;
			querySlave.include = false;
			search.Children.Add(querySlave, remake: false);

			ThingQueryQuest queryQuestLodger = ThingQueryMaker.MakeQuery<ThingQueryQuest>();
			//Default is Quest Lodger
			queryQuestLodger.include = false;
			search.Children.Add(queryQuestLodger, remake: false);

			search.name = "TD.PeopleForBill".Translate() + bill.LabelCap;

			// Maybe don't immediately open it.
			// Find.WindowStack.Add(new PersonCounterEditor(search));

			return search;
		}

		public void OpenPersonCounter(Bill_Production bill)
		{
			Find.WindowStack.Add(new PersonCounterEditor(GetPersonCounter(bill)));
		}
	}

	public class PersonCounterEditor : SearchEditorWindow
	{
		QuerySearch originalSearch;
		public PersonCounterEditor(QuerySearch search) : base(search, null)
		{
			drawer.search.changed = false;
			originalSearch = search.CloneForUse();

			//Same as the Dialog_BillConfig
			forcePause = true;
			doCloseX = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;

			//Overrides from SearchEditorWindow
			onlyOneOfTypeAllowed = true;
			//preventCameraMotion = false;
			draggable = false;
			resizeable = false;
			//above //doCloseX = true;
		}

		public override Vector2 InitialSize => new Vector2(750, 320);

		public override void SetInitialSizeAndPosition()
		{
			base.SetInitialSizeAndPosition();
			windowRect.x = (UI.screenWidth - windowRect.width) / 2;
			windowRect.y = 0;
		}

		public override void Import(QuerySearch search)
		{
			// Keep name and map type, only take these:
			drawer.search.parameters.listType = search.parameters.listType;
			drawer.search.changedSinceRemake = true;

			drawer.search.Children.queries = search.Children.queries;
			drawer.search.Children.matchAllQueries = search.Children.matchAllQueries;

			drawer.search.changed = true;
		}

		public override void PostClose()
		{
			if (drawer.search.changed)
			{
				Verse.Find.WindowStack.Add(new Dialog_MessageBox(
					null,
					"Confirm".Translate(), null,
					"No".Translate(), () =>
					{
						Import(originalSearch);
						drawer.search.changed = false;
					},
					"Keep changes?",
					true, null,
					delegate () { }// I dunno who wrote this class but this empty method is required so the window can close with esc because its logic is very different from its base class
					));
			}
		}
	}

	public static class MapCompExtensions
	{
		public static int CurrentPersonCount(this Map map, Bill_Production bill) =>
			map.GetComponent<PersonCountMapComp>().CountFor(bill);

		public static void OpenPersonCounter(this Map map, Bill_Production bill) =>
			map.GetComponent<PersonCountMapComp>().OpenPersonCounter(bill);

		public static bool HasPersonCounter(this Map map, Bill_Production bill) =>
			map.GetComponent<PersonCountMapComp>().HasPersonCounter(bill);

		public static QuerySearch GetPersonCounter(this Map map, Bill_Production bill) =>
			map.GetComponent<PersonCountMapComp>().GetPersonCounter(bill);

		public static void RemovePersonCounter(this Map map, Bill_Production bill) =>
			map.GetComponent<PersonCountMapComp>().RemovePersonCounter(bill);

		public static void SetPersonCounter(this Map map, Bill_Production bill, QuerySearch search) =>
			map.GetComponent<PersonCountMapComp>().SetPersonCounter(bill, search);
	}


	[HarmonyPatch(typeof(Building_WorkTable), nameof(Building_WorkTable.Notify_BillDeleted))]
	public static class DeleteBillCounter
	{
		public static void Postfix(Building_WorkTable __instance, Bill bill)
		{
			if(bill is Bill_Production billP)
			{
				__instance.MapHeld.RemovePersonCounter(billP);
			}
		}
	}
}
