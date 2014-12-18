using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEngine;
using Verse;

namespace TransFix.Extends
{
    public static class StorageSettingsEx
    {
        private static readonly Func<ThingFilter, List<SpecialThingFilterDef>> getDisallowedSpecialFilters;
        static StorageSettingsEx()
        {
            ParameterExpression expression;
            getDisallowedSpecialFilters = Expression.Lambda<Func<ThingFilter, List<SpecialThingFilterDef>>>(
                Expression.Field(expression = Expression.Parameter(typeof(ThingFilter), "that"), "disallowedSpecialFilters"),
                new ParameterExpression[] { expression })
                .Compile();
        }

        public static void ExposeDataEx(this StorageSettings that)
        {
            Scribe_Values.LookValue<StoragePriority>(ref that.priority, "priority", StoragePriority.Unstored, false);
            Scribe_Fix.LookDeepNotNull<ThingFilter>(ref that.allowances, "allowances");
        }

        public static void ExposeDataEx(this ThingFilter that)
        {
            that.ExposeData();
            var disallowedSpecialFilters = getDisallowedSpecialFilters(that);
            Scribe_Fix.LookListNotNull<SpecialThingFilterDef>(ref disallowedSpecialFilters, "disallowedSpecialFilters", LookMode.DefReference, null);
            //FIXME: set 

            var allowedDefs = (HashSet<ThingDef>)that.AllowedThingDefs;
            Scribe_Fix.LookHashSet<ThingDef>(ref allowedDefs, "allowedDefs");
        }

    }
}
