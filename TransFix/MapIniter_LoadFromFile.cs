using RimWorld;
using RimWorld.SquadAI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Scribe_Fix.LookDeepNotNull<ResearchManager>(ref map.researchManager, "researchManager", null);
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
            Scribe_Fix.LookDeepNotNull<TerrainGrid>(ref map.terrainGrid, "terrainGrid", null);
            Scribe_Deep.LookDeep<BoolGrid>(ref map.homeRegionGrid, "homeRegionGrid", MapChangeType.HomeRegion);
            Scribe_Deep.LookDeep<BoolGrid>(ref map.noRoofRegionGrid, "noRoofRegionGrid", MapChangeType.NoRoofRegion);
            Scribe_Deep.LookDeep<ZoneManager>(ref map.zoneManager, "zoneManager");
            Scribe_Deep.LookDeep<History>(ref map.history, "history");
            Scribe_Fix.LookListNotNull<MapComponent>(ref map.components, "components", LookMode.Deep, null);
            Find.CameraMap.Expose();
        }

        public static void InitMapFromFile(string mapFileName)
        {
            //DeepProfiler.Start("InitMapFromFile");
            //Verse.MapIniter_LoadFromFile.InitMapFromFile(mapFileName);//47287ms(empty), 9441ms
            //DeepProfiler.End("InitMapFromFile");
            //return;
            Stopwatch sw = Stopwatch.StartNew();
            //Verse.MapIniter_LoadFromFile.InitMapFromFile(mapFileName);//47287ms(empty)
            //Log.Message(((DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerMillisecond) + "ms used.");
            //return;
            //109397ms??
            Log.Message("Initializing map from file " + mapFileName + " with mods " + GenText.ToCommaList(LoadedModManager.LoadedMods.Select<Mod, string>(mod => mod.name)));
            Find.RootMap.curMap = new Map();
            string filePath = GenFilePaths.FilePathForSavedMap(mapFileName);
            //List<Thing> list = new List<Thing>();
            Scribe.InitLoading(filePath);
            Scribe_Values.LookValue<string>(ref MapInitData.loadedVersion, "gameVersion", null, false);
            if (MapInitData.loadedVersion != VersionControl.versionStringFull)
            {
                Log.Warning("Version mismatch: Map file is version " + MapInitData.loadedVersion + ", we are running version " + VersionControl.versionStringFull + ".");
            }
            Log.Message(sw.ElapsedMilliseconds + "ms used before init MapInfo");
            Scribe_Deep.LookDeep<MapInfo>(ref Find.Map.info, "mapInfo");
            if (!MapFiles.IsAutoSave(mapFileName))
            {
                Find.Map.info.fileName = mapFileName;
            }
            //NOTE: Fix Faction(Faction will be loaded by Map.ConstructComponents(), and inited after MapInfo)
            Log.Message(sw.ElapsedMilliseconds + "ms used before Fix Factions");
            Log.Message("Fix Factions");
            Find.FactionManager.Fix();

            Log.Message(sw.ElapsedMilliseconds + "ms used before init Map");
            MapIniterUtility.ReinitStaticMapComponents_PreConstruct();
            Find.Map.ConstructComponents();
            MapIniterUtility.ReinitStaticMapComponents_PostConstruct();
            ExposeComponents(Find.Map);
            Log.Message(sw.ElapsedMilliseconds + "ms used before MapFileCompressor");
            MapFileCompressor compressor = new MapFileCompressor();
            compressor.ExposeData();
            //List<Thing> second = compressor.ThingsToSpawnAfterLoad().ToList<Thing>();
            Log.Message(sw.ElapsedMilliseconds + "ms used before ThingsToSpawnAfterLoad");
            Log.Message("Fix ThingsToSpawnAfterLoad...");
            IEnumerable<Thing> second = Scribe_Fix.ThingsToSpawnAfterLoad(compressor);
            Log.Message(sw.ElapsedMilliseconds + "ms used before LookListNotNull");
            List<Thing> list = new List<Thing>(0x10000);//50447 in my case
            Scribe_Fix.LookListNotNull<Thing>(ref list, "things", LookMode.Deep, null);
            Log.Message(sw.ElapsedMilliseconds + "ms used after LookListNotNull");
            //Find.ListerPawns.Fix();
            Log.Message(sw.ElapsedMilliseconds + "GetThings" + list.Count);
            //list3.AddRange(second);
            IEnumerable<Thing> enumerable2 = list.Concat<Thing>(second);
            //Log.Message(sw.ElapsedMilliseconds + "GetThings(ALL)" + list3.Count);
            //foreach (Thing thing in list3.Concat<Thing>(second))
            //{
            //    list.Add(thing);
            //}
            Scribe.ExitNode();
            Log.Message(sw.ElapsedMilliseconds + "ms used before ResolveAllCrossReferences");
            LoadCrossRefHandlerEx.ResolveAllCrossReferences();

            PostLoadInitter.DoAllPostLoadInits();
            Log.Message(sw.ElapsedMilliseconds + "ms used before Fix pawn");
            Log.Message("Fix Pawns");
            list.Fix();
            Log.Message(sw.ElapsedMilliseconds + "ms used before spawn");
            foreach (Thing thing in enumerable2)
            {
                try
                {
                    GenSpawn.Spawn(thing, thing.Position, thing.Rotation);
                }
                catch (Exception exception)
                {
                    Log.Error(string.Concat(new object[] { "Exception spawning loaded thing ", thing, ": ", exception }));
                }
            }
            Log.Message(sw.ElapsedMilliseconds + "ms used after spawn");
            Scribe_Fix.CheckMapGen();
            MapIniterUtility.FinalizeMapInit();
            if (!Application.isEditor && (VersionControl.BuildFromVersionString(MapInitData.loadedVersion) != VersionControl.BuildFromVersionString(VersionControl.versionStringFull)))
            {
                object[] args = new object[] { MapInitData.loadedVersion, VersionControl.versionStringFull };
                string newText = "SaveGameIncompatibleWarningText".Translate(args);
                Find.LayerStack.Add(new Layer_TextMessage("SaveGameIncompatibleWarningTitle".Translate(), newText));
            }
            Log.Message(sw.ElapsedMilliseconds + "ms used.");
            Resources.UnloadUnusedAssets();
            Scribe_Fix.Clean();
            GC.Collect();
            sw.Stop();
            Log.Message(sw.ElapsedMilliseconds + "ms used.");//3957ms used.
        }
    }
}
