﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Diagnostics;

using HarmonyLib;
using MyBhapticsTactsuit;


namespace BeatSaberFunctionalBhaptics
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class BeatSaberFunctionalBhaptics : MonoBehaviour
    {
        public static BeatSaberFunctionalBhaptics Instance { get; private set; }

        public static TactsuitVR tactsuitVr;
        //public static bool inObstacle = false;


        //public static IPA.Logging.Logger logger { get; private set; }

        // These methods are automatically called by Unity, you should remove any you aren't using.
        #region Startup
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (Instance != null)
            {
                Plugin.Log?.Warn($"Instance of {GetType().Name} already exists, destroying.");
                GameObject.DestroyImmediate(this);
                return;
            }
            GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            Instance = this;
            //Plugin.Log?.Warn("Awake()");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            // patch all functions
            var harmony = new Harmony("bhaptics.functional.patch.beatsaber");
            harmony.PatchAll();
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            tactsuitVr.LOG($"{name}: OnDestroy()");
            if (Instance == this)
                Instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.

        }
        #endregion

        #region Player effects


        [HarmonyPatch(typeof(MissedNoteEffectSpawner), "HandleNoteWasMissed", new Type[] { typeof(NoteController) })]
        public class bhaptics_NoteMissed
        {
            [HarmonyPostfix]
            public static void Postfix(NoteController noteController)
            {
                if (noteController.noteData.colorType != ColorType.None) tactsuitVr.PlaybackHaptics("MissedNote");

            }
        }

        [HarmonyPatch(typeof(BombExplosionEffect), "SpawnExplosion", new Type[] { typeof(UnityEngine.Vector3) })]
        public class bhaptics_BombExplosion
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionBelly");
            }
        }

        [HarmonyPatch(typeof(CuttableBySaber), "CallWasCutBySaberEvent", new Type[] { typeof(Saber), typeof(UnityEngine.Vector3), typeof(UnityEngine.Quaternion), typeof(UnityEngine.Vector3) })]
        public class bhaptics_NoteCut
        {
            [HarmonyPostfix]
            public static void Postfix(Saber saber)
            {
                bool isRight = false;
                if (saber.name == "RightSaber") isRight = true;
                tactsuitVr.Recoil("Blade", isRight);
                //tactsuitVr.LOG("Hit: " + saber.name);
                //tactsuitVr.PlaybackHaptics("HeartBeat");
            }
        }
        

        [HarmonyPatch(typeof(PlayerHeadAndObstacleInteraction), "GetObstaclesContainingPoint", new Type[] { typeof(Vector3), typeof(List<ObstacleController>) })]
        public class bhaptics_HeadAndObstacle
        {
//            [HarmonyPostfix]
            public static void Postfix(List<ObstacleController> obstacleControllers)
            {
                //bool inObstacle = (__instance.intersectingObstacles.Count > 0);
                //tactsuitVr.LOG("Cheching if in Obstacle 2: " + __instance.intersectingObstacles.ToString());
                if (obstacleControllers.Count > 0) tactsuitVr.PlaybackHaptics("HitByWall");
                    //tactsuitVr.PlaySpecialEffect("HitByWall");
            }
        }
        
        
        [HarmonyPatch(typeof(LevelCompletionResultsHelper), "ProcessScore", new Type[] { typeof(PlayerData), typeof(PlayerLevelStatsData), typeof(LevelCompletionResults), typeof(IDifficultyBeatmap), typeof(PlatformLeaderboardsModel) })]
        public class bhaptics_LevelResults
        {
            [HarmonyPostfix]
            public static void Postfix(LevelCompletionResults levelCompletionResults)
            {
                if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared) tactsuitVr.PlaybackHaptics("LevelSuccess");
                if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed) tactsuitVr.PlaybackHaptics("LevelFailed");
            }
        }
        


        #endregion

        
        
    }
}