using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class WorldLoaderEx
    {
        public static void LoadWorldFromFile(string absFilePath, bool finalize)
        {
            Scribe.InitLoading(absFilePath);
            Current.World = new World();
            Current.World.ExposeDataEx();
            if (finalize)
            {
                LoadCrossRefHandler.ResolveAllCrossReferences();
                PostLoadInitter.DoAllPostLoadInits();
            }
        }
    }
}
