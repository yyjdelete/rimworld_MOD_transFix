using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Verse;

namespace TransFix
{
     public static class LoadCrossRefHandlerEx
    {
        private static readonly List<Saveable> crossReferencingSaveables;
        private static readonly List<List<string>> idListsRead;
        private static readonly Queue<string> idsRead;

        static LoadCrossRefHandlerEx()
        {
            BindingFlags bindFlag = BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var crossReferencingSaveablesInfo = typeof(LoadCrossRefHandler).GetField("crossReferencingSaveables", bindFlag);
            var idsReadInfo = typeof(LoadIDsWantedBank).GetField("idsRead", bindFlag);
            var idListsReadInfo = typeof(LoadIDsWantedBank).GetField("idListsRead", bindFlag);

            var getCrossReferencingSaveables = Expression.Lambda<Func<List<Saveable>>>(
                Expression.Field(null, crossReferencingSaveablesInfo), 
                new ParameterExpression[0])
                .Compile();
            crossReferencingSaveables = getCrossReferencingSaveables();

            var getIdsRead = Expression.Lambda<Func<Queue<string>>>(
                Expression.Field(null, idsReadInfo),
                new ParameterExpression[0])
                .Compile();
            idsRead = getIdsRead();

            crossReferencingSaveables = getCrossReferencingSaveables();

            var getIdListsRead = Expression.Lambda<Func<List<List<string>>>>(
                Expression.Field(null, idListsReadInfo),
                new ParameterExpression[0])
                .Compile();
            idListsRead = getIdListsRead();
        }

        public static void ResolveAllCrossReferences()
        {
            Scribe.mode = LoadSaveMode.ResolvingCrossRefs;
            foreach (Saveable current in LoadCrossRefHandlerEx.crossReferencingSaveables)
            {
                LoadReferenceable loadReferenceable = current as LoadReferenceable;
                if (loadReferenceable != null)
                {
                    try
                    {
                        LoadedObjectDirectory.RegisterLoaded(loadReferenceable);
                    }
                    catch
                    {
                        try
                        {
                            //ToString也出错的真是没救了
                            Log.Warning("Can not load " + loadReferenceable.ToString() + ", [" + loadReferenceable.GetType().ToString() + "]");
                        }
                        catch { }
                    }
                }
            }
            foreach (Saveable current2 in LoadCrossRefHandlerEx.crossReferencingSaveables)
            {
                try
                {
                    current2.ExposeData();
                }
                catch (Exception e)
                {
                    try
                    {
                        Log.Warning("Can not resolve " + current2.ToString() + ", [" + current2.GetType().ToString() + "]");
                    }
                    catch { }
                    Log.Notify_Exception(e);
                }
            }
            LoadIDsWantedBank.ConfirmClear();
            LoadCrossRefHandlerEx.crossReferencingSaveables.Clear();
            LoadedObjectDirectory.Clear();
        }

        public static Tuple<int, int, int> SaveState()
        {
            return new Tuple<int, int, int>(crossReferencingSaveables.Count, idsRead.Count, idListsRead.Count);
        }

        public static void RestoreState(Tuple<int, int, int> state)
        {
            int count;

            count = crossReferencingSaveables.Count - state.First;
            if (count < 0)
            {
                count = 0;
            }
            crossReferencingSaveables.RemoveRange(state.First, count);

            string[] datas = idsRead.ToArray();
            idsRead.Clear();
            count = Math.Min(datas.Length, state.Second);
            for (int i = 0; i < count; ++i)
            {
                idsRead.Enqueue(datas[i]);
            }

            count = idListsRead.Count - state.Third;
            if (count < 0)
            {
                count = 0;
            }
            crossReferencingSaveables.RemoveRange(state.Third, count);
        }
    }
}
