using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace Everybody_Gets_One
{
	public class Settings : ModSettings
	{
		public bool setting;

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);
			
			options.CheckboxLabeled("Sample setting", ref setting);
			options.Gap();

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref setting, "setting", true);
		}
	}
}