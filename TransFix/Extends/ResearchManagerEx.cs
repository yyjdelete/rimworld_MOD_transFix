using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class ResearchManagerEx
    {
        private static readonly Func<ResearchManager, Dictionary<ResearchProjectDef, float>> getProgress;
        //private static readonly Expression<Action<ResearchManager, ResearchProjectDef>> setProgress;

        static ResearchManagerEx()
        {
            ParameterExpression expression;
            getProgress = Expression.Lambda<Func<ResearchManager, Dictionary<ResearchProjectDef, float>>>(
                Expression.Field(expression = Expression.Parameter(typeof(ResearchManager), "rm"), "progress"),
                new ParameterExpression[] { expression })
                .Compile();
        }
        public static void ExposeDataEx(this ResearchManager rm)
        {
            Scribe_Defs.LookDef<ResearchProjectDef>(ref rm.currentProj, "currentProj");
            Dictionary<ResearchProjectDef, float> progress = getProgress(rm);
            Scribe_Fix.LookDictionary<ResearchProjectDef, float>(ref progress, "progress", LookMode.DefReference, LookMode.Value);
            //not needed
            //progress = setProgress(rm, progress);
        }
    }
}
