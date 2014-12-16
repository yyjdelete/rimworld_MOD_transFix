using RimWorld;
using RimWorld.SquadAI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
namespace TransFix
{
    public class MapIniter_LoadFromFile
    {

        public static void ExposeComponents(Map map)
        {
            Scribe_Deep.LookDeep<ColonyInfo>(ref map.colonyInfo, "colonyInfo");
            Scribe_Deep.LookDeep<PlaySettings>(ref map.playSettings, "playSettings");
            Scribe_Deep.LookDeep<RealTime>(ref Find.RootMap.realTime, "realTime");
            Scribe_Deep.LookDeep<StoryWatcher>(ref map.storyWatcher, "storyWatcher");
            Scribe_Deep.LookDeep<GameEnder>(ref map.gameEnder, "gameEnder");
            Scribe_Deep.LookDeep<LetterStack>(ref map.letterStack, "letterStack");
            Scribe_Deep.LookDeep<TickManager>(ref map.tickManager, "tickManager");
            Scribe_Deep.LookDeep<WeatherManager>(ref map.weatherManager, "weatherManager");
            Scribe_Deep.LookDeep<ResearchManager>(ref map.researchManager, "researchManager");
            Scribe_Deep.LookDeep<Storyteller>(ref map.storyteller, "storyteller");
            Scribe_Deep.LookDeep<ReservationManager>(ref map.reservationManager, "reservationManager");
            Scribe_Deep.LookDeep<DesignationManager>(ref map.designationManager, "designationManager");
            Scribe_Deep.LookDeep<BrainManager>(ref map.aiSquadBrainManager, "aiKingManager");
            Scribe_Deep.LookDeep<PassingShipManager>(ref map.passingShipManager, "visitorManager");
            Scribe_Deep.LookDeep<TutorNoteManager>(ref map.tutorNoteManager, "tutorNoteManager");
            Scribe_Deep.LookDeep<ConceptTracker>(ref map.conceptTracker, "conceptTracker");
            Scribe_Deep.LookDeep<MapConditionManager>(ref map.mapConditionManager, "mapConditionManager");
            Scribe_Deep.LookDeep<FogGrid>(ref map.fogGrid, "fogGrid");
            Scribe_Deep.LookDeep<RoofGrid>(ref map.roofGrid, "roofGrid");
            Scribe_Deep.LookDeep<TerrainGrid>(ref map.terrainGrid, "terrainGrid");
            Scribe_Deep.LookDeep<BoolGrid>(ref map.homeRegionGrid, "homeRegionGrid", MapChangeType.HomeRegion);
            Scribe_Deep.LookDeep<BoolGrid>(ref map.noRoofRegionGrid, "noRoofRegionGrid", MapChangeType.NoRoofRegion);
            Scribe_Deep.LookDeep<ZoneManager>(ref map.zoneManager, "zoneManager");
            Scribe_Deep.LookDeep<History>(ref map.history, "history");
            try
            {
                Scribe_Collections.LookList<MapComponent>(ref map.components, "components", LookMode.Deep, (object[]) null);
            }
            catch (Exception exception)
            {
                Log.Warning("Failed to load all map components");
                Log.Warning(exception.ToString());
                Log.Warning(exception.StackTrace);
            }
            Find.CameraMap.Expose();
        }

        public static void InitMapFromFile(string mapFileName)
        {
            Log.Message("Initializing map from file " + mapFileName + " with mods " + GenText.ToCommaList(LoadedModManager.LoadedMods.Select<Mod, string>(mod => mod.name)));
            Find.RootMap.curMap = new Map();
            string filePath = GenFilePaths.FilePathForSavedMap(mapFileName);
            List<Thing> list = new List<Thing>();
            Scribe.InitLoading(filePath);
            Scribe_Values.LookValue<string>(ref MapInitData.loadedVersion, "gameVersion", null, false);
            if (MapInitData.loadedVersion != VersionControl.versionStringFull)
            {
                Log.Warning("Version mismatch: Map file is version " + MapInitData.loadedVersion + ", we are running version " + VersionControl.versionStringFull + ".");
            }
            Scribe_Fix.LookDeep<MapInfo>(ref Find.Map.info, "mapInfo");
            if (!MapFiles.IsAutoSave(mapFileName))
            {
                Find.Map.info.fileName = mapFileName;
            }
            MapIniterUtility.ReinitStaticMapComponents_PreConstruct();
            Find.Map.ConstructComponents();
            MapIniterUtility.ReinitStaticMapComponents_PostConstruct();
            ExposeComponents(Find.Map);
            MapFileCompressor compressor = new MapFileCompressor();
            compressor.ExposeData();
            List<Thing> second = compressor.ThingsToSpawnAfterLoad().ToList<Thing>();
            List<Thing> list3 = new List<Thing>(0x2710);
            Scribe_Fix.LookList<Thing>(ref list3, "things", LookMode.Deep, (object[]) null);
            foreach (Thing thing in list3.Concat<Thing>(second))
            {
                list.Add(thing);
            }
            Scribe.ExitNode();
            LoadCrossRefHandler.ResolveAllCrossReferences();
            PostLoadInitter.DoAllPostLoadInits();
            foreach (Thing thing2 in list)
            {
                try
                {
                    GenSpawn.Spawn(thing2, thing2.Position, thing2.Rotation);
                }
                catch (Exception exception)
                {
                    Log.Error(string.Concat(new object[] { "Exception spawning loaded thing ", thing2, ": ", exception }));
                }
            }
            MapIniterUtility.FinalizeMapInit();
            if (!Application.isEditor && (VersionControl.BuildFromVersionString(MapInitData.loadedVersion) != VersionControl.BuildFromVersionString(VersionControl.versionStringFull)))
            {
                object[] args = new object[] { MapInitData.loadedVersion, VersionControl.versionStringFull };
                string newText = "SaveGameIncompatibleWarningText".Translate(args);
                Find.LayerStack.Add(new Layer_TextMessage("SaveGameIncompatibleWarningTitle".Translate(), newText));
            }
        }
    }
}
