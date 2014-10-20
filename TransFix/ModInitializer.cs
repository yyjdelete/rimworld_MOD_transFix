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
 

    public class ModInitializer : ITab, IDisposable
    {
        private static string modName = "TransFix";
        public static string ModName { get { return modName; } }

        protected GameObject gameObject;

        public ModInitializer()
        {
            Log.Message("Initialized the TransFix mod");
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

        #region IDisposable Members

        /// <summary>
        /// Internal variable which checks if Dispose has already been called
        /// </summary>
        private Boolean disposed;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(Boolean disposing)
        {
            Log.Message("Dispose: " + disposing);
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                //TODO: Managed cleanup code here, while managed refs still valid
            }
            //TODO: Unmanaged cleanup code here

            disposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Call the private Dispose(bool) helper and indicate 
            // that we are explicitly disposing
            this.Dispose(true);

            // Tell the garbage collector that the object doesn't require any
            // cleanup when collected since Dispose was called explicitly.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The destructor for the class.
        /// </summary>
        ~ModInitializer()
        {
            this.Dispose(false);
        }


        #endregion
        
    }
    
    internal class ModController : MonoBehaviour
    {
        private readonly string ModName = ModInitializer.ModName;
        private LoadedLanguage oldLang;

        private bool IsModEnabled()
        {
            return LoadedModManager.LoadedMods.Any(m => ModName.Equals(m.name));
        }

        public void OnLevelWasLoaded(int level)
        {
            Log.Message("OnLevelWasLoaded: " + level);
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
                                Log.Message("Trans fixed.");
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
            Log.Message("Start: " + enabled);
            base.enabled = false;
        }

        public virtual void Update()
        {
            Log.Message("Update: " + enabled);
            base.enabled = false;
        }

        private void FixTransLocked()
        {
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
                            if (!IsTranslated<ThingDef>(def.defName + "_Meat.label"))
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
                            if (!IsTranslated<ThingDef>(def.defName + "_Meat.description"))
                            {
                                meat.description = "MeatDesc".Translate(labels);
                                ++count;
                            }
                        }

                        var leather = race.leatherDef;
                        if (leather != null)
                        {
                            if (!IsTranslated<ThingDef>(def.defName + "_Leather.label"))
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
                            if (!IsTranslated<ThingDef>(def.defName + "_Leather.description"))
                            {
                                leather.description = "LeatherDesc".Translate(labels);
                                ++count;
                            }
                        }

                        var corpse = race.corpseDef;
                        if (corpse != null)
                        {
                            if (!IsTranslated<ThingDef>(def.defName + "_Corpse.label"))
                            {
                                corpse.label = "CorpseLabel".Translate(labels);
                                ++count;
                            }
                            if (!IsTranslated<ThingDef>(def.defName + "_Corpse.description"))
                            {
                                corpse.description = "CorpseDesc".Translate(labels);
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
                            if (!IsTranslated<ThingDef>(def.defName + "_Seed.label"))
                            {
                                seed.label = "SeedLabel".Translate(new object[] { def.label });
                                ++count;
                            }
                            //NO SeedDesc
#if false
                            if (IsTranslated("SeedDesc"))
                            {
                                seed.description = "SeedDesc".Translate(new object[] { def.label });
                                ++count;
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
            if (count > 0)
            {
                //再次覆盖(注意这里以最新的activeLanguage为准, 前面是没办法)
                LanguageDatabase.activeLanguage.InjectIntoDefs();
            }
            Log.Message("Fix " + count + " trans.");
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
