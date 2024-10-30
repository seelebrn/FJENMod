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
        public Dictionary<string, object> GeneralSettings { get; set; }
        public List<PatcherInfo> Patchers { get; set; } = new List<PatcherInfo>();
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
        // Serialize the settings to a file
        public void Serialize(string filePath)
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
        public static bool Deserialize(string filePath, out ModSettings deserializedSettings)
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
                if (string.IsNullOrWhiteSpace(line)) continue;

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
                        string[] parts = line.Split(':');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            object value = parts[1].Trim();
                            deserializedSettings.GeneralSettings[key] = value;
                        }
                        break;
                    case "[Patchers]":
                        if (PatcherInfo.TryCreatePatcherFromString(line, out PatcherInfo patcher))
                            deserializedSettings.Patchers.Add(patcher);
                        break;
                }
            }

            return true;
        }

        public void ApplySettings()
        {
            foreach (PatcherInfo patcher in Patchers)
            {
                patcher.Patch();
            }
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

        public override string ToString() => $"{ClassName}.{MethodName}({string.Join(",", Parameters)}){PatchType}";

        private MethodInfo OriginalMethodInfo
        {
            get
            {
                if (Parameters == null || Parameters.Count == 0)
                    return AccessTools.Method(Helpers.ResolveType(ClassName), MethodName);
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
                Debug.Log($"Created a PatcherInfo successfully! {result}");
            }
            else
            {
                result = null;
                Debug.LogError($"Failed to create a PatcherInfo object from the source string: {sourceString}");
            }
            return result != null;
        }
 
        public void Patch()
        {
            // Apply the transpiler patch
            try
            {
                Debug.Log($"Patching {ToString()}...)");
                if (PatchType == "Transpiler")
                    FromJianghuENMod.harmony.Patch(OriginalMethodInfo, transpiler: new HarmonyMethod(typeof(PatcherInfo).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.Public)));
                else if(PatchType == "Postfix")
                    FromJianghuENMod.harmony.Patch(OriginalMethodInfo, transpiler: new HarmonyMethod(typeof(PatcherInfo).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.Public)));
                Debug.Log($"Patched successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error patching {OriginalMethodInfo.Name}: {e.Message}");
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
                        FromJianghuENMod.translationDict.TryGetValue(operand, out string translatedText))
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
