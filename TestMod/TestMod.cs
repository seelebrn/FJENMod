﻿using BepInEx;
using HarmonyLib;
using LitJson;
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
        public static Dictionary<string, string> translationDict;

        private static FileSystemWatcher watcher;

        public static ModSettings settings;
        public static Patchers patchers;

        public static HashSet<string> untranslated = new();
        public static HashSet<string> obsolete = new();
        public static HashSet<string> matched = new();
        Dictionary<string, string> keystoupdate = new();
        public static void InitializeTranslationDictionary(string dir)
        {
            string filePath = Path.Combine(Paths.PluginPath, "Translations", dir);
            ReloadDictionary(filePath);

            if (watcher == null)
            {
                watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath), Path.GetFileName(filePath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };

                watcher.Changed += (sender, e) => ReloadDictionary(filePath);
                watcher.EnableRaisingEvents = true;
            }
        }
        private static void ReloadDictionary(string filePath)
        {
            Dictionary<string, string> newDict = new();

            IEnumerable<string> lines = File.ReadLines(filePath);

            foreach (string line in lines)
            {
                string[] arr = line.Split('¤');
                if (arr[0] != arr[1])
                {
                    KeyValuePair<string, string> pair = new(Regex.Replace(arr[0], @"\t|\n|\r", ""), arr[1]);

                    if (!newDict.ContainsKey(pair.Key))
                        newDict.Add(pair.Key, pair.Value);
                    else
                        Debug.Log($"Found a duplicated line while parsing {filePath}: {pair.Key}");
                }
            }

            Debug.Log("Dictionary reloaded !");

            translationDict = newDict;
        }

        public static Harmony harmony;
        public void Awake()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            InitializeTranslationDictionary("KV.txt");
            Logger.LogInfo("Hello World ! Welcome to Cadenza's plugin !");
            harmony = new Harmony("Cadenza.IWOL.EnMod");
            InitializeSettings();
            InitializePatchers();
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }


        private void InitializePatchers()
        {
            string patchersPath = Path.Combine(Paths.PluginPath, "FJPatchers.txt");
            if (File.Exists(patchersPath))
            {
                patchers = Helpers.Deserialize<Patchers>(patchersPath);
                patchers.PatchAll();
            }
            else
            {
                patchers = new Patchers();
                patchers.patchers.Add(new TranspilerPatcher("test", "testmeh", new string[] { "aa", "ss" }));
                Helpers.Serialize(patchers, patchersPath);
            }
        }
        private void InitializeSettings()
        {
            string settingsPath = Path.Combine(Paths.PluginPath, "FJSettings.txt");
            if (File.Exists(settingsPath))
            {
                settings = Helpers.Deserialize<ModSettings>(settingsPath);
            }
            else
            {
                settings = new ModSettings();
                Helpers.Serialize(settings, settingsPath);
            }
        }

        //------------------------------------------------------------------------------------------
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.F1)) ExportStrings();
            if (Input.GetKeyUp(KeyCode.F2)) UpdateTranslations();
            if (Input.GetKeyUp(KeyCode.F3)) ScanAndDumpAssets();
        }
        private void ExportStrings()
        {
            Debug.Log("Cleaning a few things...");

            Helpers.DeleteFileIfExists(Path.Combine(Paths.PluginPath, "untranslated.txt"));
            Helpers.DeleteFileIfExists(Path.Combine(Paths.PluginPath, "obsolete.txt"));
            Helpers.DeleteFileIfExists(Path.Combine(Paths.PluginPath, "NewKV.txt"));

            Debug.Log("Exporting untranslated strings...");
            string untranslatedPath = Path.Combine(Paths.PluginPath, "untranslated.txt");
            using (StreamWriter untranslatedWriter = new(untranslatedPath, append: true))
            {
                foreach (string text in untranslated.Distinct())
                {
                    if (Helpers.IsChinese(text) && !string.IsNullOrEmpty(text) && !text.DoesMatchAny(@"\r", @"\n", "\r\n"))
                    {
                        untranslatedWriter.WriteLine(text.Replace("\r", "\\r").Replace("\n", "\\n"));
                    }
                }
            }
            Debug.Log("Successfully (I hope) exported untranslated strings... !");

            Debug.Log("Exporting obsolete strings...");
            string obsoletePath = Path.Combine(Paths.PluginPath, "obsolete.txt");
            using (StreamWriter obsoleteWriter = new(obsoletePath, append: true))
            {
                foreach (KeyValuePair<string, string> kvp in translationDict)
                {
                    if (!matched.Contains(kvp.Key) && !untranslated.Contains(kvp.Key) && !string.IsNullOrEmpty(kvp.Key))
                    {
                        obsoleteWriter.WriteLine($"{kvp.Key}¤{kvp.Value}");
                    }
                }
            }
            Debug.Log("Successfully (I hope) exported obsolete strings... !");

            Debug.Log("Creating your new KV...! ");
            string newKVPath = Path.Combine(Paths.PluginPath, "NewKV.txt");
            using (StreamWriter newKVWriter = new(newKVPath, append: true))
            {
                foreach (string matchedline in matched.Distinct())
                {
                    if (translationDict.TryGetValue(matchedline, out string value) && !string.IsNullOrEmpty(matchedline) && !matchedline.DoesMatchAny(@"\r", @"\n", "\r\n"))
                    {
                        newKVWriter.WriteLine($"{matchedline}¤{value}");
                    }
                }
            }
            Debug.Log("Successfully (I hope) created a new KV !");

            System.Media.SystemSounds.Beep.Play();
            System.Threading.Thread.Sleep(1000);
            System.Media.SystemSounds.Asterisk.Play();
            System.Threading.Thread.Sleep(1000);
            System.Media.SystemSounds.Exclamation.Play();
            System.Media.SystemSounds.Beep.Play();
            System.Threading.Thread.Sleep(1000);
            System.Media.SystemSounds.Asterisk.Play();
            System.Threading.Thread.Sleep(1500);
            System.Media.SystemSounds.Exclamation.Play();
            System.Threading.Thread.Sleep(1500);
            System.Media.SystemSounds.Question.Play();
            System.Threading.Thread.Sleep(1500);
            System.Media.SystemSounds.Beep.Play();
            System.Threading.Thread.Sleep(1000);
            System.Media.SystemSounds.Beep.Play();
            System.Threading.Thread.Sleep(1000);
        }
        private void UpdateTranslations()
        {
            UnityEngine.UI.Text[] alltext = FindObjectsOfType<UnityEngine.UI.Text>();
            TextMeshProUGUI[] alltmp = FindObjectsOfType<TextMeshProUGUI>();

            foreach (UnityEngine.UI.Text x in alltext)
            {
                if (translationDict.ContainsValue(x.text) && !keystoupdate.ContainsValue(x.text))
                {
                    keystoupdate.Add(translationDict.FirstOrDefault(zz => zz.Value == x.text).Key, x.text);
                    Debug.Log("KeyToUpdate filled with " + translationDict.FirstOrDefault(z => z.Value == x.text).Key + "¤" + x.text);
                }
            }

            foreach (TextMeshProUGUI y in alltmp)
            {
                if (translationDict.ContainsValue(y.text) && !keystoupdate.ContainsValue(y.text))
                {
                    keystoupdate.Add(translationDict.First(z => z.Value == y.text).Key, y.text);
                    Debug.Log("KeyToUpdate filled with " + translationDict.FirstOrDefault(z => z.Value == y.text).Key + "¤" + y.text);
                }
            }
            translationDict.Clear();
            Awake();
            LocalizationManager.Instance.SetLanguageID(0);
            foreach (UnityEngine.UI.Text x in alltext)
            {
                string chstring = keystoupdate.FirstOrDefault(zz => zz.Value == x.text).Key;
                if (x.text != "" && x.text != null)
                {
                    if (keystoupdate.ContainsValue(x.text) && translationDict.ContainsKey(chstring))
                    {
                        Debug.Log("Old Value = " + x.text);
                        Debug.Log("CH key = " + chstring);
                        Debug.Log("Newvalue = " + translationDict[chstring]);

                        if (x.text != translationDict[chstring])
                        {
                            x.text = translationDict[chstring];
                            keystoupdate.Remove(chstring);
                        }
                    }
                }
            }
            foreach (TextMeshProUGUI tmpro in alltmp)
            {
                if (!string.IsNullOrEmpty(tmpro.text))
                {
                    string chstring2 = keystoupdate.FirstOrDefault(zz => zz.Value == tmpro.text).Key;
                    if (keystoupdate.ContainsValue(tmpro.text) && translationDict.ContainsKey(chstring2))
                    {
                        Debug.Log($"Old Value = {tmpro.text}");
                        Debug.Log($"CH key = {chstring2}");
                        Debug.Log($"Newvalue = {translationDict[chstring2]}");

                        if (tmpro.text != translationDict[chstring2])
                        {
                            tmpro.text = translationDict[chstring2];
                            keystoupdate.Remove(chstring2);
                        }
                    }
                }
            }
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
                        Debug.Log("Now scanning : " + x.FullName);
                        Dump.LoadAssetsFile(x.FullName);
                    }
                }
            }
            DirectoryInfo di2 = new(Path.Combine(Paths.GameRootPath, "FromJianghu_Data", "StreamingAssets", "AssetBundles"));

            foreach (FileInfo x in di2.GetFiles())
            {
                if (!x.FullName.Contains("manifest"))
                {
                    Debug.Log("Now scanning : " + x.FullName);
                    Dump.LoadAssetBundles(x.FullName);
                }
            }

            Helpers.DeleteFileIfExists(Path.Combine(Paths.PluginPath, "UITextUN.txt"));

            foreach (string s in untranslated.Distinct())
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
    }

}