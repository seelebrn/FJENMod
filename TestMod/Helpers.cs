using BehaviorDesigner.Runtime.Tasks;
using BepInEx;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
                FJDebug.Log($"Converted {typeName} to type name: {qualifiedTypeName}");
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
                FJDebug.Log($"Trying to resolve generic type: {genericTypeName} with inner types: {innerTypesName}. typestring: {typeString}");
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
                        FJDebug.LogError($"Failed to resolve inner type: {innerTypeName}");
                        return null;
                    }
                }

                // Resolve the generic type definition (e.g., "List<>")
                Type genericTypeDefinition = GetTypeFromAssembly(genericTypeName);
                if (genericTypeDefinition == null)
                {
                    FJDebug.LogError($"Failed to resolve generic type: {genericTypeName}");
                    return null;
                }
                else
                {
                    FJDebug.Log($"Successfully resolved generic type: {genericTypeName}");
                }
                // Make the generic type with the resolved inner types
                return genericTypeDefinition.MakeGenericType(innerTypes);
            }
            else
            {
                FJDebug.Log("Not generic, passing to GetTypeFromAssembly");
                // If it's not a generic type, just resolve it normally
                return GetTypeFromAssembly(typeString);
            }
        }
        public static Type GetTypeFromAssembly(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                FJDebug.Log($"Couldn't find type {typeName} in default assembly, trying Assembly-Csharp");

                Assembly assembly = null;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "Assembly-CSharp")
                    {
                        assembly = asm;
                        break;
                    }
                }

                if (assembly == null)
                {
                    FJDebug.LogError("Failed to find Assembly-CSharp in loaded assemblies.");
                    return null;
                }

                // Get the type from the assembly
                type = assembly.GetType(typeName);
                if (type == null)
                {
                    FJDebug.LogError($"Failed to find type {typeName} in both default assembly and Assembly-Csharp");

                    // Get all types from the assembly
                    Type[] types = assembly.GetTypes();

                    // Print each type's full name
                    foreach (Type t in types)
                    {
                        FJDebug.Log($"TYPES: |{t.FullName}| |{typeName}| {t.FullName == typeName}");
                        if (t.FullName == typeName)
                        {
                            FJDebug.Log("Names match, try using this one");
                            type = t;
                            break;
                        }
                    }
                }
            }
            if (type != null)
                FJDebug.Log($"Found type {typeName}! {type}");
            else
                FJDebug.LogError($"Failed to find type {typeName} after all attempts");
            return type;
        }
        public static T TryGetComponentInChildrenDeep<T>(this Component monoBehavior, out T component, bool silent = true) where T : MonoBehaviour
        {
            component = monoBehavior.GetComponentInChildren<T>();

            if (!component)
            {
                foreach (Transform child in monoBehavior.transform)
                {
                    if (child.TryGetComponentInChildrenDeep(out component, true))
                    {
                        break;
                    }
                }
            }

            if (!component && !silent)
            {
                FJDebug.LogError($"Failed to find {typeof(T)} component in children of {monoBehavior.name}");
            }
            return component;
        }
        public static T GetComponentInParentsDeep<T>(this MonoBehaviour monoBehavior, bool silent = true) where T : MonoBehaviour
        {
            if (monoBehavior.TryGetComponentInParent(out T result))
            {
                return result;
            }
            else
            {
                Transform parent = monoBehavior.transform.parent;
                while (parent)
                {
                    if (parent.TryGetComponent(out result))
                    {
                        return result;
                    }
                    parent = parent.parent;
                }
            }

            if (!result && !silent)
            {
                FJDebug.LogError($"Failed to find {typeof(T)} component in parents of {monoBehavior.name}");
            }
            return default;
        }
        public static T TryGetComponentInChildren<T>(this MonoBehaviour monoBehavior, out T component, bool silent = true) where T : MonoBehaviour
        {
            component = monoBehavior.GetComponentInChildren<T>();
            if (!component && !silent)
            {
                FJDebug.LogError($"Failed to find {typeof(T)} component in children of {monoBehavior.name}");
            }
            return component;
        }
        public static T TryGetComponentInParent<T>(this MonoBehaviour monoBehavior, out T component, bool silent = true) where T : MonoBehaviour
        {
            component = monoBehavior.GetComponentInParent<T>();
            if (!component && !silent)
            {
                FJDebug.LogError($"Failed to find {typeof(T)} component in parent of {monoBehavior.name}");
            }
            return component;
        }
        public static string GetFullPathToObject(Component component)
        {
            if (component == null) return string.Empty;

            StringBuilder objectFullPath = new StringBuilder(component.name);
            Transform currentParent = component.transform.parent;

            while (currentParent != null)
            {
                objectFullPath.Insert(0, $"{currentParent.name}/");
                currentParent = currentParent.parent;
            }

            return objectFullPath.ToString();
        }
        public static void TryPrintOutInfo(MonoBehaviour uiObject)
        {
            if (!uiObject) return;

            if (ModSettings.GetSettingValue<bool>("printUiElementInfo"))
            {
                TextMeshProUGUI text = null;
                if (!ModSettings.GetSettingValue<bool>("printUiElementInfoWithTextOnly") ||
                     uiObject.TryGetComponentInChildrenDeep(out text))
                {
                    RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
                    Vector2 rectSize = rectTransform.sizeDelta;

                    LayoutGroup layoutGroup = uiObject.GetComponentInParentsDeep<LayoutGroup>();

                    string layoutGroupName = layoutGroup is VerticalLayoutGroup ? "VerticalLayoutGroup" : layoutGroup is HorizontalLayoutGroup ? "HorizontalLayoutGroup" : layoutGroup is GridLayoutGroup ? "GridLayoutGroup" : "None";
                    string layoutGroupParameters;
                    string layoutGroupFullPath = GetFullPathToObject(layoutGroup);
                    if (layoutGroup is HorizontalOrVerticalLayoutGroup group)
                    {
                        layoutGroupParameters = $"spacing: {group.spacing}, size {group.GetComponent<RectTransform>().sizeDelta}, control {group.childControlWidth}";
                    }
                    else if (layoutGroup is GridLayoutGroup gGroup)
                    {
                        layoutGroupParameters = $"spacing: {gGroup.spacing}, size {gGroup.GetComponent<RectTransform>().sizeDelta}, {gGroup.cellSize}";
                    }
                    else
                    {
                        layoutGroupParameters = "None";
                    }
                    string outputString = $"--- {uiObject.name} INFO ---\n" +
                                          $"Full Path: {GetFullPathToObject(uiObject)}\n" +
                                          $"Size: {rectSize}\n" +
                                          $"Layout Group: {layoutGroupName}\n" +
                                          $"Layout Group Full Path: {layoutGroupFullPath}\n" +
                                          $"Layout Group Parameters: {layoutGroupParameters}\n";

                    if (text)
                    {
                        outputString += $"Text Child name: {text.name}\n" +
                                        $"Text Child full path: {GetFullPathToObject(text)}\n" +
                                        $"Text: {text.text}\n";
                    }
                    outputString += "--- END OF INFO ---";

                    Debug.Log(outputString);
                }
            }
        }
    }
    public static class FJDebug
    {
        public static void Log(object message)
        {
            if (ModSettings.GetSettingValue<bool>("enableDebugLog"))
                Debug.Log($"[FromJianghuENMod Log] {message}");
        }
        public static void LogError(object message)
        {
            Debug.LogError($"[FromJianghuENMod Error] {message}");
        }
    }
}