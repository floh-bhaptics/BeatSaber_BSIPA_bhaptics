using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bhaptics.Tact;
using bHapticsFunctional;
using System.Resources;
using System.Globalization;
using System.Collections;

namespace MyBhapticsTactsuit
{
    public class TactsuitVR
    {
        /* A class that contains the basic functions for the bhaptics Tactsuit, like:
         * - A Heartbeat function that can be turned on/off
         * - A function to read in and register all .tact patterns in the bHaptics subfolder
         * - A logging hook to output to the Melonloader log
         * - 
         * */
        public bool suitDisabled = true;
        public bool systemInitialized = false;
        // Event to start and stop the heartbeat thread
        //private static ManualResetEvent HeartBeat_mrse = new ManualResetEvent(false);
        // dictionary of all feedback patterns found in the bHaptics directory
        public Dictionary<String, String> FeedbackMap = new Dictionary<String, String>();
        public List<string> myEffectStrings = new List<string> { };

#pragma warning disable CS0618 // remove warning that the C# library is deprecated
        public HapticPlayer hapticPlayer;
#pragma warning restore CS0618 


        private static RotationOption defaultRotationOption = new RotationOption(0.0f, 0.0f);

        public TactsuitVR()
        {
            LOG("Initializing suit");
            try
            {
#pragma warning disable CS0618 // remove warning that the C# library is deprecated
                hapticPlayer = new HapticPlayer("H3VR_bhaptics", "H3VR_bhaptics");
#pragma warning restore CS0618
                suitDisabled = false;
            }
            catch { LOG("Suit initialization failed!"); }
            RegisterAllTactFiles();
            LOG("Starting HeartBeat thread...");
        }

        public void LOG(string logStr)
        {
            Plugin.Log.Info(logStr);
            //Plugin.Log.LogMessage(logStr);
        }

        

        void RegisterAllTactFiles()
        {
            ResourceSet resourceSet = bHapticsFunctional.Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            
            foreach (DictionaryEntry d in resourceSet)
            {
                try
                {
                    hapticPlayer.RegisterTactFileStr(d.Key.ToString(), d.Value.ToString());
                    LOG("Pattern registered: " + d.Key.ToString());
                }
                catch (Exception e) { LOG(e.ToString()); }

                FeedbackMap.Add(d.Key.ToString(), d.Value.ToString());
            }
            systemInitialized = true;
        }

        public void PlaybackHaptics(String key, float intensity = 1.0f, float duration = 1.0f)
        {
            //LOG("Trying to play");
            if (FeedbackMap.ContainsKey(key))
            {
                //LOG("ScaleOption");
                ScaleOption scaleOption = new ScaleOption(intensity, duration);
                //LOG("Submit");
                hapticPlayer.SubmitRegistered(key, scaleOption);
            }
            else
            {
                LOG("Feedback not registered: " + key);
            }
        }

        public void Recoil(string weaponName, bool isRightHand, float intensity = 1.0f)
        {
            // weaponName is a parameter that will go into the vest feedback pattern name
            // isRightHand is just which side the feedback is on
            // intensity should usually be between 0 and 1

            float duration = 1.0f;
            var scaleOption = new ScaleOption(intensity, duration);
            // the function needs some rotation if you want to give the scale option as well
            var rotationFront = new RotationOption(0f, 0f);
            // make postfix according to parameter
            string postfix = "_L";
            if (isRightHand) { postfix = "_R"; }

            // stitch together pattern names for Arm and Hand recoil
            string keyHands = "RecoilHands" + postfix;
            string keyArm = "Recoil" + postfix;
            // vest pattern name contains the weapon name. This way, you can quickly switch
            // between swords, pistols, shotguns, ... by just changing the shoulder feedback
            // and scaling via the intensity for arms and hands
            string keyVest = "Recoil" + weaponName + "Vest" + postfix;
            hapticPlayer.SubmitRegistered(keyHands, scaleOption);
            hapticPlayer.SubmitRegistered(keyArm, scaleOption);
            hapticPlayer.SubmitRegistered(keyVest, scaleOption);
        }


        public bool IsPlaying(String effect)
        {
            return IsPlaying(effect);
        }

        public void PlaySpecialEffect(string effect)
        {
            foreach (string myEffect in myEffectStrings)
            {
                if (IsPlaying(myEffect)) return;
            }
            if (IsPlaying(effect)) return;
            PlaybackHaptics(effect);
        }

        public void StopThreads()
        {
            // Yes, looks silly here, but if you have several threads like this, this is
            // very useful when the player dies or starts a new level
            //StopHeartBeat();
        }


    }
}
