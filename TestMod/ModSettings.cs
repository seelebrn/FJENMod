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
        protected Type GetTypeFromAssembly(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                Debug.Log($"Couldn't find type {typeName} in default assembly, trying Assembly-Csharp");

                Assembly assembly = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        assembly = asm;
                        break;
                    }
                }

                if (assembly == null)
                {
                    Debug.LogError("Failed to find Assembly-CSharp in loaded assemblies.");
                    return null;
                }

                // Get the type from the assembly
                type = assembly.GetType(typeName);
                if (type == null)
                {
                    Debug.LogError($"Failed to find type {typeName} in both default assembly and Assembly-Csharp");

                    // Get all types from the assembly
                    Type[] types = assembly.GetTypes();

                    // Print each type's full name
                    foreach (Type t in types)
                    {
                        Debug.Log($"TYPES: |{t.FullName}| |{typeName}| {t.FullName == typeName}");
                        if (t.FullName == typeName)
                        {
                            Debug.Log("Names match, try using this one");
                            return t;
                        }
                    }
                }
            }
            return type;
        }

        private MethodInfo OriginalMethodInfo
        {
            get
            {
                if (Parameters == null || Parameters.Count == 0)
                    return AccessTools.Method(ResolveType(ClassName), MethodName);
                else
                    return AccessTools.Method(ResolveType(ClassName), MethodName, Parameters.Select(ResolveType).ToArray());
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
                    if (FullyQualifyTypes(parameters[i], out string qualifiedTypeName))
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
        private static bool FullyQualifyTypes(string typeName, out string qualifiedTypeName)
        {
            qualifiedTypeName = typeName.ToLower() switch
            {
                "int" => "System.Int32",
                "float" => "System.Single",
                "bool" => "System.Boolean",
                "string" => "System.String",
                "list" => "System.Collections.Generic.List`1",
                _ => typeName,
            };
            if(qualifiedTypeName != typeName)
                Debug.Log($"Converted {typeName} to type name: {qualifiedTypeName}");
            return qualifiedTypeName != typeName;
        }
        /// <summary>
        /// This will properly get the type from the assembly , even if it's a generic type
        /// </summary>
        /// <param name="typeString"></param>
        /// <returns></returns>
        protected Type ResolveType(string typeString)
        {
            // Check if the type is a generic type (e.g., "List<int>")
            if (typeString.Contains("<") && typeString.EndsWith(">"))
            {
                // Extract the generic type name and the inner type(s)
                int genericStartIndex = typeString.IndexOf("<");
                string genericTypeName = typeString.Substring(0, genericStartIndex); // e.g., "List"
                string innerTypesName = typeString.Substring(genericStartIndex + 1, typeString.Length - genericStartIndex - 2); // e.g., "int"

                //incase any type names need qualifying, do it first
                if (FullyQualifyTypes(innerTypesName, out string qualifiedTypeName))
                    innerTypesName = qualifiedTypeName;
                if(FullyQualifyTypes(genericTypeName, out string qualifiedGenericTypeName))
                    genericTypeName = qualifiedGenericTypeName;

                // Split inner types in case of multiple generic arguments (e.g., "Dictionary<int, string>")
                string[] innerTypeNames = innerTypesName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // Recursively resolve each inner type
                Type[] innerTypes = new Type[innerTypeNames.Length];
                for (int i = 0; i < innerTypeNames.Length; i++)
                {
                    string innerTypeName = innerTypeNames[i].Trim(); // Remove any extra spaces
                    innerTypes[i] = GetTypeFromAssembly(innerTypeName); // Resolve the inner type using GetTypeFromAssembly
                    if (innerTypes[i] == null)
                    {
                        Debug.LogError($"Failed to resolve inner type: {innerTypeName}");
                        return null;
                    }
                }

                // Resolve the generic type definition (e.g., "List<>")
                Type genericTypeDefinition = GetTypeFromAssembly(genericTypeName);
                if (genericTypeDefinition == null)
                {
                    Debug.LogError($"Failed to resolve generic type: {genericTypeName}");
                    return null;
                }

                // Make the generic type with the resolved inner types
                return genericTypeDefinition.MakeGenericType(innerTypes);
            }
            else
            {
                // If it's not a generic type, just resolve it normally
                return GetTypeFromAssembly(typeString);
            }
        }
        public void Patch()
        {
            // Apply the transpiler patch
            try
            {
                Debug.Log($"Patching {ToString()}...)");
                if (PatchType == "Transpiler")
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
