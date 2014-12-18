using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using Verse;

namespace TransFix.Extends
{
    public static class ZoneEx
    {
        
        public static void ExposeDataEx(this Zone zone)
        {
            //zone.ExposeData();//注意是虚函数
            Scribe_Values.LookValue<string>(ref zone.zoneName, "zoneName", null, false);
            Scribe_Values.LookValue<Color>(ref zone.zoneColor, "zoneColor", new Color(), false);
            Scribe_Values.LookValue<bool>(ref zone.hidden, "hidden", false, false);
            Scribe_Collections.LookList<IntVec3>(ref zone.cells, "cells", LookMode.Undefined, null);

            zone.FixCells();
        }

        private static void FixCells(this Zone zone)
        {
            if (zone.cells == null)
            {
                //A7
                Scribe_Fix.LookListNotNull<IntVec3>(ref zone.cells, "squares", LookMode.Undefined, null);
                if (zone.cells == null)
                {
                    throw new InvalidOperationException("Zone must have cells!!!");
                }
            }
        }
        public static void ExposeDataEx(this Zone_Growing zone)
        {
            //((Zone)zone).ExposeDataEx();
            //Scribe_Defs.LookDef<ThingDef>(ref zone.plantDefToGrow, "plantDefToGrow");
            zone.ExposeData();
            zone.FixCells();
        }
        public static void ExposeDataEx(this Zone_Stockpile zone)
        {
            ((Zone)zone).ExposeDataEx();
            Scribe_Fix.LookDeepNotNull<StorageSettings>(ref zone.settings, "settings", zone);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                zone.slotGroup = new SlotGroup(zone);
            }
        }

    }
}
