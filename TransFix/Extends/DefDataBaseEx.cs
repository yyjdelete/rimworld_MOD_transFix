using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class DefDatabaseEx<T> where T : global::Verse.Def, new()
    {
        private static readonly Func<Dictionary<string, T>> getDefsByName;
        static DefDatabaseEx()
        {
            BindingFlags bindFlag = BindingFlags.Static | BindingFlags.NonPublic;
            var fi = typeof(DefDatabase<T>).GetField("defsByName", bindFlag);
            getDefsByName = Expression.Lambda<Func<Dictionary<string, T>>>(
                Expression.Field(null, fi),
                null)
                .Compile();
        }

        public static Dictionary<string, T> DefsByName { get { return getDefsByName(); } }
    }
}
