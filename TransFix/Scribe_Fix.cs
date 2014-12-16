using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace TransFix
{
    public static class Scribe_Fix
    {
        //skip empty
        public static void LookList<T>(ref List<T> list, string listLabel, LookMode lookMode, params object[] ctorArgs)
        {
            LookListWithEmpty<T>(ref list, listLabel, lookMode, ctorArgs);
            if (Scribe.mode == LoadSaveMode.LoadingVars && )
            list.Where(t)
        }

        public static void LookListWithEmpty<T>(ref List<T> list, string listLabel, LookMode lookMode, params object[] ctorArgs)
        {
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
                            T item = ScribeExtractor.ValueFromNode<T>(subNode, default(T));
                            list.Add(item);
                        }
                    }
                    else if (lookMode == LookMode.Deep)
                    {
                        list = new List<T>();
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            T item;
                            try
                            {
                                //skip bad and null
                                item = ScribeExtractor.SaveableFromNode<T>(subNode, ctorArgs);
                                //maybe used by dict
                                if (!System.Object.Equals(item, default(T)))
                                    list.Add(item);
                            }
                            catch
                            {
                            }
                        }
                    }
                    else if (lookMode == LookMode.DefReference)
                    {
                        list = new List<T>();
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            T item;
                            try
                            {
                                item = ScribeExtractor.DefFromNodeUnsafe<T>(subNode);//DefFromNodeUnsafe == DefFromNode
                            }
                            catch
                            {
                                item = default(T);
                            }
                            list.Add(item);
                        }
                    }
                    else if (lookMode == LookMode.TargetPack)
                    {
                        list = new List<T>();
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            T item = (T)(System.Object)ScribeExtractor.TargetPackFromNode(subNode, TargetPack.Invalid);
                            list.Add(item);
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

        public static void LookDeep<T>(ref T target, string label) where T : Saveable
        {
            LookDeep<T>(ref target, label, new object[0]);
        }

        public static void LookDeep<T>(ref T target, string label, object[] ctorArgs) where T : Saveable
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
                target = Scribe_Fix.SaveableFromNode<T>(Scribe.curParent[label], ctorArgs);
            }
        }


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
            try
            {
                Type typeInAnyAssembly = null;
                XmlAttribute attribute2 = subNode.Attributes["Class"];
                if (attribute2 != null)
                {
                    typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(attribute2.Value);
                }
                else
                {
                    typeInAnyAssembly = typeof(T);
                }
                if (typeInAnyAssembly == null)
                {
                    throw new ArgumentNullException("classType");
                }
                T local2 = (T)Activator.CreateInstance(typeInAnyAssembly, ctorArgs);
                LoadCrossRefHandler.RegisterLoadedSaveable((Saveable)local2);
                XmlNode curParent = Scribe.curParent;
                Scribe.curParent = subNode;
                ((Saveable)local2).ExposeData();
                Scribe.curParent = curParent;
                PostLoadInitter.RegisterForPostLoadInit((Saveable)local2);
                local = local2;
            }
            catch (Exception exception)
            {
                local = default(T);
                object[] objArray1 = new object[] { "SaveableFromNode exception: ", exception, "\nSubnode:\n", subNode.OuterXml };
                throw new InvalidOperationException(string.Concat(objArray1));
            }
            return local;
        }


    }
}
