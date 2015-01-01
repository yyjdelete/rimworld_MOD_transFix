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

namespace TransFix.Extends
{
    public static class ZoneEx
    {
        private static readonly Func<Zone_Growing, ThingDef> getPlantDefToGrow;
        //private static readonly Action<Zone> callZoneExposeData;
        static ZoneEx()
        {
            ParameterExpression expression;
            getPlantDefToGrow = Expression.Lambda<Func<Zone_Growing, ThingDef>>(
                Expression.Field(expression = Expression.Parameter(typeof(Zone_Growing), "zone"), "plantDefToGrow"),
                new ParameterExpression[] { expression })
                .Compile();

            //Type baseType = typeof(Zone);
            //BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            //var mi = baseType.GetMethod("ExposeData", flags);
            //var methodBldr = new DynamicMethod("", null, new[] { baseType }, baseType, true);
            //var il = methodBldr.GetILGenerator();//获取il生成器
            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Call, mi);//Not Virt
            //il.Emit(OpCodes.Ret);
            //callZoneExposeData = (Action<Zone>)methodBldr.CreateDelegate(typeof(Action<Zone>));
        }
        public static void ExposeDataEx(this Zone zone)
        {
            //zone.ExposeData();//注意是虚函数
            Scribe_Values.LookValue<string>(ref zone.zoneName, "zoneName", null, false);
            Scribe_Values.LookValue<Color>(ref zone.zoneColor, "zoneColor", default(Color), false);
            Scribe_Values.LookValue<bool>(ref zone.hidden, "hidden", false, false);
            Scribe_Collections.LookList<IntVec3>(ref zone.cells, "cells", LookMode.Value, null);//注意自动补全的null参数类型错误
            //callZoneExposeData(zone);

            zone.FixCells();
        }

        private static void FixCells(this Zone zone)
        {
            if (zone.cells == null)
            {
                //A7
                Scribe_Collections.LookList<IntVec3>(ref zone.cells, "squares", LookMode.Value, null);
                if (zone.cells == null || zone.cells.Count == 0)
                {
                    throw new InvalidOperationException("Zone must have cells!!!");
                }
            }
        }
        public static void ExposeDataEx(this Zone_Growing zone)
        {
            //((Zone)zone).ExposeDataEx();
            //Scribe_Defs.LookDef<ThingDef>(ref zone.plantDefToGrow, "plantDefToGrow");
            zone.ExposeData();

            if (getPlantDefToGrow(zone) == null)
            {
                throw new InvalidOperationException("Unknown plant to grown");
            }
            zone.FixCells();
        }
        public static void ExposeDataEx(this Zone_Stockpile zone)
        {
            ((Zone)zone).ExposeDataEx();
            //callZoneExposeData(zone);
            //zone.FixCells();
            Scribe_Fix.LookDeepNotNull<StorageSettings>(ref zone.settings, "settings", zone);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                zone.slotGroup = new SlotGroup(zone);
            }
        }

    }
}
