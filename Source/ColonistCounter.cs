using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using TD_Find_Lib;

namespace Everybody_Gets_One
{
	public class PersonCountMapComp : MapComponent
	{
		//cache these counts each update
		public Dictionary<Bill_Production, QuerySearch> billPersonCounters = new();

		public PersonCountMapComp(Map map) : base(map) { }

		public int CountFor(Bill_Production bill)
		{
			QuerySearch personCounter;
			if(!billPersonCounters.TryGetValue(bill, out personCounter))
			{
				personCounter = MakePersonCounter(bill);
				billPersonCounters[bill] = personCounter;
			}
			personCounter.RemakeList();
			return personCounter.result.allThings.Count;
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

			search.name = "People for bill ";

			Find.WindowStack.Add(new SearchEditorWindow(search, null));

			return search;
		}
	}

	public static class MapCompExtensions
	{
		public static int CurrentPersonCount(this Map map, Bill_Production bill) =>
			map.GetComponent<PersonCountMapComp>().CountFor(bill);
	}
}
