using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using Zeepkist.Deaths.Deaths;

namespace Zeepkist.Deaths
{

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("ZeepSDK")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        public static ConfigEntry<string> Death { get; private set; }
        public static ConfigEntry<int> DeathTime { get; private set; }

        private void Awake()
        {
            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            Plugin.Death = this.Config.Bind<string>("Mod", "Death", DeathsEnum.DarkSouls.ToString(), 
                new ConfigDescription("Death scene to play when dead",
                new AcceptableValueList<string>(Enum.GetNames(typeof(DeathsEnum)))));
            Plugin.Death.SettingChanged += Death_SettingChanged;

            Plugin.DeathTime = this.Config.Bind<int>("Mod", "Death Time", 5, "Number of seconds of how long death screen stays active");
            Plugin.DeathTime.SettingChanged += DeathTime_SettingChanged;

            DeathManager.Initialize();
            DeathManager.currentDeathType = Enum.Parse<DeathsEnum>(Death.Value);
            DeathManager.deathTime = DeathTime.Value;
        }

        private void DeathTime_SettingChanged(object sender, EventArgs e)
        {
            Logger.LogInfo($"Mod status changed.  New DeathTime = {DeathTime.Value}");
            DeathManager.deathTime = DeathTime.Value;
        }

        private void Death_SettingChanged(object sender, EventArgs e)
        {
            Logger.LogInfo($"Mod status changed.  New status = {Death.Value}");
            DeathManager.currentDeathType = Enum.Parse<DeathsEnum>(Death.Value);
        }

        public void OnGUI()
        {
            if (Enum.Parse<DeathsEnum>(Death.Value) != DeathsEnum.Disabled)
            {
                DeathManager.OnGui();
            }
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}