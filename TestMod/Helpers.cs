using BehaviorDesigner.Runtime.Tasks;
using BepInEx;
using System;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Fully qualifies the given type name.
        /// </summary>
        /// <param name="typeName">The type name to qualify.</param>
        /// <param name="qualifiedTypeName">The fully qualified type name.</param>
        /// <returns>True if the type name was qualified; otherwise, false.</returns>
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
        /// Resolves the type from the given type string, even if it's a generic type.
        /// </summary>
        /// <param name="typeString">The type string to resolve.</param>
        /// <returns>The resolved type, or null if the type could not be resolved.</returns>
        public static Type ResolveType(string typeString)
        {
            if (typeString.Contains("<") && typeString.EndsWith(">"))
            {
                int genericStartIndex = typeString.IndexOf("<");
                string genericTypeName = typeString.Substring(0, genericStartIndex);
                string innerTypesName = typeString.Substring(genericStartIndex + 1, typeString.Length - genericStartIndex - 2);
                FJDebug.Log($"Trying to resolve generic type: {genericTypeName} with inner types: {innerTypesName}. typestring: {typeString}");
                if (FullyQualifyTypes(innerTypesName, out string qualifiedTypeName))
                    innerTypesName = qualifiedTypeName;
                if (FullyQualifyTypes(genericTypeName, out string qualifiedGenericTypeName))
                    genericTypeName = qualifiedGenericTypeName;

                string[] innerTypeNames = innerTypesName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                Type[] innerTypes = new Type[innerTypeNames.Length];
                for (int i = 0; i < innerTypeNames.Length; i++)
                {
                    string innerTypeName = innerTypeNames[i].Trim();
                    innerTypes[i] = GetTypeFromAssembly(innerTypeName);
                    if (innerTypes[i] == null)
                    {
                        FJDebug.LogError($"Failed to resolve inner type: {innerTypeName}");
                        return null;
                    }
                }

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
                return genericTypeDefinition.MakeGenericType(innerTypes);
            }
            else
            {
                FJDebug.Log("Not generic, passing to GetTypeFromAssembly");
                return GetTypeFromAssembly(typeString);
            }
        }

        /// <summary>
        /// Gets the type from the assembly by the given type name.
        /// </summary>
        /// <param name="typeName">The type name to resolve.</param>
        /// <returns>The resolved type, or null if the type could not be found.</returns>
        public static Type GetTypeFromAssembly(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                FJDebug.Log($"Couldn't find type {typeName} in default assembly, trying Assembly-Csharp");

                Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(asm => asm.GetName().Name == "Assembly-CSharp");

                if (assembly == null)
                {
                    FJDebug.LogError("Failed to find Assembly-CSharp in loaded assemblies.");
                    return null;
                }

                type = assembly.GetType(typeName);
                if (type == null)
                {
                    FJDebug.LogError($"Failed to find type {typeName} in both default assembly and Assembly-Csharp");

                    Type[] types = assembly.GetTypes();
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

        /// <summary>
        /// Tries to get the component of type T in the children of the given component, searching deeply.
        /// </summary>
        /// <typeparam name="T">The type of component to find.</typeparam>
        /// <param name="monoBehavior">The component to search in.</param>
        /// <param name="component">The found component, or null if not found.</param>
        /// <param name="silent">If true, suppresses error logging.</param>
        /// <returns>The found component, or null if not found.</returns>
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

        /// <summary>
        /// Gets the component of type T in the parents of the given MonoBehaviour, searching deeply.
        /// </summary>
        /// <typeparam name="T">The type of component to find.</typeparam>
        /// <param name="monoBehavior">The MonoBehaviour to search in.</param>
        /// <param name="silent">If true, suppresses error logging.</param>
        /// <returns>The found component, or null if not found.</returns>
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

        /// <summary>
        /// Tries to get the component of type T in the children of the given MonoBehaviour.
        /// </summary>
        /// <typeparam name="T">The type of component to find.</typeparam>
        /// <param name="monoBehavior">The MonoBehaviour to search in.</param>
        /// <param name="component">The found component, or null if not found.</param>
        /// <param name="silent">If true, suppresses error logging.</param>
        /// <returns>The found component, or null if not found.</returns>
        public static T TryGetComponentInChildren<T>(this MonoBehaviour monoBehavior, out T component, bool silent = true) where T : MonoBehaviour
        {
            component = monoBehavior.GetComponentInChildren<T>();
            if (!component && !silent)
            {
                FJDebug.LogError($"Failed to find {typeof(T)} component in children of {monoBehavior.name}");
            }
            return component;
        }

        /// <summary>
        /// Tries to get the component of type T in the parent of the given MonoBehaviour.
        /// </summary>
        /// <typeparam name="T">The type of component to find.</typeparam>
        /// <param name="monoBehavior">The MonoBehaviour to search in.</param>
        /// <param name="component">The found component, or null if not found.</param>
        /// <param name="silent">If true, suppresses error logging.</param>
        /// <returns>The found component, or null if not found.</returns>
        public static T TryGetComponentInParent<T>(this MonoBehaviour monoBehavior, out T component, bool silent = true) where T : MonoBehaviour
        {
            component = monoBehavior.GetComponentInParent<T>();
            if (!component && !silent)
            {
                FJDebug.LogError($"Failed to find {typeof(T)} component in parent of {monoBehavior.name}");
            }
            return component;
        }

        /// <summary>
        /// Gets the full path to the given component in the hierarchy.
        /// </summary>
        /// <param name="component">The component to get the path for.</param>
        /// <returns>The full path to the component.</returns>
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

                    string layoutGroupName = layoutGroup switch
                    {
                        VerticalLayoutGroup => "VerticalLayoutGroup",
                        HorizontalLayoutGroup => "HorizontalLayoutGroup",
                        GridLayoutGroup => "GridLayoutGroup",
                        _ => "None"
                    };

                    string layoutGroupParameters = layoutGroup switch
                    {
                        HorizontalOrVerticalLayoutGroup group => $"spacing: {group.spacing}, size {group.GetComponent<RectTransform>().sizeDelta}, control {group.childControlWidth}",
                        GridLayoutGroup gGroup => $"spacing: {gGroup.spacing}, size {gGroup.GetComponent<RectTransform>().sizeDelta}, {gGroup.cellSize}",
                        _ => "None"
                    };

                    string layoutGroupFullPath = GetFullPathToObject(layoutGroup);
                    string componentsOnTheObject = string.Join(", ", uiObject.GetComponents<Component>().Select(c => c.GetType().Name));

                    string outputString = $"\n--- {uiObject.name} (size:{rectSize}) INFO ---\n" +
                                          $"Full Path: {GetFullPathToObject(uiObject)}\n" +
                                          $"Components: {componentsOnTheObject}\n" +
                                          $"Layout Group: {layoutGroupName}\n" +
                                          $"Layout Group Full Path: {layoutGroupFullPath}\n" +
                                          $"Layout Group Parameters: {layoutGroupParameters}\n";

                    if (text)
                    {
                        outputString += $"Text Child full path: {GetFullPathToObject(text)}\n" +
                                        $"Text: {text.text}\n";
                    }
                    outputString += "--- END OF INFO ---";

                    FJDebug.Log(outputString, "ObjectInfo");
                }
            }
        }
    }
    public static class FJDebug
    {
        /// <summary>
        /// Logs a message to the console or a file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logFileName">The name of the log file without the extension.</param>
        public static void Log(object message, string logFileName = "")
        {
            if (ModSettings.GetSettingValue<bool>("enableDebugLog"))
            {
                if (!string.IsNullOrEmpty(logFileName))
                    LogToFile(message, "Log", logFileName);
                else
                    Debug.Log($"[FromJianghuENMod Log] {message}");
            }
        }

        /// <summary>
        /// Logs an error message to the console or a file.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="logFileName">The name of the log file without the extension.</param>
        public static void LogError(object message, string logFileName = "")
        {
            if (!string.IsNullOrEmpty(logFileName))
                LogToFile(message, "Error", logFileName);
            else
                Debug.LogError($"[FromJianghuENMod Error] {message}");
        }

        /// <summary>
        /// Logs a message to a file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="messageType">The type of message (Log or Error).</param>
        /// <param name="logFileName">The name of the log file without the extension.</param>
        private static void LogToFile(object message, string messageType, string logFileName)
        {
            if (string.IsNullOrEmpty(message.ToString()) || string.IsNullOrEmpty(messageType) || string.IsNullOrEmpty(logFileName))
            {
                Debug.LogError("[FromJianghuENMod Error] Invalid parameters provided to LogToFile.");
                return;
            }

            string logFilePath = Path.Combine(Paths.PluginPath, "Logs", $"{logFileName}.txt");
            string logMessage = $"[FromJianghuENMod {messageType}] {message}\n";
            string logDirectory = Path.GetDirectoryName(logFilePath);

            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                using (StreamWriter writer = new(logFilePath, true, Encoding.UTF8))
                {
                    writer.Write(logMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FromJianghuENMod Error] Failed to log message to file: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all log files in the Logs directory.
        /// </summary>
        public static void ClearAllLogs()
        {
            string logDirectory = Path.Combine(Paths.PluginPath, "Logs");

            if (Directory.Exists(logDirectory))
            {
                try
                {
                    Directory.Delete(logDirectory, true);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FromJianghuENMod Error] Failed to clear logs: {ex.Message}");
                }
            }
        }
    }
}