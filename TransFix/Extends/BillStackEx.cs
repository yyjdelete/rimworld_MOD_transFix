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
    public static class BillStackEx
    {
        private static Func<BillStack, List<Bill>> getBills;

        static BillStackEx()
        {
            Type curType = typeof(BillStack);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var fi = curType.GetField("bills", flags);
            var methodBldr = new DynamicMethod("", typeof(List<Bill>), new[] { curType }, curType, true);
            var il = methodBldr.GetILGenerator();//获取il生成器
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fi);
            il.Emit(OpCodes.Ret);
            getBills = (Func<BillStack, List<Bill>>)methodBldr.CreateDelegate(typeof(Func<BillStack, List<Bill>>));
        }

        public static List<Bill> GetBills(this BillStack that)
        {
            return (that != null) ? getBills(that) : null;
        }
    }
}
