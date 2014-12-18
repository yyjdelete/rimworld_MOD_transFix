using RimWorld;
using RimWorld.Planet;
using RimWorld.SquadAI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using TransFix.Extends;
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
            //StoryWatcher->StoryState->Dictionary<IncidentDef, int>
            Scribe_Fix.LookDeepNotNull<StoryWatcher>(ref map.storyWatcher, "storyWatcher", null);
            Scribe_Deep.LookDeep<GameEnder>(ref map.gameEnder, "gameEnder");
            Scribe_Deep.LookDeep<LetterStack>(ref map.letterStack, "letterStack");
            Scribe_Deep.LookDeep<TickManager>(ref map.tickManager, "tickManager");
            Scribe_Deep.LookDeep<WeatherManager>(ref map.weatherManager, "weatherManager");
            Scribe_Fix.LookDeepNotNull<ResearchManager>(ref map.researchManager, "researchManager", null);//FIX Research
            Scribe_Deep.LookDeep<Storyteller>(ref map.storyteller, "storyteller");
            Scribe_Deep.LookDeep<ReservationManager>(ref map.reservationManager, "reservationManager");
            Scribe_Deep.LookDeep<DesignationManager>(ref map.designationManager, "designationManager");
            Scribe_Deep.LookDeep<BrainManager>(ref map.aiSquadBrainManager, "aiKingManager");
            Scribe_Fix.LookDeepNotNull<PassingShipManager>(ref map.passingShipManager, "visitorManager");//贸易商
            //Scribe_Deep.LookDeep<TutorNoteManager>(ref map.tutorNoteManager, "tutorNoteManager");
            //Scribe_Deep.LookDeep<ConceptTracker>(ref map.conceptTracker, "conceptTracker");
            Scribe_Fix.LookDeepNotNull<MapConditionManager>(ref map.mapConditionManager, "mapConditionManager", null);//Fix while zombie use it
            Scribe_Deep.LookDeep<FogGrid>(ref map.fogGrid, "fogGrid");
            Scribe_Deep.LookDeep<RoofGrid>(ref map.roofGrid, "roofGrid");
            Scribe_Fix.LookDeepNotNull<TerrainGrid>(ref map.terrainGrid, "terrainGrid", null);//Try rehash before set sand
            Scribe_Deep.LookDeep<BoolGrid>(ref map.homeRegionGrid, "homeRegionGrid", MapChangeType.HomeRegion);
            Scribe_Deep.LookDeep<BoolGrid>(ref map.noRoofRegionGrid, "noRoofRegionGrid", MapChangeType.NoRoofRegion);
            Scribe_Fix.LookDeepNotNull<BoolGrid>(ref map.snowClearRegionGrid, "snowClearRegionGrid", MapChangeType.SnowClearRegion);
            //Zone的cells由squares改为了cells
            Scribe_Fix.LookDeepNotNull<ZoneManager>(ref map.zoneManager, "zoneManager", null);
            Scribe_Deep.LookDeep<History>(ref map.history, "history");
            Scribe_Fix.LookDeepNotNull<TemperatureCache>(ref map.temperatureCache, "temperatureGrid", null);
            Scribe_Fix.LookDeepNotNull<SnowGrid>(ref map.snowGrid, "snowGrid", null);
            Scribe_Fix.LookListNotNull<MapComponent>(ref map.components, "components", LookMode.Deep, null);

            map.CheckMapComponent();

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

            MapNewRes(MapInitData.loadedVersion, VersionControl.versionStringFull);

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
            Scribe_Fix.LookDeepNotNull<MapInfo>(ref Find.Map.info, "mapInfo", null);
            if (!MapFiles.IsAutoSave(mapFileName))
            {
                Find.Map.info.fileName = mapFileName;
            }
            //NOTE: Fix Faction(Faction will be loaded by Map.ConstructComponents(), and inited after MapInfo)
            Log.Message(sw.ElapsedMilliseconds + "ms used before Fix Factions");
            Log.Message("Fix Factions");
            if (Find.FactionManager.Fix())
            {
                Log.Warning("Save new Factions to world");
                WorldSaver.SaveToFile(Current.World);
            }

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
            IEnumerable<Thing> second = compressor.ThingsToSpawnAfterLoadEx();
            Log.Message(sw.ElapsedMilliseconds + "ms used before LookListNotNull");
            List<Thing> list3 = new List<Thing>(0x10000);//50447 in my case
            Scribe_Fix.LookListNotNull<Thing>(ref list3, "things", LookMode.Deep, null);
            Log.Message(sw.ElapsedMilliseconds + "ms used after LookListNotNull");
            //Find.ListerPawns.Fix();
            Log.Message(sw.ElapsedMilliseconds + "GetThings" + list3.Count);
            //list3.AddRange(second);
            IEnumerable<Thing> list = list3.Concat(second);
            //Log.Message(sw.ElapsedMilliseconds + "GetThings(ALL)" + list3.Count);
            //foreach (Thing thing in list3.Concat<Thing>(second))
            //{
            //    list.Add(thing);
            //}
            Scribe.ExitNode();
            Log.Message(sw.ElapsedMilliseconds + "ms used before ResolveAllCrossReferences");
            LoadCrossRefHandlerEx.ResolveAllCrossReferences();

            Log.Message(sw.ElapsedMilliseconds + "ms used before Fix pawn");
            Log.Message("Fix Pawns");
            list3.Fix();//before spawn, after ResolveAllCrossReferences

            PostLoadInitter.DoAllPostLoadInits();
            //foreach (Faction f1 in Find.FactionManager.AllFactions)
            //{
            //    foreach (Faction f2 in Find.FactionManager.AllFactions)
            //    {
            //        Log.Message(String.Format("{0}[{1}]->{2}[{3}]", f1.name, f1.def.label, f2.name, f2.def.label));
            //        FactionUtility.IsHostileToward(f1, f2);
            //    }
            //}
            Log.Message(sw.ElapsedMilliseconds + "ms used before spawn");
            foreach (Thing thing2 in list)
            {
                try
                {
                    GenSpawn.Spawn(thing2, thing2.Position, thing2.Rotation);
                    //var g = thing2.Graphic;//check whether if could be draw
                }
                catch (Exception exception)
                {
                    Log.Error(string.Concat(new object[] { "Exception spawning loaded thing ", thing2, "(", thing2.Stuff ,"): ", exception, thing2.Position, thing2.Rotation }));
                    //remove it
                    thing2.Destroy();
                }
            }
            Log.Message(sw.ElapsedMilliseconds + "ms used after spawn");
            ModUtils.CheckMapGen();
            Log.Message(sw.ElapsedMilliseconds + "ms used after CheckMapGen");
            //在此之后地图的绘制冻结解除
            MapIniterUtilityEx.FinalizeMapInit();
            if (!Application.isEditor && (VersionControl.BuildFromVersionString(MapInitData.loadedVersion) != VersionControl.BuildFromVersionString(VersionControl.versionStringFull)))
            {
                object[] args = new object[] { MapInitData.loadedVersion, VersionControl.versionStringFull };
                string newText = "SaveGameIncompatibleWarningText".Translate(args);
                Find.LayerStack.Add(new Layer_TextMessage("SaveGameIncompatibleWarningTitle".Translate(), newText));
            }
            Log.Message(sw.ElapsedMilliseconds + "ms used.");
            Resources.UnloadUnusedAssets();
            ModUtils.Clean();
            GC.Collect();
            sw.Stop();
            Log.Message(sw.ElapsedMilliseconds + "ms used.");//3957ms used.
        }

        private static void MapNewRes(string oldVer, string newVer)
        {
            if (oldVer != newVer)
            {
                //A7->A8
                //A8: Metal->Steel, StoneBlocks->BlocksLimestone
                if (!DefDatabaseEx<ThingDef>.DefsByName.ContainsKey("Metal"))
                    DefDatabaseEx<ThingDef>.DefsByName.Add("Metal", ThingDefOf.Steel);
                if (!DefDatabaseEx<ThingDef>.DefsByName.ContainsKey("StoneBlocks"))
                    DefDatabaseEx<ThingDef>.DefsByName.Add("StoneBlocks", ThingDef.Named("BlocksLimestone"));
            }
        }
    }
}
