using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Zeepkist.Deaths.Deaths
{
    internal static class DeathResourceManager
    {
        static DeathsEnum LastDeathTexture = DeathsEnum.Disabled;
        static DeathsEnum LastDeathAudio = DeathsEnum.Disabled;

        static Texture2D texture = null;
        static AudioClip audioclip = null;

        static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("DeathResourceManager");

        public static Texture2D GetDeathTexture(DeathsEnum deathsEnum)
        {
            if (deathsEnum == LastDeathTexture && texture != null)
            {
                return texture;
            }

            LastDeathTexture = deathsEnum;
            if (deathsEnum == DeathsEnum.DarkSouls)
            {
                texture = LoadTexture("darksouls.png");
            }
            if (deathsEnum == DeathsEnum.GTA)
            {
                texture = LoadTexture("gta.png");
            }
            if (deathsEnum == DeathsEnum.MortalKombat)
            {
                texture = LoadTexture("mk.png");
            }

            return texture;
        }

        public static async Task<AudioClip> GetDeathAudio(DeathsEnum deathsEnum)
        {
            if (deathsEnum == LastDeathAudio && audioclip != null)
            {
                return audioclip;
            }

            LastDeathAudio = deathsEnum;
            if (deathsEnum == DeathsEnum.DarkSouls)
            {
                audioclip = await GetAudioClip("darksouls.mp3");
            }
            if (deathsEnum == DeathsEnum.GTA)
            {
                audioclip = await GetAudioClip("gta.mp3");
            }
            if (deathsEnum == DeathsEnum.MortalKombat)
            {
                audioclip = await GetAudioClip("mk.mp3");
            }

            return audioclip;
        }

        private static Texture2D LoadTexture(string path)
        {
            Logger.LogInfo($"Loading a texture from path {path}");

            string dllFile = System.Reflection.Assembly.GetAssembly(typeof(DeathResourceManager)).Location;
            string dllDirectory = Path.GetDirectoryName(dllFile);
            string imagePath = Path.Combine(dllDirectory, path);

            byte[] imageBytes = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(imageBytes);

            return texture;
        }

        public static async Task<AudioClip> GetAudioClip(string path)
        {
            Logger.LogInfo($"Creating an audio clip from path {path}");

            string dllFile = System.Reflection.Assembly.GetAssembly(typeof(DeathResourceManager)).Location;
            string dllDirectory = Path.GetDirectoryName(dllFile);
            string audioPath = Path.Combine(dllDirectory, path);

            AudioClip clip = null;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.MPEG))
            {
                uwr.SendWebRequest();

                // wrap tasks in try/catch, otherwise it'll fail silently
                try
                {
                    while (!uwr.isDone) await Task.Delay(5);

                    if (uwr.result == UnityWebRequest.Result.ConnectionError)
                    {
                        Logger.LogError($"{uwr.error}");
                    }
                    else
                    {
                        clip = DownloadHandlerAudioClip.GetContent(uwr);
                    }
                }
                catch (Exception err)
                {
                    Logger.LogInfo($"{err.Message}, {err.StackTrace}");
                }
            }

            return clip;
        }
    }
}
