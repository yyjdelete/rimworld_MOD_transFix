using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class MapInfoEx
    {
        public static void ExposeDataEx(this MapInfo mapInfo)
        {
            Scribe_Values.LookValue<string>(ref mapInfo.fileName, "name", null, false);
            IntVec3 size = mapInfo.Size;
            Scribe_Values.LookValue<IntVec3>(ref size, "size", new IntVec3(), false);
            mapInfo.Size = size;
            Scribe_Values.LookValue<IntVec2>(ref mapInfo.worldCoords, "worldCoords", new IntVec2(), false);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                string fileNameNoExtension = Current.World.info.FileNameNoExtension;
                Scribe_Values.LookValue<string>(ref fileNameNoExtension, "worldFileName", null, false);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe.SaveState();
                string str2 = "error";
                Scribe_Values.LookValue<string>(ref str2, "worldFileName", null, false);
                WorldLoaderEx.LoadWorldFromFile(GenFilePaths.FilePathForWorld(str2), false);
                Scribe.RestoreState();
            }
            Scribe_Values.LookValue<string>(ref mapInfo.historyGameplayID, "historyGameplayID", null, false);
            Scribe_Values.LookValue<string>(ref mapInfo.historyFirstUploadDate, "historyFirstUploadDate", null, false);
            Scribe_Values.LookValue<int>(ref mapInfo.historyFirstUploadTime, "historyFirstUploadTime", 0, false);
            Scribe_Values.LookValue<int>(ref ThingIDCounter.maxThingIDIndex, "maxThingIDIndex", 0, false);
        }

    }
}
