using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Diagnostics;

using HarmonyLib;
using MyBhapticsTactsuit;


namespace BeatSaber_BSIPA_bhaptics
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
    public class BeatSaber_MusicalBhaptics : MonoBehaviour
    {
        public static BeatSaber_MusicalBhaptics Instance { get; private set; }
        public static bool musicalMod = false;

        public static TactsuitVR tactsuitVr;
        public static List<string> myEffectStrings = new List<string> { };
        public static Stopwatch timerLastEffect = new Stopwatch();
        public static Stopwatch timerSameTime = new Stopwatch();
        public static int numberOfEvents = 0;
        public static int totalNumberOfEvents = 3000;
        public static int defaultTriggerNumber = 4;
        public static int currentTriggerNumber = 4;
        public static List<float> highWeights = new List<float> { };
        public static float weightFactor = 1.0f;
        public static bool reducedWeight = false;
        public static bool ringEffectOff = false;
        public static System.Random rnd = new System.Random();

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
            Plugin.Log?.Debug($"{name}: Awake()");
            tactsuitVr = new TactsuitVR();
            myEffectStrings = tactsuitVr.myEffectStrings;
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.beatsaber");
            harmony.PatchAll();
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Plugin.Log?.Debug($"{name}: OnDestroy()");
            if (Instance == this)
                Instance = null; // This MonoBehaviour is being destroyed, so set the static instance property to null.

        }
        #endregion

        #region Map analysis
        public static void resetGlobalParameters()
        {
            highWeights.Clear();
            weightFactor = 1.0f;
            reducedWeight = false;
            totalNumberOfEvents = 3000;
            defaultTriggerNumber = 4;
            currentTriggerNumber = 4;
            ringEffectOff = false;
        }

        public static void analyzeMap(BeatmapData beatmapData)
        {
            // if there are too many ring effects, it gets annoying
            ringEffectOff = (beatmapData.spawnRotationEventsCount > 50);
            // count total number of events, estimate trigger number
            totalNumberOfEvents = beatmapData.beatmapEventsData.Count();
            defaultTriggerNumber = totalNumberOfEvents / 500;
            if (defaultTriggerNumber <= 1) defaultTriggerNumber = 2;
            currentTriggerNumber = defaultTriggerNumber;
        }

        [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromBinary", new Type[] { typeof(byte[]), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_GetBinaryData
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapData __result)
            {
                resetGlobalParameters();
                analyzeMap(__result);
            }
        }

        [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromJson", new Type[] { typeof(string), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_GetJsonData
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapData __result)
            {
                resetGlobalParameters();
                analyzeMap(__result);
            }
        }

        [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromBeatmapSaveData", new Type[] { typeof(List<BeatmapSaveData.NoteData>), typeof(List<BeatmapSaveData.WaypointData>), typeof(List<BeatmapSaveData.ObstacleData>), typeof(List<BeatmapSaveData.EventData>), typeof(BeatmapSaveData.SpecialEventKeywordFiltersData), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_GetMemoryData
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapData __result)
            {
                resetGlobalParameters();
                analyzeMap(__result);
            }
        }

        #endregion

        #region Player effects


        [HarmonyPatch(typeof(MissedNoteEffectSpawner), "HandleNoteWasMissed", new Type[] { typeof(NoteController) })]
        public class bhaptics_NoteMissed
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (musicalMod) return;
                tactsuitVr.PlaybackHaptics("MissedNote");
            }
        }

        [HarmonyPatch(typeof(BombExplosionEffect), "SpawnExplosion", new Type[] { typeof(UnityEngine.Vector3) })]
        public class bhaptics_BombExplosion
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (musicalMod) return;
                tactsuitVr.PlaybackHaptics("ExplosionBelly");
            }
        }

        [HarmonyPatch(typeof(CuttableBySaber), "CallWasCutBySaberEvent", new Type[] { typeof(Saber), typeof(UnityEngine.Vector3), typeof(UnityEngine.Quaternion), typeof(UnityEngine.Vector3) })]
        public class bhaptics_NoteCut
        {
            [HarmonyPostfix]
            public static void Postfix(Saber saber)
            {
                if (musicalMod) return;
                bool isRight = false;
                if (saber.name == "RightSaber") isRight = true;
                tactsuitVr.Recoil("Blade", isRight);
                //tactsuitVr.LOG("Hit: " + saber.name);
                //tactsuitVr.PlaybackHaptics("HeartBeat");
            }
        }
        
        [HarmonyPatch(typeof(BeatmapObjectExecutionRatingsRecorder), "Update", new Type[] { })]
        public class bhaptics_EnergyChange
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapObjectExecutionRatingsRecorder __instance)
            {
                if (musicalMod) return;
                if (__instance.beatmapObjectExecutionRatings.Count() == 0) return;
                BeatmapObjectExecutionRating lastRating = __instance.beatmapObjectExecutionRatings[__instance.beatmapObjectExecutionRatings.Count() - 1];
                if (lastRating.beatmapObjectRatingType == BeatmapObjectExecutionRating.BeatmapObjectExecutionRatingType.Obstacle)
                {
                    if (((ObstacleExecutionRating)lastRating).rating == ObstacleExecutionRating.Rating.NotGood) tactsuitVr.PlaySpecialEffect("HitByWall");
                }
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
                resetGlobalParameters();
            }
        }
        


        #endregion

        
        #region Lighting effects

        [HarmonyPatch(typeof(TrackLaneRingsRotationEffect), "SpawnRingRotationEffect", new Type[] { })]
        public class bhaptics_RingRotationEffect
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!musicalMod) return;
                tactsuitVr.LOG("Ring effect");
                if (ringEffectOff) return;
                tactsuitVr.PlaySpecialEffect("RingRotation");
                tactsuitVr.LOG("Done.");
            }
        }
        
        
        [HarmonyPatch(typeof(EnvironmentSpawnRotation), "BeatmapEventAtNoteSpawnCallback", new Type[] { typeof(BeatmapEventData) })]
        public class bhaptics_LightChangeEffect
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapEventData beatmapEventData)
            {
                if (!musicalMod) return;
                // If it's a "special" effect, just play a pattern
                tactsuitVr.LOG("Beatmapevent!");

                if ((beatmapEventData.type == BeatmapEventType.Special0) | (beatmapEventData.type == BeatmapEventType.Special1) | (beatmapEventData.type == BeatmapEventType.Special2) | (beatmapEventData.type == BeatmapEventType.Special3))
                {
                    tactsuitVr.LOG("SpecialEffect");
                    tactsuitVr.PlaySpecialEffect(tactsuitVr.myEffectStrings[rnd.Next(myEffectStrings.Count())]);
                    return;
                }
                tactsuitVr.LOG("Timers");
                // If last effects has been a while, reduce threshold
                if (!timerLastEffect.IsRunning) timerLastEffect.Start();
                if (timerLastEffect.ElapsedMilliseconds >= 2000)
                {
                    if (currentTriggerNumber > 1) currentTriggerNumber -= 1;
                    timerLastEffect.Restart();
                }

                // Count number of effects at the "same time"
                if (timerSameTime.ElapsedMilliseconds <= 100)
                {
                    numberOfEvents += 1;
                    timerSameTime.Restart();
                }
                else
                {
                    numberOfEvents = 0;
                    timerSameTime.Restart();
                }

                // If number of simultaneous events is above threshold, trigger effect
                if (numberOfEvents >= currentTriggerNumber)
                {
                    // reset trigger (if it was lowered)
                    currentTriggerNumber = defaultTriggerNumber;
                    tactsuitVr.LOG("Strings: " + myEffectStrings.ToString());
                    int randomNumer = rnd.Next(myEffectStrings.Count);
                    tactsuitVr.LOG("Before: " + randomNumer.ToString());
                    string effectName = myEffectStrings[rnd.Next(myEffectStrings.Count)];
                    tactsuitVr.LOG("After");
                    tactsuitVr.PlaySpecialEffect(effectName);

                    // check if default trigger was set way too high or too low
                    float weight = (float)numberOfEvents / (float)defaultTriggerNumber / weightFactor;
                    if (weight > 5.0f) highWeights.Add(weight);
                    if (weight < 0.24f) highWeights.Add(weight);
                    // if this happened 4 times in a row, adjust trigger (only down)
                    if (highWeights.Count >= 4)
                    {
                        weightFactor = highWeights.Average();
                        if (weightFactor < 1.0f)
                        {
                            if ((!reducedWeight) && (defaultTriggerNumber > 2))
                            {
                                defaultTriggerNumber -= 1;
                                tactsuitVr.LOG("Trigger adjusted! " + defaultTriggerNumber.ToString() + " " + weightFactor.ToString());
                            }
                        }
                        else reducedWeight = true;
                        highWeights.Clear();
                    }
                    numberOfEvents = 0;
                }

            }
        }
        
        #endregion
        
    }
}
