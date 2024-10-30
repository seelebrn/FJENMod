using BepInEx;
using LitJson;
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FromJianghuENMod
{
    public static class Helpers
    {
        public static readonly Regex cjkCharRegex = new(@"\p{IsCJKUnifiedIdeographs}");
        public static readonly Regex englishCharRegex = new(@"[a-zA-Z]");

        public static bool IsChinese(string s)
        {
            return cjkCharRegex.IsMatch(s);
        }
        public static bool IsChineseOnly(string s)
        {
            return cjkCharRegex.IsMatch(s) && !englishCharRegex.IsMatch(s);
        }
        public static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        public static bool DoesMatchAny(this string s, params string[] patterns)
        {
            foreach (string pattern in patterns)
            {
                if (Regex.IsMatch(s, pattern))
                {
                    return true;
                }
            }
            return false;
        }
        public static void Serialize<T>(T obj, string path)
        {
            if (obj == null)
            {
                return;
            }
            string json = JsonMapper.ToJson(obj);
            File.WriteAllText(path, json);
        }
        public static T Deserialize<T>(string path)
        {
            if (!File.Exists(path))
            {
                return default;
            }
            string json = File.ReadAllText(path);
            return JsonMapper.ToObject<T>(json);
        }
        public static bool FullyQualifyTypes(string typeName, out string qualifiedTypeName)
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
            if (qualifiedTypeName != typeName)
                Debug.Log($"Converted {typeName} to type name: {qualifiedTypeName}");
            return qualifiedTypeName != typeName;
        }
        /// <summary>
        /// This will properly get the type from the assembly , even if it's a generic type
        /// </summary>
        /// <param name="typeString"></param>
        /// <returns></returns>
        public static Type ResolveType(string typeString)
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
                if (FullyQualifyTypes(genericTypeName, out string qualifiedGenericTypeName))
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
        public static Type GetTypeFromAssembly(string typeName)
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
    }
}
