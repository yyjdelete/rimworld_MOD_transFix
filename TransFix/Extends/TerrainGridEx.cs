using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class TerrainGridEx
    {
        public static void ExposeDataEx(this TerrainGrid tg)
        {
            string str = string.Empty;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                str = GridSaveUtility.CompressedStringForShortGrid(c => tg.grid[CellIndices.CellToIndex(c)].shortHash);
            }
            Scribe_Values.LookValue<string>(ref str, "terrainGridCompressed", null, false);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Dictionary<ushort, TerrainDef> dictionary = new Dictionary<ushort, TerrainDef>();
                foreach (TerrainDef current in DefDatabase<TerrainDef>.AllDefs)
                {
                    dictionary.Add(current.shortHash, current);
                }
                TerrainDef sand = TerrainDef.Named("Sand");
                foreach (GridSaveUtility.LoadedGridShort cur in GridSaveUtility.LoadedUShortGrid(str))
                {
                    TerrainDef def2 = null;
                    if (!(dictionary.TryGetValue(cur.val, out def2) && def2 != null))
                    {
                        Log.Error("Did not find terrain def with short hash " + cur.val.ToString() + " for square " + cur.pos.ToString() + ".");
                        if (dictionary.TryGetValue((ushort)(cur.val - 1), out def2) && def2 != null)
                        {
                            Log.Warning("Try another hash, found " + def2.label + def2.shortHash.ToString());
                        }
                        else if (dictionary.TryGetValue((ushort)(cur.val + 1), out def2) && def2 != null)
                        {
                            Log.Warning("Try another hash, found " + def2.label + def2.shortHash.ToString());
                        }
                        else
                        {
                            def2 = sand;//can not be null
                            Log.Warning("Not found, Use " + def2.label + def2.shortHash.ToString() + "instead.");
                        }
                        dictionary[cur.val] = def2;
                    }
                    tg.grid[CellIndices.CellToIndex(cur.pos)] = def2;
                }
            }
        }
    }
}
