using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Verse;

namespace TransFix.Extends
{
    public static class FactionManagerEx
    {
        private static readonly Func<Faction, List<FactionRelation>> getRelations;

        static FactionManagerEx()
        {
            ParameterExpression expression;
            getRelations = Expression.Lambda<Func<Faction, List<FactionRelation>>>(
                Expression.Field(expression = Expression.Parameter(typeof(Faction), "f"), "relations"),
                new ParameterExpression[] { expression })
                .Compile();
        }

        public static void ExposeDataEx(this FactionManager fm)
        {
            List<Faction> allFactions = fm.AllFactionsListForReading;
            Scribe_Fix.LookListNotNull<Faction>(ref allFactions, "allFactions", LookMode.Deep, null);
            //FIXME: set 
        }

        public static bool Fix(this FactionManager fm)
        {
            //Fix faction
            var facts = fm.AllFactionsListForReading;
            //Log.Message("OLD");
            //foreach (Faction cur in facts)
            //{
            //    Log.Message(String.Format("{0}[{1}]", cur.GetUniqueLoadID(), cur.def.label));
            //}
            int count = facts.RemoveAll(fact => fact == null || fact.def == null);
            if (count > 0)
            {
                Log.Message(count.ToString() + " invalid factions removed.");
            }

            //Log.Message("REMOVED");
            //foreach (Faction cur in facts)
            //{
            //    Log.Message(String.Format("{0}[{1}]", cur.GetUniqueLoadID(), cur.def.label));
            //}
            //Log.Message("NEW");
            IEnumerable<Faction> newFacts = FactionGenerator.GenerateFactionsForNewWorld();
            HashSet<Faction> newFactSet = new HashSet<Faction>();
            foreach (Faction cur in newFacts)
            {
                newFactSet.Add(cur);
                if (!facts.Any(fact => fact.def == cur.def))
                {
                    Log.Message("Add missing faction: " + cur.GetUniqueLoadID() + "[" + cur.def.label + "]");
                    //fm.Add(cur);
                    facts.Add(cur);
                    Scribe_Fix.newMods.Add(cur.GetType().Assembly);
                    ++count;
                    //先注释掉, 解析的时候会解析到别的东西上
                    //LoadCrossRefHandler.RegisterLoadedSaveable(cur);
                }
                //Log.Message(String.Format("{0}[{1}]", cur.GetUniqueLoadID(), cur.def.label));
            }
            //Log.Message("FINAL");

            //NOTE: 由于FactionRelation的ExposeDate涉及到other的引用, 在ResolveAllCrossReferences之前未解析, 任何对LoadCrossRefHandler的变动会导致其执行顺序改变出错
            //Remove useless Relations
            foreach (Faction cur in facts)
            {
                //Log.Message(String.Format("{0}[{1}]", cur.GetUniqueLoadID(), cur.def.label));
                var relations = getRelations(cur);
                for (int i = relations.Count - 1; i >= 0; --i)
                {
                    FactionRelation curRat = relations[i];
                    var other = curRat.other;
                    if (other == null || !facts.Contains(other))
                    {
                        relations.RemoveAt(i);
                        //注意为null的情况
                        if (other != null)
                        {
                            if (!newFactSet.Contains(other))
                                Log.Message(String.Format("remove {0}[{1}]->{2}[{3}]", cur.GetUniqueLoadID(), cur.def.label, curRat.other.GetUniqueLoadID(), curRat.other.def.label));
                        }
                        else
                        {
                            Log.Message(String.Format("remove {0}[{1}]->unknown", cur.GetUniqueLoadID(), cur.def.label));
                        }
                    }
                }
                //按理说GenerateFactionsForNewWorld会在yield之前和已添加到地图中的势力建立关系, 但
                //可能在Scribe_Collections.LookList<Faction>(ref this.allFactions, "allFactions", LookMode.Deep, null);时直接就崩溃了, 
                //没有留下记录, 这会造成后续的原势力关系为空, 无法继续
                foreach (Faction other in facts)
                {
                    if (other != cur)
                    {
                        cur.TryMakeInitialRelationsWith(other);
                    }
                }
            }

            return count > 0;
        }
    }
}
