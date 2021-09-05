using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Everybody_Gets_One
{
	static class ColonistCounter
	{
		//Copy of ColonistCount plus IsQuestLodger check
		public static int PermanentColonistCount(this MapPawns mapPawns)
		{
			if (Current.ProgramState != ProgramState.Playing)
			{
				Verse.Log.Error("ColonistCount while not playing. This should get the starting player pawn count.");
				return 3;
			}
			int num = 0;
			List<Pawn> allPawns = mapPawns.AllPawns;
			for (int i = 0; i < allPawns.Count; i++)
			{
				if (allPawns[i].IsColonist && !allPawns[i].IsSlaveOfColony && !allPawns[i].IsQuestLodger())
				{
					num++;
				}
			}
			return num;
		}

		public static int PermanentSlaveCount(this MapPawns mapPawns)
		{
			if (Current.ProgramState != ProgramState.Playing)
			{
				Verse.Log.Error("SlaveCount while not playing. This should get the starting slave pawn count.");
				return 0;
			}

			int num = 0;
			List<Pawn> allPawns = mapPawns.AllPawns;
			for (int i = 0; i < allPawns.Count; i++)
			{
				if (allPawns[i].IsSlaveOfColony && !allPawns[i].IsQuestLodger())
				{
					num++;
				}
			}

			return num;
		}
	}
}
