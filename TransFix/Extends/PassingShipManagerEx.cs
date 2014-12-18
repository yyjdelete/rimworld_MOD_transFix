using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class PassingShipManagerEx
    {
        public static void ExposeDataEx(this PassingShipManager that)
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Fix.LookListNotNull<PassingShip>(ref that.passingShips, "passingShips", LookMode.Deep, null);
            }
        }
    }
}
