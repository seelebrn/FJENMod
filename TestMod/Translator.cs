﻿using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;

namespace FromJianghuENMod
{
    /// <summary>
    /// Provides translation functionalities for the mod, including loading translations, 
    /// attempting translations, and exporting untranslated and obsolete strings.
    /// </summary>
    public class Translator
    {
        public static Dictionary<string, string> translationDict;
        public static HashSet<string> untranslatedLastLoaded = new();
        public static HashSet<string> untranslatedCurrent = new();
        public static HashSet<string> obsolete = new();
        public static HashSet<string> matched = new();

        /// <summary>
        /// Gets the path to the KV.txt file containing translations.
        /// </summary>
        public static string KVPath => Path.Combine(Paths.PluginPath, "Translations", "KV.txt");

        /// <summary>
        /// Gets the path to the NewKV.txt file for new translations.
        /// </summary>
        public static string NewKVPath => Path.Combine(Paths.PluginPath, "NewKV.txt");

        /// <summary>
        /// Gets the path to the untranslated.txt file for untranslated strings.
        /// </summary>
        public static string UntranslatedPath => Path.Combine(Paths.PluginPath, "untranslated.txt");

        /// <summary>
        /// Gets the path to the obsolete.txt file for obsolete translations.
        /// </summary>
        public static string ObsoletePath => Path.Combine(Paths.PluginPath, "obsolete.txt");

        public static void Initialize()
        {
            ReloadDictionary();
        }

        /// <summary>
        /// Reloads the translation dictionary from the KV.txt file.
        /// </summary>
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

        /// <summary>
        /// Attempts to translate a given string key.
        /// </summary>
        /// <param name="key">The string key to translate.</param>
        /// <param name="translatedString">The translated string if found, otherwise the original key.</param>
        /// <returns>True if the translation was successful, otherwise false.</returns>
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

        /// <summary>
        /// Updates the untranslated.txt file with new untranslated strings.
        /// </summary>
        public static void UpdateUntranslatedTextFile()
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
            }
        }

        /// <summary>
        /// Exports untranslated and obsolete strings to their respective files and creates a new KV file.
        /// </summary>
        public static void ExportStrings()
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

        /// <summary>
        /// Updates the translations in the game by checking for new translations and applies them.
        /// </summary>
        public static void UpdateTranslations()
        {
            Dictionary<string, string> keysToUpdate = new();

            UnityEngine.UI.Text[] alltext = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>();
            TextMeshProUGUI[] alltmp = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();

            void StoreTranslatedStringsForUpdating(Func<IEnumerable<string>> func)
            {
                foreach (string s in func())
                {
                    if (translationDict.ContainsValue(s) && !keysToUpdate.ContainsValue(s))
                    {
                        string chstring = translationDict.FirstOrDefault(translation => translation.Value == s).Key;
                        if (!string.IsNullOrEmpty(chstring) && !string.IsNullOrEmpty(s))
                        {
                            keysToUpdate[s] = chstring;
                            FJDebug.Log($"KeyToUpdate filled with {s}={chstring}");
                        }
                    }
                }
            }
            void UpdateText(Func<IEnumerable<string>> func, Action<string, int> setter)
            {
                List<string> strings = func().ToList();
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
    }
}
