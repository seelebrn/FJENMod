using BepInEx;
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
        private List<LayoutGroupChangerInfo> LayoutChangers { get; set; } = new();
        private List<ObjectResizerInfo> ObjectResizers { get; set; } = new();
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
        public static void Reload()
        {
            FJDebug.Log("Reloading");
            Deserialize(out Instance);
        }
        public static void ApplyAllModifiersToCurrentView()
        {
            Image[] images = GameObject.FindObjectsOfType<Image>();
            VerticalLayoutGroup[] vLayoutGroups = GameObject.FindObjectsOfType<VerticalLayoutGroup>();
            HorizontalLayoutGroup[] hLayoutGroups = GameObject.FindObjectsOfType<HorizontalLayoutGroup>();
            GridLayoutGroup[] gLayoutGroups = GameObject.FindObjectsOfType<GridLayoutGroup>();
            TextMeshProUGUI[] tmpros = GameObject.FindObjectsOfType<TextMeshProUGUI>();

            //merge all of them into one collection using a one liner
            IEnumerable<Component> allInOne = images.Cast<Component>()
                                 .Concat(vLayoutGroups.Cast<Component>())
                                 .Concat(hLayoutGroups.Cast<Component>())
                                 .Concat(gLayoutGroups.Cast<Component>())
                                 .Concat(tmpros.Cast<Component>());
            FJDebug.Log($"Applying all. {allInOne.Count()}. resizers: {Instance.ObjectResizers.Count()}, layoutChangers: {Instance.LayoutChangers.Count()}");

            foreach (Component component in allInOne)
            {
                if (component)
                {
                    if (TryGetApplicableObjectResizer(component, out ObjectResizerInfo resizer))
                    {
                        resizer.ApplyObjectResizer(component);
                    }
                    if (component is LayoutGroup layout && TryGetApplicableLayoutGroupChanger(layout, out LayoutGroupChangerInfo changerInfo))
                    {
                        changerInfo.ApplyLayoutChanger(layout);
                    }
                }
                else
                {
                    FJDebug.LogError($"Component is null");
                }
            }
        }
        // Deserialize the settings from a file
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
            Instance.ObjectResizers.Clear();
            Instance.LayoutChangers.Clear();

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
                    case "[LayoutGroupChangers]":
                        if (LayoutGroupChangerInfo.TryCreateLayoutChangerFromString(line, out LayoutGroupChangerInfo layoutChanger))
                            Instance.LayoutChangers.Add(layoutChanger);
                        break;
                    case "[ObjectResizers]":
                        if (ObjectResizerInfo.TryCreateObjectResizerFromString(line, out ObjectResizerInfo resizer))
                            Instance.ObjectResizers.Add(resizer);
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
        public static bool TryGetApplicableLayoutGroupChanger(LayoutGroup group, out LayoutGroupChangerInfo changer)
        {
            changer = Instance.LayoutChangers.FirstOrDefault(r => r.CanBeApplied(group));
            return changer != null;
        }
        public static bool TryGetApplicableObjectResizer(Component obj, out ObjectResizerInfo resizer)
        {
            resizer = Instance.ObjectResizers.FirstOrDefault(r => r.CanBeApplied(obj));
            return resizer != null;
        }
    }

    // Class to store patcher information
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
    public class ObjectResizerInfo
    {
        private string FullPath { get; set; }
        private Vector2 SizeChange { get; set; }
        public bool CanBeApplied(Component obj)
        {
            //FJDebug.Log($"==={Helpers.GetFullPathToObject(obj)} vs {FullPath}===\n");
            return Helpers.GetFullPathToObject(obj) == FullPath;
        }
        public override string ToString()
        {
            return $"{FullPath} = {SizeChange.x};{SizeChange.y}";
        }
        public static bool TryCreateObjectResizerFromString(string inputString, out ObjectResizerInfo resizer)
        {
            resizer = null;

            // Match the input string with the expected format
            Match match = Regex.Match(inputString, @"(?<fullPath>[\w\/.()]+)\s\=\s(?<sizeChange>.+)");
            if (match.Success)
            {
                string fullPath = match.Groups["fullPath"].Value;
                string sizeChange = match.Groups["sizeChange"].Value;

                // Parse the size change value
                string[] sizeParts = sizeChange.Split(';');
                if (sizeParts.Length == 2 &&
                    float.TryParse(sizeParts[0], out float width) &&
                    float.TryParse(sizeParts[1], out float height))
                {
                    resizer = new ObjectResizerInfo(fullPath, new Vector2(width, height));
                }
            }

            if (resizer == null)
                FJDebug.LogError($"Failed to create an ObjectResizerInfo object from the input string: {inputString}");
            else
                FJDebug.Log($"Created an ObjectResizerInfo successfully! {resizer}");

            return resizer != null;
        }
        public ObjectResizerInfo(string fullPath, Vector2 sizeChange)
        {
            FullPath = fullPath;
            SizeChange = sizeChange;
        }
        public void ApplyObjectResizer(Component obj)
        {
            FJDebug.Log($"Applying size change to {obj.name}");
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = SizeChange;
            FJDebug.Log($"Changing size to {rectTransform.sizeDelta}");
        }
    }
    public class LayoutGroupChangerInfo
    {
        private string FullPath { get; set; }
        private Vector2? CellSizeChange { get; set; }
        private Vector2? SpacingChange { get; set; }
        public bool CanBeAppliedByName(LayoutGroup group) => Helpers.GetFullPathToObject(group) == FullPath;
        public bool CanBeAppliedByParameters(LayoutGroup group)
        {
            //Cellsize is only applicable to GridLayoutGroup. Everything else is applicable to all LayoutGroups
            if (CellSizeChange != null && group is not GridLayoutGroup gridLayoutGroup)
            {
                return false;
            }
            return true;
        }
        public bool CanBeApplied(LayoutGroup group) => CanBeAppliedByName(group) && CanBeAppliedByParameters(group);

        public override string ToString()
        {
            return $"Path: {FullPath}, CellSizeChange: {CellSizeChange}, SpacingChange: {SpacingChange}";
        }

        public void ApplyLayoutChanger(LayoutGroup group)
        {
            FJDebug.Log($"Applying layout changer to {group.name}");

            if (CellSizeChange.HasValue && group is GridLayoutGroup gridLayoutGroup)
            {
                gridLayoutGroup.cellSize = CellSizeChange.Value;
                FJDebug.Log($"Changing cellsize to {gridLayoutGroup.cellSize}");
            }

            if (SpacingChange.HasValue)
            {
                if (group is HorizontalOrVerticalLayoutGroup layoutGroup)
                {
                    layoutGroup.spacing = SpacingChange.Value.x;
                    FJDebug.Log($"Changing spacing to {layoutGroup.spacing}");
                }
                else if (group is GridLayoutGroup gridLayout)
                {
                    gridLayout.spacing = SpacingChange.Value;
                    FJDebug.Log($"Changing spacing to {gridLayout.spacing}");
                }
            }
        }

        public static bool TryCreateLayoutChangerFromString(string inputString, out LayoutGroupChangerInfo resizer)
        {
            // Match the input string with the expected format
            Match match = Regex.Match(inputString, @"(?<fullPath>[\w\/.()]+)\s(?<property>\w+)\s=\s(?<value>.+)");
            if (match.Success)
            {
                string fullPath = match.Groups["fullPath"].Value;
                string property = match.Groups["property"].Value;
                string value = match.Groups["value"].Value;

                Vector2? cellSizeChange = null;
                Vector2? spacingChange = null;

                // Parse the value based on the property name
                switch (property.ToLower())
                {
                    case "cellsize":
                        string[] cellSizeParts = value.Split(';');
                        if (cellSizeParts.Length == 2 &&
                            float.TryParse(cellSizeParts[0], out float cellWidth) &&
                            float.TryParse(cellSizeParts[1], out float cellHeight))
                        {
                            cellSizeChange = new Vector2(cellWidth, cellHeight);
                        }
                        break;
                    case "spacing":
                        string[] spacing = value.Split(';');
                        //if spacing is a single value, apply it to both x and y
                        if (spacing.Length == 1 &&
                            float.TryParse(spacing[0], out float spacingValue))
                        {
                            spacingChange = new(spacingValue, spacingValue);
                        }
                        //if spacing is two values, apply them to x and y respectively
                        else if (spacing.Length == 2 &&
                            float.TryParse(spacing[0], out float spacingX) &&
                            float.TryParse(spacing[1], out float spacingY))
                        {
                            spacingChange = new(spacingX, spacingY);
                        }
                        break;
                    default:
                        FJDebug.LogError($"Unsupported property name: {property}");
                        resizer = null;
                        return false;
                }

                // Create the LayoutGroupResizerInfo object
                resizer = new LayoutGroupChangerInfo(fullPath, cellSizeChange, spacingChange);
                FJDebug.Log($"Created a LayoutGroupResizerInfo successfully! {resizer}");
            }
            else
            {
                FJDebug.LogError($"Failed to create a LayoutGroupResizerInfo object from the input string: {inputString}");
                resizer = null;
            }
            return resizer != null;
        }

        public LayoutGroupChangerInfo(string fullPath, Vector2? cellSizeChange = null, Vector2? spacingChange = null)
        {
            FullPath = fullPath;
            CellSizeChange = cellSizeChange;
            SpacingChange = spacingChange;
        }
    }
}
