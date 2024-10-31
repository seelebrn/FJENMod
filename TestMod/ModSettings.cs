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

        /// <summary>
        /// Initializes the ModSettings instance by deserializing from the settings file.
        /// </summary>
        public static void Initialize()
        {
            if (!Deserialize(out Instance))
            {
                Instance = new();
            }
        }

        /// <summary>
        /// Serializes the current settings to a file.
        /// </summary>
        /// <param name="filePath">The file path to save the settings.</param>
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

        /// <summary>
        /// Reloads the settings from the settings file.
        /// </summary>
        public static void Reload()
        {
            FJDebug.Log("Reloading");
            Deserialize(out Instance);
        }

        /// <summary>
        /// Applies all object modifiers to the current view.
        /// </summary>
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

        /// <summary>
        /// Deserializes the settings from the settings file.
        /// </summary>
        /// <param name="deserializedSettings">The deserialized ModSettings instance.</param>
        /// <returns>True if deserialization was successful, otherwise false.</returns>
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
                // Apply settings and patchers
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

        /// <summary>
        /// Applies the settings by patching all patchers.
        /// </summary>
        public static void ApplySettings()
        {
            foreach (PatcherInfo patcher in Instance.Patchers)
            {
                patcher.Patch();
            }
        }

        /// <summary>
        /// Gets the value of a setting by key.
        /// </summary>
        /// <typeparam name="T">The type of the setting value.</typeparam>
        /// <param name="key">The key of the setting.</param>
        /// <returns>The value of the setting if found and of the correct type, otherwise default.</returns>
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

        /// <summary>
        /// Applies all applicable object modifiers to the specified component.
        /// </summary>
        /// <param name="obj">The component to apply the modifiers to.</param>
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

        /// <summary>
        /// Tries to get all applicable object modifiers for the specified component.
        /// </summary>
        /// <param name="obj">The component to check for applicable modifiers.</param>
        /// <param name="applicableModifiers">The list of applicable object modifiers.</param>
        /// <returns>True if there are applicable modifiers, otherwise false.</returns>
        public static bool TryGetApplicableModifiers(Component obj, out List<ObjectModifier> applicableModifiers)
        {
            applicableModifiers = Instance.ObjectModifiers.Where(m => m.CanBeApplied(obj)).ToList();
            return applicableModifiers.Count > 0;
        }
    }
}