﻿using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;
using VerseBase;

namespace TransFix
{

    public class RootMap : VerseBase.RootMap
    {
        private static bool globalInitDone;

        public override void Start()
        {
            if (!globalInitDone)
            {
                VersionControl.LogVersionNumber();
                Application.targetFrameRate = 60;
                Prefs.Init();
                globalInitDone = true;
            }
            if (!PlayDataLoader.loaded)
            {
                PlayDataLoader.LoadAllPlayData();
            }
            Find.ResetBaseReferences();
            base.realTime = new RealTime();
            base.soundRoot = new SoundRoot();
            if (Application.loadedLevelName == "Gameplay")
            {
                base.uiRoot = new RimWorld.UIMapRoot();
            }
            else if (Application.loadedLevelName == "Entry")
            {
                base.uiRoot = new UIEntryRoot();
            }
            if (MapInitData.mapToLoad.NullOrEmpty())
            {
                MapIniter_NewGame.InitNewGeneratedMap();
            }
            else
            {
                TransFix.MapIniter_LoadFromFile.InitMapFromFile(MapInitData.mapToLoad);
            }
        }
    }
}

