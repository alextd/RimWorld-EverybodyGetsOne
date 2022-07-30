using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Everybody_Gets_One
{
	public class ColonistCountMapComp : MapComponent
	{
		//cache these counts each update
		public int colonistCount, slaveCount, inhabitantCount;

		public ColonistCountMapComp(Map map) : base(map) { }
		public override void FinalizeInit()
		{
			MapComponentUpdate();
		}
		public override void MapComponentUpdate()
		{
			colonistCount = map.mapPawns.AllPawns.Count(p => p.IsColonist && !p.IsSlaveOfColony && !p.IsQuestLodger());
			slaveCount = map.mapPawns.AllPawns.Count(p => p.IsSlaveOfColony && !p.IsQuestLodger());
			inhabitantCount = map.mapPawns.AllPawns.Count(p => (p.IsColonist || p.IsSlaveOfColony) && !p.IsQuestLodger());
		}
	}
	public static class MapCompExtensions
	{
		public static int CurrentColonistCount(this Map map) =>
			map.GetComponent<ColonistCountMapComp>().colonistCount;
		public static int CurrentSlaveCount(this Map map) =>
			map.GetComponent<ColonistCountMapComp>().slaveCount;
		public static int CurrentInhabitantCount(this Map map) =>
			map.GetComponent<ColonistCountMapComp>().inhabitantCount;
	}
}
