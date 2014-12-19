using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Verse;

namespace TransFix.Extends
{
    public static class MapDrawerEx
    {
        private static Func<MapDrawer, IntVec2> getNumSects;
        private static Func<MapDrawer, Section[,]> getSections;
        private static Action<MapDrawer, Section[,]> setSections;

        //private static Func<Section, List<SectionLayer>> getSectionLayers;
        private static Action<Section, List<SectionLayer>> setSectionLayers;

        static MapDrawerEx()
        {
            ParameterExpression expression;
            getNumSects = Expression.Lambda<Func<MapDrawer, IntVec2>>(
                Expression.Property(expression = Expression.Parameter(typeof(MapDrawer), "md"), "NumSects"),
                new ParameterExpression[] { expression })
                .Compile();

            BindingFlags bindFlag = BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            FieldInfo sectionsInfo = typeof(MapDrawer).GetField("sections", bindFlag);
            getSections = Expression.Lambda<Func<MapDrawer, Section[,]>>(
                Expression.Field(expression = Expression.Parameter(typeof(MapDrawer), "md"), sectionsInfo),
                new ParameterExpression[] { expression })
                .Compile();
            setSections = (md, sections)=>sectionsInfo.SetValue(md, sections);


            FieldInfo sectionLayersInfo = typeof(Section).GetField("sectionLayers", bindFlag);
            //getSectionLayers = Expression.Lambda<Func<Section, List<SectionLayer>>>(
            //    Expression.Field(expression = Expression.Parameter(typeof(Section), "s"), sectionLayersInfo),
            //    new ParameterExpression[] { expression })
            //    .Compile();
            setSectionLayers = (s, sectionLayers) => sectionLayersInfo.SetValue(s, sectionLayers);
        }
        public static void RegenerateEverythingNowEx(this MapDrawer that)
        {
            var numSects = getNumSects(that);
            int x = numSects.x;
            int z = numSects.z;
            var sections = getSections(that);
            if (sections == null)
            {
                sections = new Section[x, z];
                setSections(that, sections);
            }
            IEnumerable<Func<Section, SectionLayer>> layerFuncs = typeof(SectionLayer).AllLeafSubclasses()
                .Select(t =>
                {
                    ParameterExpression expression;
                    ConstructorInfo constructor = t.GetConstructor(new Type[] { typeof(Section) });
                    return Expression.Lambda<Func<Section, SectionLayer>>(
                        Expression.New(constructor, expression = Expression.Parameter(typeof(Section), "s")),
                        new ParameterExpression[] { expression })
                        .Compile();
                })
                .ToList();
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < z; j++)
                {
                    var cur = sections[i, j];
                    if (cur == null)
                    {
                        sections[i, j] = cur = CreateSection(new IntVec3(i, 0, j), layerFuncs);//new Section
                    }
                    try
                    {
                        cur.RegenerateAllLayers();
                    }
                    catch { }
                }
            }
        }

#if false
        private static Section CreateSection(IntVec3 sectCoords, IEnumerable<Type> layerTypes)
        {
            //Ugly, but no empty init method
            Section res = (Section)FormatterServices.GetUninitializedObject(typeof(Section));


            res.changesThisFrame = MapChangeType.None;
            
            var sectionLayers = new List<SectionLayer>();
            res.botLeft = sectCoords * 0x11;
            foreach (Type current in layerTypes)
            {
                sectionLayers.Add((SectionLayer)Activator.CreateInstance(current, res));
            }
            setSectionLayers(res, sectionLayers);

            return res;
        }
#else
        private static Section CreateSection(IntVec3 sectCoords, IEnumerable<Func<Section, SectionLayer>> layerFuncs)
        {
            //Ugly, but no empty init method
            Section res = (Section)FormatterServices.GetUninitializedObject(typeof(Section));


            res.changesThisFrame = MapChangeType.None;

            var sectionLayers = new List<SectionLayer>();
            res.botLeft = sectCoords * 0x11;
            foreach (var func in layerFuncs)
            {
                sectionLayers.Add(func(res));
            }
            setSectionLayers(res, sectionLayers);

            return res;
        }
#endif
    }
}
/*
 NullReferenceException: Object reference not set to an instance of an object
  at Verse.SectionLayer_FogOfWar.Regenerate () [0x00000] in <filename unknown>:0 

  at Verse.Section.RegenerateAllLayers () [0x00000] in <filename unknown>:0 

  at TransFix.Extends.MapDrawerEx.RegenerateEverythingNowEx (Verse.MapDrawer that) [0x00000] in <filename unknown>:0 

  at TransFix.Extends.MapIniterUtilityEx.FinalizeMapInit () [0x00000] in <filename unknown>:0 

  at TransFix.MapIniter_LoadFromFile.InitMapFromFile (System.String mapFileName) [0x00000] in <filename unknown>:0 

  at TransFix.RootMap.Start () [0x00000] in <filename unknown>:0 */