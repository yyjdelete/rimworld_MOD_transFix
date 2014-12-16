using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using UnityEngine;
using Verse;

namespace TransFix
{
    public static class Scribe_Fix
    {
        //Only for LoadSaveMode.LoadingVars and with LookMode.DefReference/LookMode.Deep Only
        public enum ErrMode
        {
            Skip = 0,
            UseDefault = 1,
            //Reseed = 2
        }

        //skip empty
        public static void LookListNotNull<T>(ref List<T> list, string listLabel, LookMode lookMode, params object[] ctorArgs)
        {
            LookList<T>(ref list, listLabel, lookMode, ErrMode.Skip, ctorArgs);
        }

        public static void LookList<T>(ref List<T> list, string listLabel, LookMode lookMode, ErrMode errMode, params object[] ctorArgs)
        {
            if (lookMode == LookMode.Undefined)
            {
                if (typeof(T) != typeof(TargetPack))
                {
                    if (!typeof(T).IsValueType && (typeof(T) != typeof(string)))
                    {
                        if (!typeof(Def).IsAssignableFrom(typeof(T)))
                        {
                            Log.Error("LookList call with a list of " + typeof(T) + " must have lookMode set explicitly.");
                            return;
                        }
                        lookMode = LookMode.DefReference;
                    }
                    else
                    {
                        lookMode = LookMode.Value;
                    }
                }
                else
                {
                    lookMode = LookMode.TargetPack;
                }
            }

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Scribe.EnterNode(listLabel);
                if (list == null)
                {
                    Scribe.WriteAttribute("IsNull", "True");
                }
                else
                {
                    foreach (T local in list)
                    {
                        if (lookMode == LookMode.Value)
                        {
                            T local2 = local;
                            T defaultValue = default(T);
                            Scribe_Values.LookValue<T>(ref local2, XmlToItem.ListItemName, defaultValue, true);
                        }
                        else if (lookMode == LookMode.TargetPack)
                        {
                            TargetPack pack = (TargetPack)(System.Object)local;
                            Scribe_TargetPack.LookTargetPack(ref pack, XmlToItem.ListItemName);
                        }
                        else if (lookMode == LookMode.DefReference)
                        {
                            Def def = (Def)(System.Object)local;
                            Scribe_Defs.LookDef<Def>(ref def, XmlToItem.ListItemName);
                        }
                        else if (lookMode == LookMode.Deep)
                        {
                            Saveable target = (Saveable)local;
                            Scribe_Deep.LookDeep<Saveable>(ref target, XmlToItem.ListItemName, ctorArgs);
                        }
                        else if (lookMode == LookMode.MapReference)
                        {
                            LoadReferenceable refee = (LoadReferenceable)local;
                            Scribe_References.LookReference<LoadReferenceable>(ref refee, XmlToItem.ListItemName);
                        }
                    }
                }
                Scribe.ExitNode();
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                XmlNode node = Scribe.curParent[listLabel];
                if (node == null)
                {
                    list = null;
                }
                else
                {
                    XmlAttribute attribute = node.Attributes["IsNull"];
                    if ((attribute != null) && ("true".Equals(attribute.Value, StringComparison.OrdinalIgnoreCase)))
                    {
                        list = null;
                    }
                    else if (lookMode == LookMode.Value)
                    {
                        list = new List<T>();
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            T item;
                            try
                            {
                                item = ScribeExtractor.ValueFromNode<T>(subNode, default(T));
                            }
                            catch
                            {
                                item = default(T);
                                Log.Warning("Failed to LookList(mode=" + lookMode + "): " + listLabel + "[" + typeof(T).ToString() + "]\nSubnode:\n" + subNode.OuterXml);
                            }
                            if (errMode != ErrMode.Skip || !object.Equals(item, default(T)))
                            {
                                list.Add(item);
                            }
                        }
                    }
                    else if (lookMode == LookMode.Deep)
                    {
                        list = new List<T>();
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            //Thing
                            T item;
                            try
                            {
                                //skip bad and null
                                item = Scribe_Fix.SaveableFromNode<T>(subNode, ctorArgs);
                            }
                            catch (Exception e)
                            {
                                //Log.Notify_Exception(e);
                                item = default(T);
                                Log.Warning("Failed to LookList(mode=" + lookMode + "): " + listLabel + "[" + typeof(T).ToString() + "]\nSubnode:\n" + subNode.OuterXml);
                            }
                            if (errMode != ErrMode.Skip || !object.Equals(item, default(T)))
                            {
                                list.Add(item);
                            }
                        }
                    }
                    else if (lookMode == LookMode.DefReference)
                    {
                        list = new List<T>();
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            //Def
                            T item;
                            try
                            {
                                item = ScribeExtractor.DefFromNodeUnsafe<T>(subNode);//DefFromNodeUnsafe == DefFromNode
                            }
                            catch
                            {
                                item = default(T);
                                Log.Warning("Failed to LookList(mode=" + lookMode + "): " + listLabel + "[" + typeof(T).ToString() + "]\nSubnode:\n" + subNode.OuterXml);
                            }
                            if (errMode != ErrMode.Skip || !object.Equals(item, default(T)))
                            {
                                list.Add(item);
                            }
                        }
                    }
                    else if (lookMode == LookMode.TargetPack)
                    {
                        list = new List<T>();
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            T item;
                            try
                            {
                                item = (T)(System.Object)ScribeExtractor.TargetPackFromNode(subNode, TargetPack.Invalid);
                            }
                            catch
                            {
                                item = default(T);
                                Log.Warning("Failed to LookList(mode=" + lookMode + "): " + listLabel + "[" + typeof(T).ToString() + "]\nSubnode:\n" + subNode.OuterXml);
                            }
                            if (errMode != ErrMode.Skip || !object.Equals(item, default(T)))
                            {
                                list.Add(item);
                            }
                        }
                    }
                    else if (lookMode == LookMode.MapReference)
                    {
                        List<string> targetLoadIDList = new List<string>();
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            targetLoadIDList.Add(subNode.InnerText);
                        }
                        LoadIDsWantedBank.RegisterLoadIDListReadFromXml(targetLoadIDList);
                    }
                }
            }
            else if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                if (lookMode == LookMode.MapReference)
                {
                    list = LoadCrossRefHandler.NextResolvedRefList<T>();
                }
                else if ((lookMode == LookMode.TargetPack) && (list != null))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        Thing thing = LoadCrossRefHandler.NextResolvedRef<Thing>();
                        TargetPack pack3 = (TargetPack)(System.Object)list[i];
                        IntVec3 loc = pack3.Loc;
                        if (thing != null)
                        {
                            list[i] = (T)(System.Object)new TargetPack(thing);
                        }
                        else
                        {
                            list[i] = (T)(System.Object)new TargetPack(loc);
                        }
                    }
                }
            }
        }

        public static void LookDeepNotNull<T>(ref T target, string label, params object[] ctorArgs) where T : Saveable
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (target == null)
                {
                    Scribe.EnterNode(label);
                    Scribe.WriteAttribute("IsNull", "True");
                    Scribe.ExitNode();
                }
                else
                {
                    Scribe.EnterNode(label);
                    if (target.GetType() != typeof(T))
                    {
                        Scribe.WriteAttribute("Class", GenTypes.GetTypeNameWithoutIgnoredNamespaces(target.GetType()));
                    }
                    target.ExposeData();
                    Scribe.ExitNode();
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                try
                {
                    target = Scribe_Fix.SaveableFromNode<T>(Scribe.curParent[label], ctorArgs);
                }
                catch
                {
                    Log.Warning("Bad data for " + label + ": " + typeof(T) + "\nSubnode:\n" + Scribe.curParent[label]);
                    //bad data, clean
                    if (target == null)
                    {
                        Log.Warning("Use default");
                        target = (T)((ctorArgs == null || ctorArgs.Length == 0) ? Activator.CreateInstance(typeof(T)) : Activator.CreateInstance(typeof(T), ctorArgs));
                        //target = (T)Activator.CreateInstance(typeof(T), ctorArgs);
                    }
                }
            }
        }

        public static void LookDictionary<K, V>(ref Dictionary<K, V> dict, string dictLabel, LookMode keyLookMode = LookMode.Undefined, LookMode valueLookMode = LookMode.Undefined)
            where K : new()
            where V : new()
        {
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
            {
                Scribe.EnterNode(dictLabel);
                List<K> list = new List<K>();
                List<V> list2 = new List<V>();
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    foreach (KeyValuePair<K, V> pair in dict)
                    {
                        list.Add(pair.Key);
                        list2.Add(pair.Value);
                    }
                }
                LookList<K>(ref list, "keys", keyLookMode, ErrMode.UseDefault, null);
                LookList<V>(ref list2, "values", valueLookMode, ErrMode.UseDefault, null);
                if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    dict.Clear();
                    for (int i = 0; i < list.Count; i++)
                    {
                        //Skip invalid keys
                        if (list[i] != null)//Object.Equals
                            dict.Add(list[i], list2[i]);
                    }
                }
                Scribe.ExitNode();
            }
        }


#if false
        public static void LookValue<T>(ref T value, string label, T defaultValue = default(T), bool forceSave = false)
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (typeof(T) == typeof(TargetPack))
                {
                    Log.Error("TargetPacks must be saved with LookTarget.");
                }
                else if (typeof(Saveable).IsAssignableFrom(typeof(T)))
                {
                    Debug.LogWarning("Using Look with a Saveable reference (" + ((T)value) + "). Use LookSaveable instead.");
                }
                else if (typeof(Thing).IsAssignableFrom(typeof(T)))
                {
                    Debug.LogWarning("Using Look with a Thing reference (" + ((T)value) + "). Use LookThingRef instead.");
                }
                else if ((forceSave || ((((T)value) == null) && (defaultValue != null))) || ((((T)value) != null) && !value.Equals(defaultValue)))
                {
                    Scribe.WriteElement(label, value.ToString());
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                value = ScribeExtractor.ValueFromNode<T>(Scribe.curParent[label], defaultValue);
            }
        }
#endif
        private static readonly Dictionary<Type, Action<Saveable>> exposeDataOveride = new Dictionary<Type, Action<Saveable>>();
        public static readonly HashSet<Assembly> newMods = new HashSet<Assembly>();
		private static readonly Func<ResearchManager, Dictionary<ResearchProjectDef, float>> getProgress;
		private static readonly Func<MapFileCompressor, string> getCompressedString;
        private static readonly Func<Faction, List<FactionRelation>> getRelations;

        private static readonly HashSet<Assembly> modCache = new HashSet<Assembly>();
        private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        static Scribe_Fix()
        {
            ParameterExpression expression;
            Scribe_Fix.getProgress = Expression.Lambda<Func<ResearchManager, Dictionary<ResearchProjectDef, float>>>(Expression.Field(expression = Expression.Parameter(typeof(ResearchManager), "rm"), "progress"), new ParameterExpression[]
	        {
		        expression
	        }).Compile();
            Scribe_Fix.getCompressedString = Expression.Lambda<Func<MapFileCompressor, string>>(Expression.Field(expression = Expression.Parameter(typeof(MapFileCompressor), "mfc"), "compressedString"), new ParameterExpression[]
	        {
		        expression
	        }).Compile();
            Scribe_Fix.getRelations = Expression.Lambda<Func<Faction, List<FactionRelation>>>(Expression.Field(expression = Expression.Parameter(typeof(Faction), "f"), "relations"), new ParameterExpression[]
	        {
		        expression
	        }).Compile();
            Scribe_Fix.exposeDataOveride.Add(typeof(ResearchManager), new Action<Saveable>(Scribe_Fix.ExposeData4ResearchManager));
            Scribe_Fix.exposeDataOveride.Add(typeof(TerrainGrid), new Action<Saveable>(Scribe_Fix.ExposeData4TerrainGrid));
        }

        public static T SaveableFromNode<T>(XmlNode subNode, object[] ctorArgs)
        {
            T local;
            if (subNode == null)
            {
                return default(T);
            }
            XmlAttribute attribute = subNode.Attributes["IsNull"];
            if ((attribute != null) && (attribute.Value == "True"))
            {
                return default(T);
            }
            Tuple<int, int, int> state = null;
            try
            {
                Type typeInAnyAssembly = null;
                XmlAttribute attribute2 = subNode.Attributes["Class"];
                if (attribute2 != null)
                {
                    typeInAnyAssembly = Scribe_Fix.GetTypeFast(attribute2.Value);
                    if (typeInAnyAssembly == null)
                    {
                        Log.Warning(attribute2.Value + " is not found.");
                        var element = subNode["def"];
                        if (element != null)
                        {
                            //Log.Warning("x" + element.InnerText);
                            //Log.Warning("x" + element.Value);
                            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(element.InnerText);
                            if (def != null)
                            {
                                Log.Warning("But lucky, it seems that we can still make this " + def.label + "[" + def.defName + "] anyway." );
                                typeInAnyAssembly = def.thingClass;
                            }
                        }
                    }
                }
                else
                {
                    typeInAnyAssembly = typeof(T);
                }
                //Can not make raw things
                //typeInAnyAssembly = typeInAnyAssembly ?? typeof(T);
                if (typeInAnyAssembly == null)
                {
                    throw new ArgumentNullException("classType");
                }

                T local2 = (T)((ctorArgs == null || ctorArgs.Length == 0) ? Activator.CreateInstance(typeInAnyAssembly) : Activator.CreateInstance(typeInAnyAssembly, ctorArgs));
                XmlNode curParent = Scribe.curParent;
                Scribe.curParent = subNode;
                //不能随意条件此元素的位置, 对ExposeData中引用了LookMode.TargetPack或者LookReference的情况, 最终解析顺序必须和原始队列完全一致
                state = LoadCrossRefHandlerEx.SaveState();
                LoadCrossRefHandler.RegisterLoadedSaveable((Saveable)local2);
                try
                {
                    Action<Saveable> func;
                    if (!exposeDataOveride.TryGetValue(typeInAnyAssembly, out func) || func == null)
                    {
                        ((Saveable)local2).ExposeData();
                    }
                    else
                    {
                        func((Saveable)local2);
                    }
                }
                catch
                {
                    Log.Warning("ExposeData failed.");
                    throw;
                }
                finally
                {
                    //restore
                    Scribe.curParent = curParent;
                }
                PostLoadInitter.RegisterForPostLoadInit((Saveable)local2);
                local = local2;
            }
            catch (Exception e)
            {
                if (state != null)
                    LoadCrossRefHandlerEx.RestoreState(state);
                Log.Notify_Exception(e);
                local = default(T);
                object[] objArray1 = new object[] { "SaveableFromNode exception.\nSubnode:\n", subNode.OuterXml };
                throw new InvalidOperationException(string.Concat(objArray1));
            }
            return local;
        }

        public static void Fix(this FactionManager fm)
        {
            //Fix faction
            var facts = fm.AllFactionsListForReading;
            //Log.Message("OLD");
            //foreach (Faction cur in facts)
            //{
            //    Log.Message(String.Format("{0}[{1}]", cur.GetUniqueLoadID(), cur.def.label));
            //}
            int count = facts.RemoveAll(fact => fact == null || fact.def == null);
            //Log.Message("REMOVED");
            //foreach (Faction cur in facts)
            //{
            //    Log.Message(String.Format("{0}[{1}]", cur.GetUniqueLoadID(), cur.def.label));
            //}
            //Log.Message("NEW");
            IEnumerable<Faction> newFacts = FactionGenerator.GenerateFactionsForNewWorld();
            foreach (Faction cur in newFacts)
            {
                if (!facts.Any(fact => fact.def == cur.def))
                {
                    Log.Message("Add missing faction: " + cur.GetUniqueLoadID() + "[" + cur.def.label + "]");
                    //fm.Add(cur);
                    facts.Add(cur);
                    Scribe_Fix.newMods.Add(cur.GetType().Assembly);
                    ++count;
                    //先注释掉, 解析的时候会解析到别的东西上
                    //LoadCrossRefHandler.RegisterLoadedSaveable(cur);
                }
                //Log.Message(String.Format("{0}[{1}]", cur.GetUniqueLoadID(), cur.def.label));
            }
            //Log.Message("FINAL");

            //NOTE: 由于FactionRelation的ExposeDate涉及到other的引用, 在ResolveAllCrossReferences之前未解析, 任何对LoadCrossRefHandler的变动会导致其执行顺序改变出错
            //Remove useless Relations
            foreach (Faction cur in facts)
            {
                //Log.Message(String.Format("{0}[{1}]", cur.GetUniqueLoadID(), cur.def.label));
                var relations = getRelations(cur);
                for (int i = relations.Count - 1; i >= 0; --i)
                {
                    FactionRelation curRat = relations[i];
                    if ((curRat.other == null) || !facts.Contains(curRat.other))
                    {
                        relations.RemoveAt(i);
                        //注意为null的情况
                        if (curRat.other != null)
                        {
                            Log.Message(String.Format("remove {0}[{1}]->{2}[{3}]", cur.GetUniqueLoadID(), cur.def.label, curRat.other.GetUniqueLoadID(), curRat.other.def.label));
                        }
                        else
                        {
                            Log.Message(String.Format("remove {0}[{1}]->unknown", cur.GetUniqueLoadID(), cur.def.label));
                        }
                    }
                }
                //按理说GenerateFactionsForNewWorld会在yield之前和已添加到地图中的势力建立关系, 但
                //可能在Scribe_Collections.LookList<Faction>(ref this.allFactions, "allFactions", LookMode.Deep, null);时直接就崩溃了, 
                //没有留下记录, 这会造成后续的原势力关系为空, 无法继续
                foreach (Faction other in facts)
                {
                    if (other != cur)
                    {
                        cur.TryMakeInitialRelationsWith(other);
                    }
                }
            }
        }


        public static void Fix(this IEnumerable<Thing> pawnContainer)
        {
            //Fix Pawn.story.hairDef
            foreach (Thing thing in pawnContainer)
            {
                Pawn cur = thing as Pawn;
                if (cur != null)
                {
                    //hair
                    if (cur.story != null && cur.story.hairDef == null)
                    {
                        cur.story.hairDef = PawnHairChooser.RandomHairDefFor(cur, cur.Faction.def);
                        Log.Message("Add hair for " + cur.Nickname);
                    }
                    //faction

                    //cloth
                    
                    //attach
                }
            }
        }



        [Obsolete]
        public static void Fix(this ListerPawns lp)
        {
            foreach (Pawn pawn in lp.AllPawns)
            {
                Log.Message(pawn.story.hairDef.defName);
                if (pawn.story.hairDef == null)
                {
                    pawn.story.hairDef = PawnHairChooser.RandomHairDefFor(pawn, pawn.Faction.def);
                    Log.Message("Add hair for " + pawn.Name);
                }
            }
        }
        private static void ExposeData4ResearchManager(Saveable saveable)
        {
            ResearchManager researchManager = (ResearchManager)saveable;
            Scribe_Defs.LookDef<ResearchProjectDef>(ref researchManager.currentProj, "currentProj");
            Dictionary<ResearchProjectDef, float> dictionary = Scribe_Fix.getProgress(researchManager);
            Scribe_Fix.LookDictionary<ResearchProjectDef, float>(ref dictionary, "progress", LookMode.DefReference, LookMode.Value);
        }
        private static void ExposeData4TerrainGrid(Saveable saveable)
        {
            TerrainGrid tg = (TerrainGrid)saveable;
            string compressedString = string.Empty;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                compressedString = GridSaveUtility.CompressedStringForShortGrid((IntVec3 c) => tg.grid[CellIndices.CellToIndex(c)].shortHash);
            }
            Scribe_Values.LookValue<string>(ref compressedString, "terrainGridCompressed", null, false);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Dictionary<ushort, TerrainDef> dictionary = new Dictionary<ushort, TerrainDef>();
                foreach (TerrainDef current in DefDatabase<TerrainDef>.AllDefs)
                {
                    dictionary.Add(current.shortHash, current);
                }
                TerrainDef terrainDef = TerrainDef.Named("Sand");
                foreach (GridSaveUtility.LoadedGridShort current2 in GridSaveUtility.LoadedUShortGrid(compressedString))
                {
                    TerrainDef terrainDef2 = null;
                    if (!dictionary.TryGetValue(current2.val, out terrainDef2) || terrainDef2 == null)
                    {
                        string[] array = new string[5];
                        array[0] = "Did not find terrain def with short hash ";
                        string[] arg_FE_0 = array;
                        int arg_FE_1 = 1;
                        ushort val = current2.val;
                        arg_FE_0[arg_FE_1] = val.ToString();
                        array[2] = " for square ";
                        string[] arg_121_0 = array;
                        int arg_121_1 = 3;
                        IntVec3 pos = current2.pos;
                        arg_121_0[arg_121_1] = pos.ToString();
                        array[4] = ".";
                        Log.Error(string.Concat(array));
                        if (dictionary.TryGetValue((ushort)(current2.val - 1), out terrainDef2) && terrainDef2 != null)
                        {
                            Log.Warning("Try another hash, found " + terrainDef2.label + terrainDef2.shortHash.ToString());
                        }
                        else
                        {
                            if (dictionary.TryGetValue((ushort)(current2.val + 1), out terrainDef2) && terrainDef2 != null)
                            {
                                Log.Warning("Try another hash, found " + terrainDef2.label + terrainDef2.shortHash.ToString());
                            }
                            else
                            {
                                Log.Warning("Not found, Use " + terrainDef2.label + terrainDef2.shortHash.ToString() + "instead.");
                                terrainDef2 = terrainDef;
                            }
                        }
                        dictionary[current2.val] = terrainDef2;
                    }
                    tg.grid[CellIndices.CellToIndex(current2.pos)] = terrainDef2;
                }
            }
        }

        public static IEnumerable<Thing> ThingsToSpawnAfterLoad(MapFileCompressor compressor)
        {
            DeepProfiler.Start("calc hash map.");
            Dictionary<ushort, ThingDef> dictionary = new Dictionary<ushort, ThingDef>();
            foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs)
            {
                if (dictionary.ContainsKey(current.shortHash))
                {
                    Log.Error(string.Concat(new string[]
			        {
				        "Hash collision between ",
				        current.label,
				        " and  ",
				        dictionary[current.shortHash].label,
				        ": both have short hash ",
				        current.shortHash.ToString()
			        }));
                }
                else
                {
                    dictionary.Add(current.shortHash, current);
                }
            }
            DeepProfiler.End("calc hash map.");
            ThingDef[] array = new ThingDef[]
	        {
		        ThingDef.Named("Sandstone"),
		        ThingDef.Named("Limestone"),
		        ThingDef.Named("Granite"),
		        ThingDef.Named("Slate"),
		        ThingDef.Named("Marble")
	        };
            DeepProfiler.Start("LoadedUShortGrid");
            IEnumerable<GridSaveUtility.LoadedGridShort> enumerable = GridSaveUtility.LoadedUShortGrid(Scribe_Fix.getCompressedString(compressor));
            DeepProfiler.End("LoadedUShortGrid");
            DeepProfiler.Start("init");
            foreach (GridSaveUtility.LoadedGridShort current2 in enumerable)
            {
                if (current2.val != 0)
                {
                    ThingDef thingDef = null;
                    if (!dictionary.TryGetValue(current2.val, out thingDef) || thingDef == null)
                    {
                        Log.Warning("Map compressor decompression error: No thingDef with short hash " + current2.val);
                        if (dictionary.TryGetValue((ushort)(current2.val - 1), out thingDef) && thingDef != null)
                        {
                            Log.Warning("Try another hash, found " + thingDef.label + thingDef.shortHash.ToString());
                        }
                        else
                        {
                            if (dictionary.TryGetValue((ushort)(current2.val + 1), out thingDef) && thingDef != null)
                            {
                                Log.Warning("Try another hash, found " + thingDef.label + thingDef.shortHash.ToString());
                            }
                            else
                            {
                                thingDef = array[(int)current2.val % array.Length];
                                Log.Warning("Not found, Use " + thingDef.label + thingDef.shortHash.ToString() + "instead.");
                            }
                        }
                        dictionary[current2.val] = thingDef;
                    }
                    if (thingDef != null)
                    {
                        Thing thing = ThingMaker.MakeThing(thingDef, null);
                        thing.SetPositionDirect(current2.pos);
                        yield return thing;
                    }
                }
            }
            DeepProfiler.End("init");
            yield break;
        }
        private static Type GetTypeFast(string name)
        {
            Type type;
            if (name == null)
            {
                type = null;
            }
            else
            {
                if (!Scribe_Fix.typeCache.TryGetValue(name, out type))
                {
                    type = GenTypes.GetTypeInAnyAssembly(name);
                    Scribe_Fix.typeCache[name] = type;
                    if (type != null)
                    {
                        Scribe_Fix.modCache.Add(type.Assembly);
                    }
                }
            }
            return type;
        }
        public static void Clean()
        {
            Scribe_Fix.typeCache.Clear();
            Scribe_Fix.modCache.Clear();
        }
        public static void CheckMapGen()
        {
            try
            {
                MapGeneratorDef random = DefDatabase<MapGeneratorDef>.GetRandom();
                if (random != null)
                {
                    bool flag = false;
                    foreach (Genstep current in random.gensteps)
                    {
                        if (!Scribe_Fix.modCache.Contains(current.GetType().Assembly))
                        {
                            current.Generate();
                            flag = true;
                        }
                    }
                    if (flag)
                    {
                        Log.Message("The world is changing all the time, I will give you some things.");
                    }
                }
            }
            catch
            {
            }
        }


    }
}
