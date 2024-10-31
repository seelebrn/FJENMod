using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FromJianghuENMod
{
    /// <summary>
    /// Class to store patcher information.
    /// </summary>
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

        /// <summary>
        /// Tries to create a <see cref="PatcherInfo"/> object from a string.
        /// </summary>
        /// <param name="sourceString">The source string.</param>
        /// <param name="result">The resulting <see cref="PatcherInfo"/> object.</param>
        /// <returns><c>true</c> if the creation was successful; otherwise, <c>false</c>.</returns>
        public static bool TryCreatePatcherFromString(string sourceString, out PatcherInfo result)
        {
            Match match = Regex.Match(sourceString, @"(?<class>[\w\.]+)\.(?<method>\w+)\((?<params>[^\)]*)\)\.(?<patchType>\w+)");
            if (match.Success)
            {
                string className = match.Groups["class"].Value;
                string methodName = match.Groups["method"].Value;
                string[] parameters = match.Groups["params"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string patchType = match.Groups["patchType"].Value;

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

        /// <summary>
        /// Applies the patch.
        /// </summary>
        public void Patch()
        {
            FJDebug.Log($"Patching ...)");
            try
            {
                FJDebug.Log($"Patching {ToString()}...)");
                if (PatchType == "Transpiler")
                    FromJianghuENMod.harmony.Patch(OriginalMethodInfo, transpiler: new HarmonyMethod(typeof(PatcherInfo).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.Public)));
                FJDebug.Log($"Patched successfully!");
            }
            catch (Exception e)
            {
                FJDebug.LogError($"Error patching {ToString()} {OriginalMethodInfo}: {e.Message}");
            }
        }

        /// <summary>
        /// Transpiler method to modify the IL code.
        /// </summary>
        /// <param name="instructions">The original instructions.</param>
        /// <param name="original">The original method.</param>
        /// <returns>The modified instructions.</returns>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            List<CodeInstruction> codes = new(instructions);
            foreach (CodeInstruction code in codes)
            {
                if (code.opcode == OpCodes.Ldstr)
                {
                    if (code.operand is string operand &&
                        !string.IsNullOrEmpty(operand) &&
                        Translator.TryTranslatingString(operand, out string translatedText))
                    {
                        code.operand = translatedText;
                        break;
                    }
                }
            }

            return codes;
        }
    }
}
