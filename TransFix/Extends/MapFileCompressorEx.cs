using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class MapFileCompressorEx
    {
        private static readonly Func<MapFileCompressor, string> getCompressedString;
        static MapFileCompressorEx()
        {
            ParameterExpression expression;
            getCompressedString = Expression.Lambda<Func<MapFileCompressor, string>>(
                Expression.Field(expression = Expression.Parameter(typeof(MapFileCompressor), "mfc"), "compressedString"),
                new ParameterExpression[] { expression })
                .Compile();
        }

        public static IEnumerable<Thing> ThingsToSpawnAfterLoadEx(this MapFileCompressor compressor)
        {
            DeepProfiler.Start("calc hash map.");
            var thingDefsByShortHash = new Dictionary<ushort, ThingDef>();
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDefsByShortHash.ContainsKey(def.shortHash))
                {
                    Log.Error("Hash collision between " + def.label + " and  " + thingDefsByShortHash[def.shortHash].label + ": both have short hash " + def.shortHash.ToString());
                }
                else
                {
                    thingDefsByShortHash.Add(def.shortHash, def);
                }

            }
            DeepProfiler.End("calc hash map.");
            ThingDef[] stones = new ThingDef[]{
                ThingDef.Named("Sandstone"),
                ThingDef.Named("Limestone"),
                ThingDef.Named("Granite"),
                ThingDef.Named("Slate"),
                ThingDef.Named("Marble")};//CollapsedRocks
            //foreach (ThingDef def in stones)
            //{
            //    Log.Message(def.label + def.shortHash);
            //}

            DeepProfiler.Start("LoadedUShortGrid");
            var tmp = GridSaveUtility.LoadedUShortGrid(getCompressedString(compressor));
            DeepProfiler.End("LoadedUShortGrid");
            DeepProfiler.Start("init");
            foreach (var gridThing in tmp)
            {
                //TTMpatch_ExtendedStoneworking will replace old stones
                if (gridThing.val != 0)
                {
                    ThingDef def = null;
                    if (!(thingDefsByShortHash.TryGetValue(gridThing.val, out def) && def != null))
                    {
                        Log.Warning("Map compressor decompression error: No thingDef with short hash " + gridThing.val);
                        if (thingDefsByShortHash.TryGetValue((ushort)(gridThing.val - 1), out def) && def != null)
                        {
                            Log.Warning("Try another hash, found " + def.label + def.shortHash.ToString());
                        }
                        else if (thingDefsByShortHash.TryGetValue((ushort)(gridThing.val + 1), out def) && def != null)
                        {
                            Log.Warning("Try another hash, found " + def.label + def.shortHash.ToString());
                        }
                        else
                        {
                            //If not set moutains can not be display.
                            def = stones[gridThing.val % stones.Length];
                            Log.Warning("Not found, Use " + def.label + def.shortHash.ToString() + "instead.");
                        }
                        //disable more warns by set an value
                        thingDefsByShortHash[gridThing.val] = def;
                    }
                    if (def != null)
                    {
                        var th = ThingMaker.MakeThing(def, null);
                        th.SetPositionDirect(gridThing.cell);
                        yield return th;
                    }
                }
            }
            DeepProfiler.End("init");
        }
    }
}
