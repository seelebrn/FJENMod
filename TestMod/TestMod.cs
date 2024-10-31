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
        public static Dictionary<string, string> translationDict;

        private float lastUntranslatedUpdate = 0;
        private float UntranslatedUpdateInterval => (float)ModSettings.GetSettingValue<int>("unloadUntranslatedStringsInterval");

        public static HashSet<string> untranslatedLastLoaded = new();
        public static HashSet<string> untranslatedCurrent = new();
        public static HashSet<string> obsolete = new();
        public static HashSet<string> matched = new();

        public static string KVPath => Path.Combine(Paths.PluginPath, "Translations", "KV.txt");
        public static string NewKVPath => Path.Combine(Paths.PluginPath, "NewKV.txt");
        public static string UntranslatedPath => Path.Combine(Paths.PluginPath, "untranslated.txt");
        public static string ObsoletePath => Path.Combine(Paths.PluginPath, "obsolete.txt");

        public static void InitializeTranslationDictionary()
        {
            ReloadDictionary();
        }
        private static void ReloadDictionary()
        {
            translationDict = new();

            foreach (string line in File.ReadLines(KVPath))
            {
                if (string.IsNullOrEmpty(line)) continue;

                string[] arr = line.Split('¤');
                if (arr.Length < 2 || arr[0] == arr[1]) continue;

                string key = Regex.Replace(arr[0], @"\t|\n|\r", "");
                if (!translationDict.ContainsKey(key))
                {
                    translationDict[key] = arr[1];
                }
                else
                {
                    FJDebug.Log($"Found a duplicated line while parsing KV.txt: {key}");
                }
            }

            FJDebug.Log("Dictionary reloaded !");
        }

        public static Harmony harmony;
        public void Awake()
        {
            harmony = new Harmony("Cadenza.IWOL.EnMod");
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            ModSettings.Initialize();
            InitializeTranslationDictionary();
            ModSettings.ApplySettings();
            Logger.LogInfo("Hello World ! Welcome to Cadenza's plugin !");
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        /// <summary>
        /// Tries to get the translation for a given key
        /// </summary>
        /// <param name="key">Original string</param>
        /// <param name="translatedString">Translation fetched from the dictionary</param>
        /// <returns>True if the key is chinese and has been found in the dictionary</returns>
        public static bool TryTranslatingString(string key, out string translatedString)
        {
            key = key.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"");
            translatedString = key;

            if (Helpers.IsChinese(key) && translationDict.TryGetValue(key, out string value))
            {
                translatedString = value;
            }
            else if (Helpers.IsChineseOnly(key) && untranslatedCurrent.Add(key))
            {
                FJDebug.Log($"Failed to find translation for key: {key}. Putting it in untranslated list.");

            }

            return translatedString != key;
        }

        //------------------------------------------------------------------------------------------
        private void Update()
        {
            if (Input.GetKey(KeyCode.F1)) ExportStrings();
            else if (Input.GetKey(KeyCode.F2)) UpdateTranslations();
            else if (Input.GetKey(KeyCode.F3)) ScanAndDumpAssets();
            else if (Input.GetKey(KeyCode.F4)) ReloadModifiersAndApply();

            if (Time.time - lastUntranslatedUpdate >= UntranslatedUpdateInterval)
                UpdateUntranslatedTextFile();
        }


        private void UpdateUntranslatedTextFile()
        {
            if (ModSettings.GetSettingValue<bool>("unloadUntranslatedStrings"))
            {
                List<string> newStrings = untranslatedCurrent.Except(untranslatedLastLoaded).ToList();

                if (newStrings.Count > 0)
                {
                    if (!File.Exists(UntranslatedPath))
                        File.Create(UntranslatedPath).Dispose();

                    FJDebug.Log($"New untranslated strings detected: {newStrings.Count}. Appending to untranslated.txt");

                    using (StreamWriter untranslatedWriter = new(UntranslatedPath, append: true))
                    {
                        foreach (string s in newStrings)
                        {
                            untranslatedWriter.WriteLine(s);
                        }
                    }
                    untranslatedLastLoaded = new HashSet<string>(untranslatedCurrent);
                }
                lastUntranslatedUpdate = Time.time;
            }
        }

        private void ExportStrings()
        {
            FJDebug.Log("Cleaning a few things...");

            Helpers.DeleteFileIfExists(UntranslatedPath);
            Helpers.DeleteFileIfExists(ObsoletePath);
            Helpers.DeleteFileIfExists(KVPath);

            FJDebug.Log("Exporting untranslated strings...");

            using (StreamWriter untranslatedWriter = new(UntranslatedPath, append: true))
            {
                foreach (string text in untranslatedCurrent)
                {
                    if (Helpers.IsChinese(text) && !string.IsNullOrEmpty(text) && !text.DoesMatchAny(@"\r", @"\n", "\r\n"))
                    {
                        untranslatedWriter.WriteLine(text.Replace("\r", "\\r").Replace("\n", "\\n"));
                    }
                }
            }
            FJDebug.Log("Successfully (I hope) exported untranslated strings... !");

            FJDebug.Log("Exporting obsolete strings...");
            using (StreamWriter obsoleteWriter = new(ObsoletePath, append: true))
            {
                foreach (KeyValuePair<string, string> kvp in translationDict)
                {
                    if (!string.IsNullOrEmpty(kvp.Key) && !matched.Contains(kvp.Key) && !untranslatedCurrent.Contains(kvp.Key))
                    {
                        obsoleteWriter.WriteLine($"{kvp.Key}¤{kvp.Value}");
                    }
                }
            }
            FJDebug.Log("Successfully (I hope) exported obsolete strings... !");

            FJDebug.Log("Creating your new KV...! ");
            using (StreamWriter newKVWriter = new(NewKVPath, append: true))
            {
                foreach (string matchedline in matched.Distinct())
                {
                    if (!string.IsNullOrEmpty(matchedline) && translationDict.TryGetValue(matchedline, out string value) && !!matchedline.DoesMatchAny(@"\r", @"\n", "\r\n"))
                    {
                        newKVWriter.WriteLine($"{matchedline}¤{value}");
                    }
                }
            }
            FJDebug.Log("Successfully (I hope) created a new KV !");

            System.Media.SystemSounds.Asterisk.Play();
        }
        private void UpdateTranslations()
        {
            Dictionary<string, string> keysToUpdate = new();

            UnityEngine.UI.Text[] alltext = FindObjectsOfType<UnityEngine.UI.Text>();
            TextMeshProUGUI[] alltmp = FindObjectsOfType<TextMeshProUGUI>();

            void StoreTranslatedStringsForUpdating(Func<IEnumerable<string>> func)
            {
                foreach (string s in func())
                {
                    if (translationDict.ContainsValue(s) && !keysToUpdate.ContainsValue(s))
                    {
                        string chstring = translationDict.FirstOrDefault(translation => translation.Value == s).Key;
                        if (!string.IsNullOrEmpty(chstring) && !string.IsNullOrEmpty(s))
                        {
                            keysToUpdate[s]=chstring;
                            FJDebug.Log($"KeyToUpdate filled with {s}={chstring}");
                        }
                    }
                }
            }
            void UpdateText(Func<IEnumerable<string>> func, Action<string, int> setter)
            {
                var strings = func().ToList();
                for (int index = 0; index < strings.Count; index++)
                {
                    //s may be either untranslated or using outdated translation
                    string s = strings[index];
                    string changedString = s;

                    if (string.IsNullOrEmpty(s)) continue;

                    //Check in case the string is untranslated
                    if (!translationDict.TryGetValue(s, out changedString))
                    {
                        FJDebug.Log($"String not found in dictionary: {s}. Trying to find it in keysToUpdate");
                        FJDebug.Log(keysToUpdate.ContainsKey(s));
                        FJDebug.Log(keysToUpdate.ContainsValue(s));
                        //If keysToUpdate has the key, the value will be the original string
                        if (keysToUpdate.TryGetValue(s, out string originalString))
                        {
                            FJDebug.Log($"Found the key in keysToUpdate: {originalString}¤{s}. Trying to fetch translation again");
                            //Try to translate the original string after fetching it
                            if (translationDict.TryGetValue(originalString, out changedString))
                            {
                                FJDebug.Log($"Found the translation for the key: {originalString}¤{s}¤{changedString}");
                            }
                            else
                            {
                                FJDebug.Log($"Failed to find the translation for the key: {s} after all attempts");
                            }

                            //Set actual string if it has changed
                            if (changedString != s && !string.IsNullOrEmpty(changedString))
                            {
                                setter(changedString, index);
                                FJDebug.Log($"Old Value = {s}");
                                FJDebug.Log($"Newvalue = {changedString}");
                            }
                        }
                    }
                }
            }

            StoreTranslatedStringsForUpdating(() => alltext.Select(x => x.text));
            StoreTranslatedStringsForUpdating(() => alltmp.Select(x => x.text));

            ReloadDictionary();

            LocalizationManager.Instance.SetLanguageID(0);

            UpdateText(() => alltext.Select(x => x.text), (s, index) => alltext[index].text = s);
            UpdateText(() => alltmp.Select(x => x.text), (s, index) => alltmp[index].text = s);
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

            foreach (string s in untranslatedCurrent.Distinct())
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