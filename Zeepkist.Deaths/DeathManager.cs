using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine;
using ZeepSDK.Racing;
using Zeepkist.Deaths.Deaths;
using BepInEx.Logging;
using Rewired;

namespace Zeepkist.Deaths
{
    static class DeathManager
    {
        public static DeathsEnum currentDeathType = DeathsEnum.Disabled;

        public static bool isDead = false;
        public static bool isFinished = false;

        public static DateTime lastDiedAt = DateTime.MinValue;
        public static int deathTime = 0;

        public static AudioClip deathAudioClip = null;
        public static Texture2D deathTexture = null;

        public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("DeathManager");

        public static void Initialize()
        {
            RacingApi.CrossedFinishLine += RacingApi_CrossedFinishLine;
            RacingApi.Crashed += RacingApi_Crashed;
            RacingApi.PlayerSpawned += RacingApi_PlayerSpawned;
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            DeathResourceManager.PreLoadDeaths().ContinueWith(t => Console.WriteLine(t.Exception),TaskContinuationOptions.OnlyOnFaulted);
        }

        private static void RacingApi_CrossedFinishLine(float time)
        {
            Logger.LogInfo($"Detected crossing finish line, setting isDead = false and isFinished = true");
            isDead = false;
            isFinished = true;
        }

        private static void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            Logger.LogInfo($"Detected scene change, setting isDead = false");
            isDead = false;
            isFinished = false;
        }

        public static void RacingApi_Crashed(CrashReason reason)
        {
            if (currentDeathType == DeathsEnum.Disabled)
            {
                Logger.LogInfo($"Detected crash, but mod is disabled.  Not setting isDead.");
                return;
            }

            if (isFinished)
            {
                Logger.LogInfo($"Detected crash, but isFinished is true.  Not setting isDead");
                isFinished = false;
                return;
            }

            Logger.LogInfo($"Detected crash, getting death resources and setting isDead = true");
            lastDiedAt = DateTime.Now;
            isDead = true;
            isFinished = false;

            DeathResource deathResource = DeathResourceManager.GetRandomDeath(currentDeathType);
            deathAudioClip = deathResource.AudioClip;
            deathTexture = deathResource.Texture;

            try
            {
                AudioManager audioManager = DeathManager.GetOrCreateAudioManager();
                audioManager.Play(new AudioItemScriptableObject() { Clip = deathAudioClip, BaseVolume = 1f, Loop = false });

                Logger.LogInfo($"Successfully played audio clip");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

        }

        private static void RacingApi_PlayerSpawned()
        {
            Logger.LogInfo($"Detected player spawn, setting isDead = false");
            isDead = false;
            isFinished = false;
        }

        public static void OnGui()
        {
            if (!isDead || currentDeathType == DeathsEnum.Disabled)
            {
                return;
            }

            // Only keep death screen up for a set amount of time
            if (lastDiedAt < DateTime.Now.AddSeconds(-deathTime)){
                Logger.LogInfo("Time is up for the death screen, setting isDead = false");
                isDead = false;
                isFinished = false;
            }

            // Label style
            GUIStyle labelStyle = new GUIStyle(GUI.skin.window);
            labelStyle.wordWrap = true;
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = Mathf.FloorToInt(Screen.height / 5);
            labelStyle.normal.textColor = Color.red;

            // Label content
            GUIContent labelContent = new GUIContent(deathTexture);

            // Label location
            Vector2 labelSize = labelStyle.CalcSize(labelContent);
            int padding = Mathf.CeilToInt(Screen.width);
            Vector2 newSize = new Vector2(labelSize.x + padding, labelSize.y + padding);
            Rect boxRect = new Rect(0, 0, 0, 0);
            boxRect.width = newSize.x;
            boxRect.height = newSize.y;
            boxRect.position = new Vector2(Display.main.renderingWidth / 2 - boxRect.width / 2, Display.main.renderingHeight / 2 - boxRect.height / 2);

            GUI.Box(boxRect, labelContent, labelStyle);
        }

        public static AudioManager GetOrCreateAudioManager()
        {
            if (AudioManager.Instance == null)
            {
                Logger.LogInfo($"AudioManager is null, creating a new instance");

                AudioManager audioManager = new AudioManager();
                AudioManager.Instance = audioManager;
            }

            return AudioManager.Instance;
        }
    }
}