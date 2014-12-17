using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class WorldEx
    {
        public static void ExposeDataEx(this World world)
        {
            Scribe_Deep.LookDeep<WorldInfo>(ref world.info, "info");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                WorldGenerator_Grid.GenerateGridIntoCurrentWorld(world.info.seedString);
            }
            Scribe_Collections.LookList<Site>(ref world.sites, "sites", LookMode.Deep, null);
            Scribe_Deep.LookDeep<FactionManager>(ref world.factionManager, "factionManager");
        }
    }
}
