using HarmonyLib;
using LitJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FromJianghuENMod
{
    [Serializable]
    public class Patchers
    {
        public List<TranspilerPatcher> patchers;
        public Patchers()
        {
            patchers = new();
        }
        public void PatchAll()
        {
            Debug.Log("PatchAll called!");
            patchers.ForEach(p => p.Patch());
        }
    }
    [Serializable]
    public class Patcher
    {
        public string className;
        public string methodName;
        public string[] argumentTypes;
        public Patcher(string className, string methodName, string[] attributeTypes)
        {
            this.className = className;
            this.methodName = methodName;
            this.argumentTypes = attributeTypes;

        }
        public Patcher()
        {
            this.className = "";
            this.methodName = "";
            this.argumentTypes = new string[0];
        }
        public virtual void Patch() { }
        protected Type GetTypeFromAssembly(string typeName) => Type.GetType($"{typeName}, Assembly-CSharp");

    }

    [Serializable]
    public class TranspilerPatcher : Patcher
    {
        // Static dictionary to store instance-specific data for each method being patched
        private static Dictionary<MethodBase, TranspilerHelper> helperMap = new();

        public List<TranspileOperationReplacer> operationReplacements;
        
        private MethodInfo OriginalMethodInfo
        {
            get
            {
                if (argumentTypes == null || argumentTypes.Length == 0)
                    return AccessTools.Method(GetTypeFromAssembly(className), methodName);
                else
                    return AccessTools.Method(Type.GetType(className), methodName, argumentTypes.Select(GetTypeFromAssembly).ToArray());
            }
        }

        public TranspilerPatcher(string className, string methodName, string[] attributeTypes) : base(className, methodName, attributeTypes)
        {
            operationReplacements = new()
            {
                new("ldstr", "Label", "Content"),
                new("ldstr", "Label", "Content"),
                new("ldstr", "Label", "Content")
            };
            Debug.Log($"TranspilerPatcher created! {operationReplacements[0].ToString()}");
        }
        public TranspilerPatcher()
        {
            this.className = "";
            this.methodName = "";
            this.argumentTypes = new string[0];
            this.operationReplacements = new();
        }
        public override void Patch()
        {
            // Create a helper object to store instance-specific data
            TranspilerHelper helper = new(operationReplacements);

            // Store the helper object in the static dictionary, keyed by the original method
            helperMap[OriginalMethodInfo] = helper;

            // Apply the transpiler patch
            FromJianghuENMod.harmony.Patch(OriginalMethodInfo, transpiler: new HarmonyMethod(typeof(TranspilerPatcher).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.Public)));
        }

        // Static transpiler method that retrieves the helper object from the static dictionary
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // Retrieve the helper object from the static dictionary using the original method as the key
            if (!helperMap.TryGetValue(original, out TranspilerHelper helper))
            {
                Debug.LogError($"No helper found for method {original.Name}");
                return instructions; // Return original instructions if no helper is found
            }

            // Use a list to store the modified instructions
            List<CodeInstruction> codes = new(instructions);

            // Iterate over the IL codes and apply replacements using the helper's operationReplacements
            foreach (CodeInstruction code in codes)
            {
                if (code.opcode == OpCodes.Ldstr)
                {
                    foreach (TranspileOperationReplacer replacement in helper.operationReplacements)
                    {
                        if (code.operand is string operand && operand == replacement.originalStringValue)
                        {
                            code.operand = replacement.newStringValue;
                            break; // Exit the inner loop once a replacement is made
                        }
                    }
                }
            }

            return codes;
        }
    }

    // Helper class to store instance-specific data
    public class TranspilerHelper
    {
        public List<TranspileOperationReplacer> operationReplacements;

        public TranspilerHelper(List<TranspileOperationReplacer> operationReplacements)
        {
            this.operationReplacements = operationReplacements;
        }
    }
    [Serializable]
    public class TranspileOperationReplacer
    {
        public string opCode;
        public string originalStringValue;
        public string newStringValue;

        public TranspileOperationReplacer(string opCode, string originalStringValue, string newStringValue)
        {
            this.opCode = opCode;
            this.originalStringValue = originalStringValue;
            this.newStringValue = newStringValue;
        }
        public TranspileOperationReplacer()
        {
            this.opCode = "";
            this.originalStringValue = "";
            this.newStringValue = "";
        }
        public override string ToString()
        {
            return $"opCode: {opCode}, originalStringValue: {originalStringValue}, newStringValue: {newStringValue}";
        }
    }
}
