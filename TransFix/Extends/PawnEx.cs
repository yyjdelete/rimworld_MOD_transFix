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
    public static class PawnEx
    {
        private static Action<ThingWithComponents> callBaseExposeData;
        private static Action<Pawn, Faction> setJailerFaction;
        private static Func<Pawn_PsychologyTracker, Pawn> getPawn;
        private static Func<ThoughtHandler, List<Thought>> getThoughts;
        private static Action<Pawn_PathFollower, PathMode> setPathMode;

        static PawnEx()
        {
            Type baseType = typeof(ThingWithComponents);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            var mi = baseType.GetMethod("ExposeData", flags);
            var methodBldr = new DynamicMethod("", null, new[] { baseType }, baseType, true);
            var il = methodBldr.GetILGenerator();//获取il生成器
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, mi);//Not Virt
            il.Emit(OpCodes.Ret);
            callBaseExposeData = (Action<ThingWithComponents>)methodBldr.CreateDelegate(typeof(Action<ThingWithComponents>));

            Type curType = typeof(Pawn);
            flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var fi = curType.GetField("jailerFactionInt", flags);
            methodBldr = new DynamicMethod("", null, new[] { curType, typeof(Faction) }, curType, true);
            il = methodBldr.GetILGenerator();//获取il生成器
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, fi);
            il.Emit(OpCodes.Ret);
            setJailerFaction = (Action<Pawn, Faction>)methodBldr.CreateDelegate(typeof(Action<Pawn, Faction>));

            curType = typeof(Pawn_PsychologyTracker);
            flags = BindingFlags.Instance | BindingFlags.NonPublic;
            fi = curType.GetField("pawn", flags);
            methodBldr = new DynamicMethod("", typeof(Pawn), new[] { curType }, curType, true);
            il = methodBldr.GetILGenerator();//获取il生成器
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fi);
            il.Emit(OpCodes.Ret);
            getPawn = (Func<Pawn_PsychologyTracker, Pawn>)methodBldr.CreateDelegate(typeof(Func<Pawn_PsychologyTracker, Pawn>));
            
            curType = typeof(ThoughtHandler);
            flags = BindingFlags.Instance | BindingFlags.NonPublic;
            fi = curType.GetField("thoughts", flags);
            methodBldr = new DynamicMethod("", typeof(List<Thought>), new[] { curType }, curType, true);
            il = methodBldr.GetILGenerator();//获取il生成器
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, fi);
            il.Emit(OpCodes.Ret);
            getThoughts = (Func<ThoughtHandler, List<Thought>>)methodBldr.CreateDelegate(typeof(Func<ThoughtHandler, List<Thought>>));



            curType = typeof(Pawn_PathFollower);
            flags = BindingFlags.Instance | BindingFlags.NonPublic;
            fi = curType.GetField("pathMode", flags);
            methodBldr = new DynamicMethod("", null, new[] { curType, typeof(PathMode) }, curType, true);
            il = methodBldr.GetILGenerator();//获取il生成器
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, fi);
            il.Emit(OpCodes.Ret);
            setPathMode = (Action<Pawn_PathFollower, PathMode>)methodBldr.CreateDelegate(typeof(Action<Pawn_PathFollower, PathMode>));
            
        }

        public static void ExposeDataEx(this Pawn that)
        {
            //base.ExposeData();
            callBaseExposeData(that);

            Scribe_Defs.LookDef<PawnKindDef>(ref that.kindDef, "kindDef");
            if (that.kindDef == null)
            {
                throw new Exception("undefined Pawn!!");
            }
            var jailerFactionInt = that.JailerFaction;
            Scribe_References.LookReference<Faction>(ref jailerFactionInt, "jailerFaction");
            setJailerFaction(that, jailerFactionInt);
            //that.SetJailerFaction(jailerFactionInt);

            Scribe_Fix.LookDeepNotNull<Pawn_StoryTracker>(ref that.story, "story", that);//发型
            Scribe_Values.LookValue<Gender>(ref that.gender, "sex", Gender.Male, false);
            Scribe_Fix.LookDeepNotNull<Pawn_ApparelTracker>(ref that.apparel, "apparel", that);//衣服(Apparel)
            Scribe_Fix.LookDeepNotNull<Pawn_EquipmentTracker>(ref that.equipment, "equipment", that);
            Scribe_Deep.LookDeep<Pawn_Thinker>(ref that.thinker, "mind", that);
            Scribe_Deep.LookDeep<Pawn_PlayerController>(ref that.playerController, "playerController", that);
            Scribe_Fix.LookDeepNotNull<Pawn_JobTracker>(ref that.jobs, "jobs", that);//FIX jobs with removed things
            Scribe_Deep.LookDeep<Pawn_AgeTracker>(ref that.ageTracker, "ageTracker", that);
            Scribe_Fix.LookDeepNotNull<Pawn_HealthTracker>(ref that.healthTracker, "healthTracker", that);//未知原因的损伤
            Scribe_Fix.LookDeepNotNull<Pawn_PathFollower>(ref that.pather, "pather", that);//PathMode.OnSquare->PathMode.OnCell
            Scribe_Deep.LookDeep<Pawn_InventoryTracker>(ref that.inventory, "inventory", that);
            Scribe_Deep.LookDeep<Pawn_FilthTracker>(ref that.filth, "filth", that);
            Scribe_Deep.LookDeep<Pawn_FoodTracker>(ref that.food, "food", that);
            Scribe_Deep.LookDeep<Pawn_RestTracker>(ref that.rest, "rest", that);
            Scribe_Fix.LookDeepNotNull<Pawn_CarryHands>(ref that.carryHands, "carryHands", that);//搬运未知物品
            Scribe_Fix.LookDeepNotNull<Pawn_PsychologyTracker>(ref that.psychology, "psychology", that);//ThoughtDef
            Scribe_Deep.LookDeep<Pawn_PrisonerTracker>(ref that.prisoner, "prisoner", that);
            Scribe_Deep.LookDeep<Pawn_Ownership>(ref that.ownership, "ownership", that);
            Scribe_Deep.LookDeep<Pawn_TalkTracker>(ref that.talker, "talker", that);
            Scribe_Deep.LookDeep<Pawn_SkillTracker>(ref that.skills, "skills", that);
            Scribe_Deep.LookDeep<Pawn_WorkSettings>(ref that.workSettings, "workSettings", that);
        }

        public static void ExposeDataEx(this Pawn_HealthTracker that)
        {
            that.ExposeData();
            //cross ref
            //that.bodyModel.healthDiffs.RemoveAll(diff =>
            //{
            //    return diff.def == null ||
            //        diff.body == null ||
            //        diff.source == null ||
            //        diff.sourceBodyPartGroup == null ||
            //        diff.sourceHealthDiffDef == null;
            //});

            //throw if bad
            //that.HealthTick();
        }

        public static void ExposeDataEx(this Pawn_PsychologyTracker that)
        {
            that.ExposeData();
            //that.Fix();

            //var pawn = getPawn(that);
            //Scribe_Values.LookValue<bool>(ref that.neverFleeIndividual, "neverFleeIndividual", false, false);
            //Scribe_Deep.LookDeep<ThoughtHandler>(ref that.thoughts, "thoughts", pawn);
            //Scribe_Deep.LookDeep<StatusLevel_Mood>(ref that.mood, "mood", pawn);
            //Scribe_Deep.LookDeep<StatusLevel_Environment>(ref that.environment, "environment", pawn);
            //Scribe_Deep.LookDeep<StatusLevel_Openness>(ref that.openness, "openness", pawn);
            //Scribe_Deep.LookDeep<PawnRecentMemory>(ref that.recentMemory, "recentMemory", pawn);
            //Scribe_Values.LookValue<int>(ref that.ticksBelowMentalBreakThreshold, "ticksBelowMentalBreakThreshold", 0, false);
        }

        public static void ExposeDataEx(this Pawn_PathFollower that)
        {
            that.ExposeData();

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var node = Scribe.curParent["pathMode"];
                if (node != null)
                {
                    if ("OnSquare".Equals(node.InnerText, StringComparison.OrdinalIgnoreCase))
                    {
                        setPathMode(that, PathMode.OnCell);
                    }
                }
                if (that.nextCell == default(IntVec3))
                {
                    Scribe_Values.LookValue<IntVec3>(ref that.nextCell, "nextSquare", default(IntVec3), false);
                }
            }
        }

        public static void Fix(this Pawn_PsychologyTracker that)
        {
            var tHandler = that.thoughts;
            if (tHandler != null)
            {
                var thoughts = getThoughts(tHandler);
                if (thoughts != null)
                {
                    thoughts.RemoveAll(t => t == null || t.def == null);
                }
                var tMap = (ICollection<ThoughtDef>)tHandler.DistinctThoughtDefs;
                tMap.Remove(null);
            }
        }
    }
}
