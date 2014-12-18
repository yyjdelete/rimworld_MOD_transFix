using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace TransFix.Extends
{
    public static class MapIniterUtilityEx
    {
        public static void FinalizeMapInit()
        {
            DeepProfiler.Start("RecalculateAllPerceivedPathCosts");
            Find.PathGrid.RecalculateAllPerceivedPathCosts();//148
            DeepProfiler.End("RecalculateAllPerceivedPathCosts");
            DeepProfiler.Start("RebuildAllRegions");
            RegionAndRoomUpdater.RebuildAllRegions();//257
            DeepProfiler.End("RebuildAllRegions");
            DeepProfiler.Start("UpdatePowerNetsAndConnections_First");
            PowerNetManager.UpdatePowerNetsAndConnections_First();//1
            DeepProfiler.End("UpdatePowerNetsAndConnections_First");

            TemperatureSaveLoadEx.ApplyLoadedDataToRegions();
            foreach (Thing thing in Find.ListerThings.AllThings.ToList<Thing>())
            {
                try
                {
                    thing.PostMapInit();
                }
                catch (Exception exception)
                {
                    Log.Error(string.Concat(new object[] { "Exception PostLoadAllSpawned in ", thing, ": ", exception }));
                }
            }
            Find.Map.listerFilth.Init();
            DeepProfiler.Start("RegenerateEverythingNow");
            //Find.Map.mapDrawer.RegenerateEverythingNow();//7163
            Find.Map.mapDrawer.RegenerateEverythingNowEx();//7163
            DeepProfiler.End("RegenerateEverythingNow");
            DeepProfiler.Start("ReapplyAllMods");
            Find.ResearchManager.ReapplyAllMods();//0
            DeepProfiler.End("ReapplyAllMods");
            DeepProfiler.Start("ResetSize");
            Find.CameraMap.ResetSize();//0
            DeepProfiler.End("ResetSize");
            DeepProfiler.Start("StartFade1");
            Find.CameraFade.StartFade(Color.black, 0f);//0
            DeepProfiler.End("StartFade1");
            DeepProfiler.Start("StartFade2");
            Find.CameraFade.StartFade(Color.clear, 0.65f);//0
            DeepProfiler.End("StartFade2");
            DeepProfiler.Start("UpdateResourceCounts");
            Find.ResourceCounter.UpdateResourceCounts();//0
            DeepProfiler.End("UpdateResourceCounts");
            DeepProfiler.Start("Notify_MapInited");
            MapInitData.Notify_MapInited();//0
            DeepProfiler.End("Notify_MapInited");
            DeepProfiler.Start("Initialized");
            Map.Initialized = true;//0
            DeepProfiler.End("Initialized");
            DeepProfiler.Output();
        }

    }
}
