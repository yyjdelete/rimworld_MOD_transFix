using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class MapConditionManagerEx
    {
        private static readonly Func<MapConditionManager, List<MapCondition>> getActiveConditions;
        static MapConditionManagerEx()
        {
            ParameterExpression expression;
            getActiveConditions = Expression.Lambda<Func<MapConditionManager, List<MapCondition>>>(
                Expression.Field(expression = Expression.Parameter(typeof(MapConditionManager), "mcm"), "activeConditions"),
                new ParameterExpression[] { expression })
                .Compile();
        }
        public static void ExposeDataEx(this MapConditionManager mcm)
        {
            List<MapCondition> activeConditions = getActiveConditions(mcm);
            Scribe_Fix.LookListNotNull<MapCondition>(ref activeConditions, "activeConditions", LookMode.Deep, null);
            //FIXME: set 
        }

    }
}
