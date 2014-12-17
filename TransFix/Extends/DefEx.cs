using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TransFix.Extends
{
    public static class DefEx
    {
        private static Action<Def> clearLabelCapCache;

        static DefEx()
        {
            Type curType = typeof(Def);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var fi = curType.GetField("cachedLabelCap", flags);
            var methodBldr = new DynamicMethod("", null, new[] { curType }, curType, true);
            var il = methodBldr.GetILGenerator();//获取il生成器
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stfld, fi);
            il.Emit(OpCodes.Ret);
            clearLabelCapCache = (Action<Def>)methodBldr.CreateDelegate(typeof(Action<Def>));
        }

        public static void ClearLabelCapCache(this Def def)
        {
            clearLabelCapCache(def);
        }
    }
}
