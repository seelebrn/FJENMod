using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FromJianghuENMod
{
    [Serializable]
    public class ModSettings
    {
        public static ModSettings Instance;

        private Dictionary<string, object> GeneralSettings { get; set; }
        private List<PatcherInfo> Patchers { get; set; } = new List<PatcherInfo>();
        public ModSettings()
        {
            GeneralSettings = new();
            Patchers = new();
        }
        public ModSettings(string filePath)
        {
            GeneralSettings = new();
            Patchers = new();
            Serialize(filePath);
        }
        public static void Initialize()
        {
            string settingsPath = Path.Combine(Paths.PluginPath, "FJSettings.txt");
            if (!Deserialize(settingsPath, out Instance))
            {
                Debug.Log("NONO DESERIALIZED");
                Instance = new(settingsPath);
            }
            else
            {

                Debug.Log("happy");
            }
        }
        // Serialize the settings to a file
        private void Serialize(string filePath)
        {
            using (StreamWriter writer = new(filePath))
            {
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
        }

        // Deserialize the settings from a file
        private static bool Deserialize(string filePath, out ModSettings deserializedSettings)
        {
            if (!File.Exists(filePath))
            {
                deserializedSettings = null;
                return false;
            }

            deserializedSettings = new();
            string[] lines = File.ReadAllLines(filePath);

            string currentSection = "";

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("/")) continue;

                //if the string is within the [] brackets, it is a section
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line;
                    continue;
                }

                //Do stuff based on the current section
                switch (currentSection)
                {
                    case "[General Settings]":
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            object value = parts[1].Trim();

                            //try to convert the value to primitive types
                            if (int.TryParse(value.ToString(), out int intValue))
                                Instance.GeneralSettings[key] = intValue;
                            else if (float.TryParse(value.ToString(), out float floatValue))
                                Instance.GeneralSettings[key] = floatValue;
                            else if (bool.TryParse(value.ToString(), out bool boolValue))
                                Instance.GeneralSettings[key] = boolValue;
                            else
                                Instance.GeneralSettings[key] = value;
                        }
                        break;
                    case "[Patchers]":
                        if (PatcherInfo.TryCreatePatcherFromString(line, out PatcherInfo patcher))
                            Instance.Patchers.Add(patcher);
                        break;
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
    }

    // Class to store patcher information
    [Serializable]
    public class PatcherInfo
    {
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public List<string> Parameters { get; set; }
        public string PatchType { get; set; }

        public override string ToString() => $"{ClassName}.{MethodName}({string.Join(",", Parameters)}).{PatchType}";

        private MethodInfo OriginalMethodInfo
        {
            get
            {
                if (Parameters == null || Parameters.Count == 0)
                {
                    FJDebug.Log($"resolving classname {ClassName}");
                    return AccessTools.Method(Helpers.ResolveType(ClassName), MethodName);
                }
                else
                    return AccessTools.Method(Helpers.ResolveType(ClassName), MethodName, Parameters.Select(Helpers.ResolveType).ToArray());
            }
        }
        public PatcherInfo(string className, string methodName, List<string> parameters, string patchType)
        {
            ClassName = className;
            MethodName = methodName;
            Parameters = parameters;
            PatchType = patchType;
        }
        public static bool TryCreatePatcherFromString(string sourceString, out PatcherInfo result)
        {
            Match match = Regex.Match(sourceString, @"(?<class>[\w\.]+)\.(?<method>\w+)\((?<params>[^\)]*)\)\.(?<patchType>\w+)");
            if (match.Success)
            {
                string className = match.Groups["class"].Value;
                string methodName = match.Groups["method"].Value;
                string[] parameters = match.Groups["params"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string patchType = match.Groups["patchType"].Value;

                //try qualify primitive types
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (Helpers.FullyQualifyTypes(parameters[i], out string qualifiedTypeName))
                        parameters[i] = qualifiedTypeName;
                }


                result = new PatcherInfo(className, methodName, new(parameters), patchType);
                FJDebug.Log($"Created a PatcherInfo successfully! {result}");
            }
            else
            {
                result = null;
                FJDebug.LogError($"Failed to create a PatcherInfo object from the source string: {sourceString}");
            }
            return result != null;
        }

        public void Patch()
        {
            FJDebug.Log($"Patching ...)");
            // Apply the transpiler patch
            try
            {
                FJDebug.Log($"Patching {ToString()}...)");
                if (PatchType == "Transpiler")
                    FromJianghuENMod.harmony.Patch(OriginalMethodInfo, transpiler: new HarmonyMethod(typeof(PatcherInfo).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.Public)));
                //else if (PatchType == "Postfix")
                //    FromJianghuENMod.harmony.Patch(OriginalMethodInfo, transpiler: new HarmonyMethod(typeof(PatcherInfo).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.Public)));
                FJDebug.Log($"Patched successfully!");
            }
            catch (Exception e)
            {
                FJDebug.LogError($"Error patching {ToString()} {OriginalMethodInfo}: {e.Message}");
            }
        }

        #region Patch methods
        // Static transpiler method that retrieves the helper object from the static dictionary
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // Use a list to store the modified instructions
            List<CodeInstruction> codes = new(instructions);
            // Iterate over the IL codes and apply replacements using the helper's operationReplacements
            foreach (CodeInstruction code in codes)
            {
                if (code.opcode == OpCodes.Ldstr)
                {
                    if (code.operand is string operand &&
                        !string.IsNullOrEmpty(operand) &&
                        FromJianghuENMod.TryTranslatingString(operand, out string translatedText))
                    {
                        code.operand = translatedText;
                        break; // Exit the inner loop once a replacement is made
                    }
                }
            }

            return codes;
        }
        #endregion
    }
}
