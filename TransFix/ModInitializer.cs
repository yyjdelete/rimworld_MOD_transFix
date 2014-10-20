using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Verse;
using VerseBase;

namespace TransFix
{
 

    public class ModInitializer : ITab
    {
        protected GameObject gameObject;

        public ModInitializer()
        {
            Log.Message(String.Format("Initialized the TransFix mod {0}.", Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            this.gameObject = PrepareMod();

        }

        private static GameObject sGameObject;
        private static readonly object modLock = new object();
        private static GameObject PrepareMod()
        {
            if (sGameObject == null)
            {
                lock (modLock)
                {
                    if (sGameObject == null)
                    {
                        sGameObject = new GameObject("TransFix");
                        sGameObject.AddComponent<ModController>();
                        UnityEngine.Object.DontDestroyOnLoad(sGameObject);
                    }
                }
            }
            return sGameObject;
        }

        protected override void FillTab()
        {
            //Log.Message("FillTab");
        }
    }
    
    internal class ModController : MonoBehaviour
    {
        private const string MOD_NAME = "TransFix";
        private LoadedLanguage oldLang;

        private bool IsModEnabled()
        {
            return LoadedModManager.LoadedMods.Any(m => MOD_NAME.Equals(m.name));
        }

        public void OnLevelWasLoaded(int level)
        {
            //Log.Message("OnLevelWasLoaded: " + level);
            if (level == 0)
            {
                //this.gameplay = false;
                //base.enabled = true;
                //base.enabled = false;
            }
            else if (level == 1)
            {
                //base.enabled = true;
                if (this.IsModEnabled())
                {
                    if (oldLang != LanguageDatabase.activeLanguage)
                    {
                        lock (this)
                        {
                            var newLang = LanguageDatabase.activeLanguage;
                            if (oldLang != newLang)
                            {
                                oldLang = newLang;
                                Log.Message("Fixing trans...");
                                FixTransLocked();
                            }
                        }
                    }
                }
            }
            else
            {
                //base.enabled = false;
            }
        }

        public virtual void Start()
        {
            //Log.Message("Start: " + enabled);
            base.enabled = false;
        }

        public virtual void Update()
        {
            //Log.Message("Update: " + enabled);
            base.enabled = false;
        }

        private void FixTransLocked()
        {
            long start = DateTime.UtcNow.Ticks;
            int count = 0;
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.category == EntityCategory.Pawn)
                {
                    //ThingDefGenerator_Corpses, ThingDefGenerator_Leather, ThingDefGenerator_Meat
                    //优先级: Xxx_Yyy.label/desc(Meat/Leather/Corpse) > Xxx.race.yyyLabel(Meat/Leather) > YyyLabel/Desc(Meat/Leather/Corpse)
                    var race = def.race;
                    if (race != null)
                    {
                        object[] labels = null;
                        //加判断太麻烦了而且概率不大, 还不如再Translate一次
                        //if (IsTranslated<ThingDef>(def.defName + ".label"))
                        {
                            labels = new object[] { def.label };
                        }

                        var meat = race.meatDef;
                        if (meat != null)
                        {
                            //NOTE: some pawns use Metal as meat of them!!!!
                            //Log.Message("" + def.defName + "->" + meat.defName);
                            if (!IsTranslated<ThingDef>(meat.defName + ".label"))
                            {
                                if (IsTranslated<ThingDef>(def.defName + ".race.meatLabel"))
                                {
                                    meat.label = race.meatLabel;
                                }
                                else
                                {
                                    meat.label = "MeatLabel".Translate(labels);
                                }
                                ++count;
                            }
                            if (!IsTranslated<ThingDef>(meat.defName + ".description"))
                            {
                                meat.description = "MeatDesc".Translate(labels);
                                ++count;
                            }
                        }

                        var leather = race.leatherDef;
                        if (leather != null)
                        {
                            //Log.Message("" + def.defName + "->" + leather.defName);
                            if (!IsTranslated<ThingDef>(leather.defName + ".label"))
                            {
                                if (IsTranslated<ThingDef>(def.defName + ".race.leatherLabel"))
                                {
                                    leather.label = race.leatherLabel;
                                }
                                else
                                {
                                    leather.label = "LeatherLabel".Translate(labels);
                                }
                                ++count;
                            }
                            if (!IsTranslated<ThingDef>(leather.defName + ".description"))
                            {
                                leather.description = "LeatherDesc".Translate(labels);
                                ++count;
                            }
                        }

                        var corpse = race.corpseDef;
                        if (corpse != null)
                        {
                            if (!IsTranslated<ThingDef>(corpse.defName + ".label"))
                            {
                                corpse.label = "CorpseLabel".Translate(labels);
                                ++count;
                            }
                            if (!IsTranslated<ThingDef>(corpse.defName + ".description"))
                            {
                                corpse.description = "CorpseDesc".Translate(labels);
                                ++count;
                            }
                        }
                    }
                }
                else if (def.category == EntityCategory.Plant)
                {
                    //ThingDefGenerator_Seeds
                    var plant = def.plant;
                    if (plant != null)
                    {
                        var seed = plant.seedDef;
                        if (seed != null)
                        {
                            if (!IsTranslated<ThingDef>(seed.defName + ".label"))
                            {
                                seed.label = "SeedLabel".Translate(new object[] { def.label });
                                ++count;
                            }
                            //NO SeedDesc
#if true
                            if (!IsTranslated<ThingDef>(seed.defName + ".description"))
                            {
                                if (IsTranslated("SeedDesc"))
                                {
                                    seed.description = "SeedDesc".Translate(new object[] { def.label });
                                    ++count;
                                }
                            }
#endif
                        }
                    }
                }
            }
            //ThingDefGenerator_Buildings(need not)
            //ThingDef.frameDef:Xxx_Frame.label/desc > Xxx.label + "FrameLabelExtra".Translate()/Xxx.description
            //ThingDef.blueprintDef:Xxx_Blueprint.label/desc > Xxx.label + "BlueprintLabelExtra".Translate()/null
            //TerrainDef.frameDef:Xxx_Frame.label/desc > Xxx.label + "FrameLabelExtra".Translate()/"Terrain building in progress."
            //TerrainDef.blueprintDef:Xxx_Blueprint.label/desc > Xxx.label + "BlueprintLabelExtra".Translate()/null

            //处理研究完成时的描述
            foreach (var def in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (!IsTranslated<ResearchProjectDef>(def.defName + ".descriptionDiscovered"))
                {
                    ++count;
                    //如果没有专门的汉化, 则从desc读取
                    def.descriptionDiscovered = def.description;
                }
            }

            //needn't with IsTranslated
            //if (count > 0)
            //{
            //    //再次覆盖(注意这里以最新的activeLanguage为准, 前面是没办法)
            //    LanguageDatabase.activeLanguage.InjectIntoDefs();
            //}
            Log.Message(String.Format("{0} trans fixed. ({1}ms)", count, ((double)(DateTime.UtcNow.Ticks - start)) / TimeSpan.TicksPerMillisecond));
        }

        /// <summary>
        /// for key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool IsTranslated(string key)
        {
            return LanguageDatabase.activeLanguage.HasKey(key);
        }

        /// <summary>
        /// for DefInjection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool IsTranslated<T>(string key)
        {
            return LanguageDatabase.activeLanguage.HasKey<T>(key);
        }
    }

    public static class LoadedLanguageHelper
    {
        private readonly static Func<DefInjectionPackage, Dictionary<string, string>> getInjectionsSimple;
        private readonly static Func<DefInjectionPackage, Dictionary<string, string>> getInjectionsPath;
        private readonly static Func<LoadedLanguage, Dictionary<string, string>> getKeyedReplacements;

        static LoadedLanguageHelper()
        {
            //BindingFlags bindFlag = BindingFlags.Instance | 
            //    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            //var injectionsSimpleInfo = typeof(DefInjectionPackage).GetField("injectionsSimple", bindFlag);
            //var injectionsPathInfo = typeof(DefInjectionPackage).GetField("injectionsPath", bindFlag);

            //使用Expression代替反射以提高性能
            ParameterExpression expression;
            getInjectionsSimple = Expression.Lambda<Func<DefInjectionPackage, Dictionary<string, string>>>(
                Expression.Field(expression = Expression.Parameter(typeof(DefInjectionPackage), "dip"), "injectionsSimple"),
                new ParameterExpression[] { expression })
                .Compile();
            getInjectionsPath = Expression.Lambda<Func<DefInjectionPackage, Dictionary<string, string>>>(
                Expression.Field(expression = Expression.Parameter(typeof(DefInjectionPackage), "dip"), "injectionsPath"),
                new ParameterExpression[] { expression })
                .Compile();
            getKeyedReplacements = Expression.Lambda<Func<LoadedLanguage, Dictionary<string, string>>>(
                Expression.Field(expression = Expression.Parameter(typeof(LoadedLanguage), "lang"), "keyedReplacements"),
                new ParameterExpression[] { expression })
                .Compile();

        }

        public static Dictionary<string, string> GetInjectionsSimple(this DefInjectionPackage dip)
        {
            return getInjectionsSimple(dip);
        }
        public static Dictionary<string, string> GetInjectionsPath(this DefInjectionPackage dip)
        {
            return getInjectionsPath(dip);
        }

        public static bool HasKey(this DefInjectionPackage dip, string key)
        {

            return dip.GetInjectionsSimple().ContainsKey(key) ||
                dip.GetInjectionsPath().ContainsKey(key);
        }
        public static Dictionary<string, string> GetKeyedReplacements(this LoadedLanguage lang)
        {
            return getKeyedReplacements(lang);
        }

        /// <summary>
        /// for DefInjection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lang"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool HasKey<T>(this LoadedLanguage lang, string key)
        {
            var dips = LanguageDatabase.activeLanguage.defInjections.Where(dip => dip.defType == typeof(T));

            return dips.Any(dip => dip.HasKey(key));
        }

        /// <summary>
        /// for key
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool HasKey(this LoadedLanguage lang, string key)
        {

            return LanguageDatabase.activeLanguage.GetKeyedReplacements().ContainsKey(key);
        }
    }
}
