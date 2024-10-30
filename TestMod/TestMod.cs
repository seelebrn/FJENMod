using BepInEx;
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

        private static StreamWriter untranslatedWriter;

        private static FileSystemWatcher watcher;

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
                        FJDebug.Log($"Found a duplicated line while parsing {filePath}: {pair.Key}");
                }
            }

            FJDebug.Log("Dictionary reloaded !");

            translationDict = newDict;
        }

        public static Harmony harmony;
        public void Awake()
        {
            harmony = new Harmony("Cadenza.IWOL.EnMod");
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            ModSettings.Initialize();
            InitializeTranslationDictionary("KV.txt");
            ModSettings.ApplySettings();
            Logger.LogInfo("Hello World ! Welcome to Cadenza's plugin !");
            harmony.PatchAll();
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
            untranslatedWriter?.Close();
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
            else if (ModSettings.GetSettingValue<bool>("unloadUntranslatedStrings"))
            {
                if (Helpers.IsChineseOnly(key) && untranslated.Add(key))
                {
                    FJDebug.Log($"Failed to find translation for key: {key}. Putting it in untranslated list.");
                    untranslated.Add(key);
                    UpdateUntranslatedTextFile();
                }
            }

            return translatedString != key;
        }

        //------------------------------------------------------------------------------------------
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.F1)) ExportStrings();
            if (Input.GetKeyUp(KeyCode.F2)) UpdateTranslations();
            if (Input.GetKeyUp(KeyCode.F3)) ScanAndDumpAssets();
        }

        private static void UpdateUntranslatedTextFile()
        {
            string pathToUntranslated = Path.Combine(Paths.PluginPath, "untranslated.txt");

            untranslatedWriter ??= new(pathToUntranslated, append: true);

            foreach (string text in untranslated)
            {
                if (Helpers.IsChinese(text) &&
                    !string.IsNullOrEmpty(text) &&
                    !text.DoesMatchAny(@"\r", @"\n", "\r\n"))
                {
                    untranslatedWriter.WriteLine(text);
                }
            }

            untranslatedWriter.Flush();
        }

        private void ExportStrings()
        {
            FJDebug.Log("Cleaning a few things...");

            Helpers.DeleteFileIfExists(Path.Combine(Paths.PluginPath, "untranslated.txt"));
            Helpers.DeleteFileIfExists(Path.Combine(Paths.PluginPath, "obsolete.txt"));
            Helpers.DeleteFileIfExists(Path.Combine(Paths.PluginPath, "NewKV.txt"));

            FJDebug.Log("Exporting untranslated strings...");
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
            FJDebug.Log("Successfully (I hope) exported untranslated strings... !");

            FJDebug.Log("Exporting obsolete strings...");
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
            FJDebug.Log("Successfully (I hope) exported obsolete strings... !");

            FJDebug.Log("Creating your new KV...! ");
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
            FJDebug.Log("Successfully (I hope) created a new KV !");

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
                    FJDebug.Log("KeyToUpdate filled with " + translationDict.FirstOrDefault(z => z.Value == x.text).Key + "¤" + x.text);
                }
            }

            foreach (TextMeshProUGUI y in alltmp)
            {
                if (translationDict.ContainsValue(y.text) && !keystoupdate.ContainsValue(y.text))
                {
                    keystoupdate.Add(translationDict.First(z => z.Value == y.text).Key, y.text);
                    FJDebug.Log("KeyToUpdate filled with " + translationDict.FirstOrDefault(z => z.Value == y.text).Key + "¤" + y.text);
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
                        FJDebug.Log("Old Value = " + x.text);
                        FJDebug.Log("CH key = " + chstring);
                        FJDebug.Log("Newvalue = " + translationDict[chstring]);

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
                        FJDebug.Log($"Old Value = {tmpro.text}");
                        FJDebug.Log($"CH key = {chstring2}");
                        FJDebug.Log($"Newvalue = {translationDict[chstring2]}");

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