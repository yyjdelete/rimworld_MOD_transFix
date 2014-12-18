using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using TransFix.Extends;
using UnityEngine;
using Verse;
using Verse.AI;

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
                            Scribe_Values.LookValue<T>(ref local2, XmlToObject.ListItemName, defaultValue, true);
                        }
                        else if (lookMode == LookMode.TargetPack)
                        {
                            TargetPack pack = (TargetPack)(System.Object)local;
                            Scribe_TargetPack.LookTargetPack(ref pack, XmlToObject.ListItemName);
                        }
                        else if (lookMode == LookMode.DefReference)
                        {
                            Def def = (Def)(System.Object)local;
                            Scribe_Defs.LookDef<Def>(ref def, XmlToObject.ListItemName);
                        }
                        else if (lookMode == LookMode.Deep)
                        {
                            Saveable target = (Saveable)local;
                            Scribe_Deep.LookDeep<Saveable>(ref target, XmlToObject.ListItemName, ctorArgs);
                        }
                        else if (lookMode == LookMode.MapReference)
                        {
                            LoadReferenceable refee = (LoadReferenceable)local;
                            Scribe_References.LookReference<LoadReferenceable>(ref refee, XmlToObject.ListItemName);
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
                    Log.Warning("List null change, maybe wrong if no-ref is used.");
                    list = null;
                }
                else
                {
                    XmlAttribute attribute = node.Attributes["IsNull"];
                    if ((attribute != null) && ("true".Equals(attribute.Value, StringComparison.OrdinalIgnoreCase)))
                    {
                        Log.Warning("List null change, maybe wrong if no-ref is used.");
                        list = null;
                    }
                    else if (lookMode == LookMode.Value)
                    {
                        list = AllocList<T>(list, node.ChildNodes.Count);
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            T item;
                            try
                            {
                                item = ScribeExtractor.ValueFromNode<T>(subNode, default(T));
                            }
                            catch (Exception e)
                            {
                                Log.Notify_Exception(e);
                                item = default(T);
                                Log.Warning("Failed to LookList(mode=" + lookMode + "): " + listLabel + "[" + typeof(T).ToString() + "]\nSubnode:\n" + subNode.OuterXml);
                            }
                            //仅在模式不为Skip或Skip且不为默认值的情况下成立
                            if (errMode != ErrMode.Skip || !object.Equals(item, default(T)))
                            {
                                list.Add(item);
                            }
                        }
                    }
                    else if (lookMode == LookMode.Deep)
                    {
                        list = AllocList<T>(list, node.ChildNodes.Count);
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
                                Log.Notify_Exception(e);
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
                        list = AllocList<T>(list, node.ChildNodes.Count);
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            //Def
                            T item;
                            try
                            {
                                item = ScribeExtractor.DefFromNodeUnsafe<T>(subNode);//DefFromNodeUnsafe == DefFromNode
                            }
                            catch (Exception e)
                            {
                                Log.Notify_Exception(e);
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
                        list = AllocList<T>(list, node.ChildNodes.Count);
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            T item;
                            try
                            {
                                item = (T)(System.Object)ScribeExtractor.TargetPackFromNode(subNode, TargetPack.Invalid);
                            }
                            catch (Exception e)
                            {
                                Log.Notify_Exception(e);
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
                    if (list != null)
                        list.Capacity = list.Count;//比TrimExcess极端
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
                        IntVec3 cell = pack3.Cell;
                        if (thing != null)
                        {
                            list[i] = (T)(System.Object)new TargetPack(thing);
                        }
                        else
                        {
                            list[i] = (T)(System.Object)new TargetPack(cell);
                        }
                    }
                }
            }
        }

        private static List<T> AllocList<T>(List<T> orig, int count)
        {
            if (orig == null)
            {
                Log.Warning("List null change, maybe wrong if no-ref is used.");
                orig = new List<T>(count);
            }
            else
            {
                orig.Clear();
                orig.Capacity = count;
            }
            return orig;
        }

        public static void LookDeepNotNull<T>(ref T target, string label, params object[] ctorArgs) where T : Saveable
        {
            LookDeepNotNullWithDef(ref target, label, null, ctorArgs);
        }
        public static void LookDeepNotNullWithDef<T>(ref T target, string label, Func<T> def, params object[] ctorArgs) where T : Saveable
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
                    target = Scribe_Fix.SaveableFromNode<T>(Scribe.curParent[label], ctorArgs, false);
                }
                catch
                {
                    Log.Warning("Bad data for " + label + ": " + typeof(T) + "\nSubnode:\n" + ((Scribe.curParent[label] != null) ? Scribe.curParent[label].OuterXml : "<null>"));
                    //bad data, clean
                    if (target == null)
                    {
                        if (def == null)
                        {
                            Log.Warning("Use new");
                            target = (T)((ctorArgs == null || ctorArgs.Length == 0) ? Activator.CreateInstance(typeof(T)) : Activator.CreateInstance(typeof(T), ctorArgs));
                            //target = (T)Activator.CreateInstance(typeof(T), ctorArgs);
                        }
                        else
                        {
                            Log.Warning("Use default");
                            target = def();
                        }
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
                List<K> list = new List<K>(dict.Count);
                List<V> list2 = new List<V>(dict.Count);
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

        public static void LookHashSet<T>(ref HashSet<T> valueHashSet, string listLabel) where T : Def, new()
        {
            List<T> list = (Scribe.mode == LoadSaveMode.Saving) ? valueHashSet.ToList() : new List<T>();

            Scribe_Fix.LookListNotNull<T>(ref list, listLabel, LookMode.Undefined, null);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                valueHashSet.Clear();
                valueHashSet.UnionWith(list);
                valueHashSet.TrimExcess();
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


        static Scribe_Fix()
        {
            RegistExposeMap();
        }

        private static void RegistExposeMap()
        {
            exposeDataOveride.Add(typeof(ResearchManager), (saveable) => ((ResearchManager)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(MapConditionManager), (saveable) => ((MapConditionManager)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(MapInfo), (saveable) => ((MapInfo)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(TerrainGrid), (saveable) => ((TerrainGrid)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(ZoneManager), (saveable) => ((ZoneManager)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(Zone), (saveable) => ((Zone)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(Zone_Growing), (saveable) => ((Zone_Growing)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(Zone_Stockpile), (saveable) => ((Zone_Stockpile)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(Pawn), (saveable) => ((Pawn)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(Pawn_HealthTracker), (saveable) => ((Pawn_HealthTracker)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(Pawn_PathFollower), (saveable) => ((Pawn_PathFollower)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(StorageSettings), (saveable) => ((StorageSettings)saveable).ExposeDataEx());
            exposeDataOveride.Add(typeof(ThingFilter), (saveable) => ((ThingFilter)saveable).ExposeDataEx());
        }

        public static T SaveableFromNode<T>(XmlNode subNode, object[] ctorArgs, bool ignoreEmpty = true)
        {
            T local;
            if (subNode == null)//maybe new version
            {
                Log.Warning("empty node??");
                throw new InvalidOperationException("empty node!!");
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
                    typeInAnyAssembly = ModUtils.GetTypeFast(attribute2.Value);
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
                else if (typeof(Thing).IsAssignableFrom(typeInAnyAssembly))
                {
                    ThingDef def = null;
                    var element = subNode["def"];
                    if (element != null)
                    {
                        //Log.Warning("x" + element.InnerText);
                        //Log.Warning("x" + element.Value);
                        def = DefDatabase<ThingDef>.GetNamedSilentFail(element.InnerText);
                    }
                    if (def == null)
                    {
                        var msg = "def is not found " + ((element != null) ? element.InnerText : "<unknown>");
                        Log.Message(msg);
                        throw new InvalidOperationException(msg);
                    }
                }


                T local2 = (T)((ctorArgs == null || ctorArgs.Length == 0) ? Activator.CreateInstance(typeInAnyAssembly) : Activator.CreateInstance(typeInAnyAssembly, ctorArgs));
                XmlNode curParent = Scribe.curParent;
                Scribe.curParent = subNode;
                //不能随意调节此元素的位置, 对ExposeData中引用了LookMode.TargetPack或者LookReference的情况, 最终解析顺序必须和原始队列完全一致
                state = LoadCrossRefHandlerEx.SaveState();
                try
                {
                    LoadCrossRefHandler.RegisterLoadedSaveable((Saveable)local2);
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
                catch(Exception e)
                {
                    Log.Warning(e.ToString());
                    Log.Warning("ExposeData failed." + subNode.OuterXml);
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
                throw new InvalidOperationException(string.Concat(objArray1), e);
            }
            return local;
        }
        public static HashSet<Assembly> newMods = new HashSet<Assembly>();

        public static void Fix(this IEnumerable<Thing> pawnContainer)
        {
            //Fix Pawn.story.hairDef
            foreach (Thing thing in pawnContainer)
            {
                Pawn cur = thing as Pawn;
                if (cur != null)
                {
                    //if (!cur.RaceProps.humanoid)
                    //    continue;
                    //hair
                    //Log.Message(cur.Nickname);
                    //Log.Message("622" + cur.story);
                    if (cur.story != null && cur.story.hairDef == null)
                    {
                        cur.story.hairDef = PawnHairChooser.RandomHairDefFor(cur, cur.Faction.def);
                        Log.Message("Add hair for " + cur.Nickname);
                    }
                    //faction

                    //cloth
                    
                    //attach

                    //jobs
                    var jobs = cur.jobs;
                    if (jobs != null && jobs.curJob != null)
                    {
                        //if (jobs.curJob.targetA == null)
                        //{
                        //    jobs.EndCurrentJob(JobCondition.Errored);
                        //}

                    }

                    if (cur.healthTracker != null)
                    {
                        var bodyModel = cur.healthTracker.bodyModel;
                        if (bodyModel != null)
                        {
                            bodyModel.healthDiffs.RemoveAll(diff =>
                            {
                                return diff.def == null ||
                                    diff.body == null ||
                                    diff.source == null ||
                                    diff.sourceBodyPartGroup == null ||
                                    diff.sourceHealthDiffDef == null;
                            });
                        }
                    }

                    if (cur.psychology != null)
                    {
                        cur.psychology.Fix();
                    }
                }
            }
        }

    }
}
//Environment.StackTrace;