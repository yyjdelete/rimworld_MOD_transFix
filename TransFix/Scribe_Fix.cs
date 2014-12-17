using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using TransFix.Extends;
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

        private static readonly HashSet<Assembly> modCache = new HashSet<Assembly>();
        private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        static Scribe_Fix()
        {
            //BindingFlags bindFlag = BindingFlags.Instance | 
            //    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            //var progressInfo = typeof(ResearchManager).GetField("progress", bindFlag);

            //faster than reflection


            exposeDataOveride.Add(typeof(ResearchManager), (saveable) => ((ResearchManager)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(MapConditionManager), (saveable) => ((MapConditionManager)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(MapInfo), (saveable) => ((MapInfo)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(TerrainGrid), (saveable) => ((TerrainGrid)saveable).ExposeDataEx());
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

            //var oilDepositDef = DefDatabase<ThingDef>.GetNamedSilentFail("OilDeposit");
            //if (oilDepositDef != null &&
            //    !Find.ListerThings.ThingsOfDef(oilDepositDef).Any())//!TTM_GameStartSpawns for still loading
            //{
            //}
            try
            {
                var defaultGen = DefDatabase<MapGeneratorDef>.GetRandom();
                if (defaultGen != null)
                {
                    bool found = false;
                    //hack: TTMCustomEvents.Initializer init for TTMa7 while they are not the same
                    Type ttmceInit = GenTypes.GetTypeInAnyAssembly("TTMCustomEvents.Initializer");
                    foreach (var cur in defaultGen.gensteps)
                    {
                        var curType = cur.GetType();
                        if (!modCache.Contains(cur.GetType().Assembly))
                        {
                            bool gen = true;
                            if (curType == ttmceInit)
                            {
                                ThingDef oilDef = DefDatabase<ThingDef>.GetNamedSilentFail("OilDeposit");
                                if (oilDef != null)
                                {
                                    Type oilClass = oilDef.thingClass;
                                    if (oilClass != null)
                                    {
                                        gen = !modCache.Contains(oilClass.Assembly);
                                    }
                                    else
                                    {
                                        gen = !Find.ListerThings.ThingsOfDef(oilDef).Any();
                                    }
                                }
                            }
                            if (gen)
                            {
                                cur.Generate();
                                found = true;
                            }
                        }
                    }
                    if (found)
                        Log.Message("The world is changing all the time, I will give you some things.");
                }
            }
            catch { }
        }


    }
}
