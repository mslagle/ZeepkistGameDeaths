using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Zeepkist.Deaths.Deaths
{
    public static class DeathResourceManager
    {
        static List<DeathResource> deathResources = new List<DeathResource>();
        static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("DeathResourceManager");

        public static async Task PreLoadDeaths()
        {
            string dllFile = System.Reflection.Assembly.GetAssembly(typeof(DeathResourceManager)).Location;
            string dllDirectory = Path.GetDirectoryName(dllFile);

            Logger.LogInfo($"Preloading death resources at {dllDirectory}");

            foreach (DeathsEnum death in Enum.GetValues(typeof(DeathsEnum)))
            {
                Logger.LogInfo($"Working death type {death}");
                if (death == DeathsEnum.Disabled || death == DeathsEnum.Random)
                {
                    Logger.LogInfo($"Skipping loading any resources for type {death}");
                    continue;
                }

                var matchingFiles = Directory.GetFiles(dllDirectory).Where(x => Path.GetFileNameWithoutExtension(x).ToLower().IndexOf(death.ToString().ToLower()) >= 0 
                    && Path.GetExtension(x) == ".mp3");                
                Logger.LogInfo($"Found {matchingFiles.Count()} number of matches for this death");

                foreach (var file in matchingFiles)
                {
                    string mp3FileName = Path.GetFileName(file);
                    string textureFileName = Path.GetFileName(file.Replace(".mp3", ".png"));
                    Logger.LogInfo($"Loading resources for {death} at {mp3FileName} and {textureFileName}");

                    try
                    {
                        Texture2D texture = LoadTexture(file.Replace(".mp3", ".png"));
                        AudioClip audioClip = await GetAudioClip(file);

                        deathResources.Add(new DeathResource(death, texture, audioClip, file));
                    } 
                    catch (Exception e) 
                    {
                        Logger.LogError(e);
                    }
                }
            }
        }

        public static DeathResource GetRandomDeath(DeathsEnum deathType)
        {
            Logger.LogInfo($"Getting a resource for death type {deathType}");
            List<DeathResource> availableResources = new List<DeathResource>();

            if (deathType == DeathsEnum.Random)
            {
                availableResources = deathResources;
            } 
            else
            {
                availableResources = deathResources.Where(x => x.DeathType == deathType).ToList();
            }

            if (availableResources.Count == 0) 
            {
                Logger.LogWarning("No resources could be found for this death type!");
                return null;
            }

            int random = Random.Range(0, availableResources.Count);
            DeathResource found = availableResources[random];

            Logger.LogInfo($"Returning random resource with path of ${found.FilePath}");
            return found;
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

            AudioClip clip = null;
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
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

    public class DeathResource
    {
        public DeathsEnum DeathType { get; set; }
        public Texture2D Texture { get; set; }
        public AudioClip AudioClip { get; set; }
        public string FilePath { get; set; }

        public DeathResource(DeathsEnum deathType, Texture2D texture, AudioClip audioClip, string filePath)
        {
            this.DeathType = deathType;
            this.Texture = texture;
            this.AudioClip = audioClip;
            this.FilePath = filePath;
        }
    }


}
