using BehaviorDesigner.Runtime.Tasks.Unity.UnityString;
using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TestMod;
using TMPro;
using UnityEngine;

namespace FromJianghuENMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class FromJianghuENMod : BaseUnityPlugin
    {
        public const string pluginGuid = "Cadenza.IWOL.EnMod";
        public const string pluginName = "FJ ENMod Continued";
        public const string pluginVersion = "0.6";
        public static Dictionary<string, string> UIText = new();

        private float lastUntranslatedUpdate = 0;
        private float UntranslatedUpdateInterval => (float)ModSettings.GetSettingValue<int>("unloadUntranslatedStringsInterval");

        public static Harmony harmony;
        public void Awake()
        {
            harmony = new Harmony("Cadenza.IWOL.EnMod");
            FJDebug.ClearAllLogs();
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            ModSettings.Initialize();
            Translator.Initialize();
            ModSettings.ApplySettings();
            Logger.LogInfo("Hello World ! Welcome to Cadenza's plugin !");
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.F1)) Translator.ExportStrings();
            else if (Input.GetKey(KeyCode.F2)) Translator.UpdateTranslations();
            else if (Input.GetKey(KeyCode.F3)) ScanAndDumpAssets();
            else if (Input.GetKey(KeyCode.F4)) ReloadModifiersAndApply();

            if (Time.time - lastUntranslatedUpdate >= UntranslatedUpdateInterval)
                Translator.UpdateUntranslatedTextFile();
        }

        private void ScanAndDumpAssets()
        {
            DirectoryInfo di = new(Path.Combine(Paths.GameRootPath, "FromJianghu_Data"));

            foreach (FileInfo x in di.GetFiles())
            {
                if (x.FullName.Contains(".assets") || x.FullName.Contains("sharedassets"))
                {
                    if (!x.FullName.Contains("resS"))
                    {
                        FJDebug.Log("Now scanning : " + x.FullName);
                        Dump.LoadAssetsFile(x.FullName);
                    }
                }
            }
            DirectoryInfo di2 = new(Path.Combine(Paths.GameRootPath, "FromJianghu_Data", "StreamingAssets", "AssetBundles"));

            foreach (FileInfo x in di2.GetFiles())
            {
                if (!x.FullName.Contains("manifest"))
                {
                    FJDebug.Log("Now scanning : " + x.FullName);
                    Dump.LoadAssetBundles(x.FullName);
                }
            }

            Helpers.DeleteFileIfExists(Path.Combine(Paths.PluginPath, "UITextUN.txt"));

            foreach (string s in Translator.untranslatedCurrent.Distinct())
            {
                using (StreamWriter tw = new(Path.Combine(Paths.PluginPath, "UITextUN.txt"), append: true))
                {
                    if (!UIText.Keys.Contains(s))
                    {
                        tw.Write(Regex.Unescape(s + Environment.NewLine));
                    }
                }
            }
        }

        private void ReloadModifiersAndApply()
        {
            ModSettings.Reload();
            //ModSettings.ApplyAllModifiersToCurrentView();
        }
    }
}