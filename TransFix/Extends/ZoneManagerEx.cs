using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class ZoneManagerEx
    {
        private static readonly Func<ZoneManager, List<Zone>> getAllZones;
        private static readonly Action<ZoneManager> callRebuildZoneGrid;
        static ZoneManagerEx()
        {
            ParameterExpression expression;
            getAllZones = Expression.Lambda<Func<ZoneManager, List<Zone>>>(
                Expression.Field(expression = Expression.Parameter(typeof(ZoneManager), "zm"), "allZones"),
                new ParameterExpression[] { expression })
                .Compile();
            
            BindingFlags bindFlag = BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            MethodInfo info = typeof(ZoneManager).GetMethod("RebuildZoneGrid", bindFlag);
            callRebuildZoneGrid = Expression.Lambda<Action<ZoneManager>>(
                Expression.Call(expression = Expression.Parameter(typeof(ZoneManager), "zm"), info),
                new ParameterExpression[] { expression })
                .Compile();
        }

        public static void RebuildZoneGrid(this ZoneManager zm)
        {
            callRebuildZoneGrid(zm);
        }
        public static void ExposeDataEx(this ZoneManager zm)
        {
            var allZones = getAllZones(zm);
            Scribe_Fix.LookListNotNull<Zone>(ref allZones, "allZones", LookMode.Deep, null);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                callRebuildZoneGrid(zm);
            }
        }

    }
}
