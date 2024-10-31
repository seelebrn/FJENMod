using BepInEx;
using FromJianghuENMod;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FromJianghuENMod
{
    [Serializable]
    public class ModSettings
    {
        public static ModSettings Instance;
        private static string SettingsPath => Path.Combine(Paths.PluginPath, "FJSettings.txt");
        private Dictionary<string, object> GeneralSettings { get; set; }
        private List<PatcherInfo> Patchers { get; set; } = new();
        private List<ObjectModifier> ObjectModifiers { get; set; } = new();

        public ModSettings()
        {
            GeneralSettings = new();
            Patchers = new();
        }

        public static void Initialize()
        {
            if (!Deserialize(out Instance))
            {
                Instance = new();
            }
        }

        private void Serialize(string filePath)
        {
            using StreamWriter writer = new(filePath);
            writer.WriteLine("[General Settings]");

            foreach (KeyValuePair<string, object> setting in GeneralSettings)
            {
                writer.WriteLine($"{setting.Key} : {setting.Value}");
            }

            writer.WriteLine("[Patchers]");
            foreach (PatcherInfo patcher in Patchers)
            {
                writer.WriteLine($"{patcher}");
            }
        }

        public static void Reload()
        {
            FJDebug.Log("Reloading");
            Deserialize(out Instance);
        }

        public static void ApplyAllModifiersToCurrentView()
        {
            var allInOne = GameObject.FindObjectsOfType<Component>()
                .Where(c => c is Image or VerticalLayoutGroup or HorizontalLayoutGroup or GridLayoutGroup or TextMeshProUGUI);

            FJDebug.Log($"Applying all. {allInOne.Count()}. modifier count: {Instance.ObjectModifiers.Count}");

            foreach (Component component in allInOne)
            {
                if (component)
                {
                    ApplyApplicableModifiers(component);
                }
                else
                {
                    FJDebug.LogError($"Component is null");
                }
            }
        }

        private static bool Deserialize(out ModSettings deserializedSettings)
        {
            if (!File.Exists(SettingsPath))
            {
                deserializedSettings = null;
                return false;
            }

            deserializedSettings = new();
            string[] lines = File.ReadAllLines(SettingsPath);

            string currentSection = "";

            Instance.Patchers.Clear();
            Instance.ObjectModifiers.Clear();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("/")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line;
                    continue;
                }
                //Apply settings and patchers
                if (currentSection == "[General Settings]")
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        if (int.TryParse(value, out int intValue))
                            Instance.GeneralSettings[key] = intValue;
                        else if (float.TryParse(value, out float floatValue))
                            Instance.GeneralSettings[key] = floatValue;
                        else if (bool.TryParse(value, out bool boolValue))
                            Instance.GeneralSettings[key] = boolValue;
                        else
                            Instance.GeneralSettings[key] = value;
                    }
                }
                else if (currentSection == "[Patchers]")
                {
                    if (PatcherInfo.TryCreatePatcherFromString(line, out PatcherInfo patcher))
                        Instance.Patchers.Add(patcher);
                }
                else if (ObjectModifierFactory.CreateObjectModifier(currentSection, line, out ObjectModifier instance))
                {
                    Instance.ObjectModifiers.Add(instance);
                }
            }

            return true;
        }

        public static void ApplySettings()
        {
            foreach (PatcherInfo patcher in Instance.Patchers)
            {
                patcher.Patch();
            }
        }

        public static T GetSettingValue<T>(string key)
        {
            if (Instance.GeneralSettings.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                    return typedValue;
                else
                    FJDebug.Log($"Failed to get {key} value as {typeof(T)} as it's a different type.");
            }
            else
            {
                FJDebug.Log($"Failed to get {key} value as it's not in the dictionary.");
            }
            return default;
        }
        public static void ApplyApplicableModifiers(Component obj)
        {
            if (TryGetApplicableModifiers(obj, out List<ObjectModifier> applicableModifiers))
            {
                foreach (ObjectModifier modifier in applicableModifiers)
                {
                    modifier.Apply(obj);
                }
            }
        }
        public static bool TryGetApplicableModifiers(Component obj, out List<ObjectModifier> applicableModifiers)
        {
            applicableModifiers = Instance.ObjectModifiers.Where(m => m.CanBeApplied(obj)).ToList();
            return applicableModifiers.Count > 0;
        }
    }
}