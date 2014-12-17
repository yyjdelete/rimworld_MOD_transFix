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
            il.Emit(OpCodes.Stfld, fi);//Not Virt
            il.Emit(OpCodes.Ret);
            setJailerFaction = (Action<Pawn, Faction>)methodBldr.CreateDelegate(typeof(Action<Pawn, Faction>));
        }

        public static void ExposeDataEx(this Pawn that)
        {
            Scribe_Defs.LookDef<PawnKindDef>(ref that.kindDef, "kindDef");
            if (that.kindDef == null)
            {
                throw new Exception("undefined Pawn!!");
            }
            if (that.RaceProps.humanoid)
                throw new Exception();
            //base.ExposeData();
            callBaseExposeData(that);

            var jailerFactionInt = that.JailerFaction;
            Scribe_References.LookReference<Faction>(ref jailerFactionInt, "jailerFaction");
            setJailerFaction(that, jailerFactionInt);
            //that.SetJailerFaction(jailerFactionInt);

            Scribe_Fix.LookDeepNotNull<Pawn_StoryTracker>(ref that.story, "story", null, that);//发型
            Scribe_Values.LookValue<Gender>(ref that.gender, "sex", Gender.Male, false);
            Scribe_Fix.LookDeepNotNull<Pawn_ApparelTracker>(ref that.apparel, "apparel", null, that);//衣服
            Scribe_Fix.LookDeepNotNull<Pawn_EquipmentTracker>(ref that.equipment, "equipment", null, that);
            Scribe_Deep.LookDeep<Pawn_Thinker>(ref that.thinker, "mind", that);
            Scribe_Deep.LookDeep<Pawn_PlayerController>(ref that.playerController, "playerController", that);
            Scribe_Fix.LookDeepNotNull<Pawn_JobTracker>(ref that.jobs, "jobs", null, that);//FIX jobs with removed things
            Scribe_Deep.LookDeep<Pawn_AgeTracker>(ref that.ageTracker, "ageTracker", that);
            Scribe_Fix.LookDeepNotNull<Pawn_HealthTracker>(ref that.healthTracker, "healthTracker", null, that);//未知原因的损伤
            Scribe_Deep.LookDeep<Pawn_PathFollower>(ref that.pather, "pather", that);
            Scribe_Deep.LookDeep<Pawn_InventoryTracker>(ref that.inventory, "inventory", that);
            Scribe_Deep.LookDeep<Pawn_FilthTracker>(ref that.filth, "filth", that);
            Scribe_Deep.LookDeep<Pawn_FoodTracker>(ref that.food, "food", that);
            Scribe_Deep.LookDeep<Pawn_RestTracker>(ref that.rest, "rest", that);
            Scribe_Fix.LookDeepNotNull<Pawn_CarryHands>(ref that.carryHands, "carryHands", null, that);//搬运未知物品
            Scribe_Deep.LookDeep<Pawn_PsychologyTracker>(ref that.psychology, "psychology", that);
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
    }
}
