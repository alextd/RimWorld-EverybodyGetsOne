using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Everybody_Gets_One
{
	public class PersonCountMapComp : MapComponent
	{
		//cache these counts each update
		public int colonistCount;

		public PersonCountMapComp(Map map) : base(map) { }
		public override void FinalizeInit()
		{
			MapComponentUpdate();
		}
		public override void MapComponentUpdate()
		{
			colonistCount = map.mapPawns.AllPawns.Count(p => p.IsColonist && !p.IsSlaveOfColony && !p.IsQuestLodger());
		}
	}
	public static class MapCompExtensions
	{
		public static int CurrentPersonCount(this Map map) =>
			map.GetComponent<PersonCountMapComp>().colonistCount;
	}
}
