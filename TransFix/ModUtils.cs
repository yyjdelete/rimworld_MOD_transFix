using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace TransFix
{
    public static class ModUtils
    {

        private static readonly HashSet<Assembly> modCache = new HashSet<Assembly>();
        private static readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

        public static Type GetTypeFast(string name)
        {
            Type type;
            if (name == null)
                type = null;
            else
            {
                if (!typeCache.TryGetValue(name, out type))
                {
                    type = GenTypes.GetTypeInAnyAssembly(name);
                    typeCache[name] = type;
                    if (type != null)
                    {
                        modCache.Add(type.Assembly);
                    }
                }
            }
            return type;
        }

        public static void Clean()
        {
            typeCache.Clear();
            modCache.Clear();
        }

        public static void CheckMapGen()
        {

            //var oilDepositDef = DefDatabase<ThingDef>.GetNamedSilentFail("OilDeposit");
            //if (oilDepositDef != null &&
            //    !Find.ListerThings.ThingsOfDef(oilDepositDef).Any())//!TTM_GameStartSpawns for still loading
            //{
            //}
            //FIXME: MapGeneratorDef is removed, how to got it??
#if A6
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
#endif
        }

        public static void CheckMapComponent(this Map map)
        {
            //Load new MapComponent, such as MapComponent_ColonistSelections/MapComponent_AddRecipesToPawn in Miscellaneous
            HashSet<Type> loadedComponent = new HashSet<Type>(map.components.Select(mc => mc.GetType()));
            foreach (Type cur in typeof(MapComponent).AllLeafSubclasses())
            {
                if (!loadedComponent.Contains(cur))
                {
                    Log.Message("Add new MapComponent: " + cur);
                    MapComponent item = (MapComponent)Activator.CreateInstance(cur);
                    map.components.Add(item);
                }
            }
        }
    }
}
