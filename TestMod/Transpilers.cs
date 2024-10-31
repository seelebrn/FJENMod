using HarmonyLib;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;

namespace FromJianghuENMod
{
    [HarmonyPatch(typeof(TMP_Text))]
    [HarmonyPatch("text", MethodType.Setter)]
    static class TextMeshProUGUI_GenerateTextMesh
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);

            //Find the index of the operation that sets m_text
            int textSetterIndex = codes.FindIndex(code => code.opcode == OpCodes.Stfld && code.operand.ToString() == "System.String m_text");

            //Stuff will break here if handled incorrectly
            if (textSetterIndex > 0)
            {
                /*
                m_text stfld operation expects the following operation order:

                i-2 => Ldarg0 (tmprougui instance)
                i-1 => Ldarg1 (string value)
                i   => stfld (m_text) field

                Ldarg1 should be replaced with a method that would return an updated string (with removed wrapper indicator tag) and disable wrapping.
                So the modified stack should look like this

                i-4 => Ldarg0 (tmprougui instance)
                i-3 => Ldarg0 (tmprougui instance)
                i-2 => Ldarg1 (string value)
                i-1 => call (calling the ModifyTextValueAndSetWrapping method that would consume i-2 and i-3 operations, pushing its return value and original Ldarg0)
                i   => stfld (m_text) field

                Since each Insert() pushes everything at the index and higher towards the end of the list, the call order matters
                */
                codes.Insert(textSetterIndex, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(textSetterIndex + 1, new CodeInstruction(OpCodes.Call, typeof(TextMeshProUGUI_GenerateTextMesh).GetMethod(nameof(ModifyTextValueAndSetWrapping))));
            }

            return codes.AsEnumerable();
        }
        // This method will modify the text value before it's stored in m_text
        public static string ModifyTextValueAndSetWrapping(string originalValue, object instance)
        {
            if (instance == null || string.IsNullOrEmpty(originalValue)) return originalValue;

            string modifiedValue = originalValue;

            //Try translating if it's pure Chinese
            if (Helpers.IsChineseOnly(modifiedValue) && Translator.TryTranslatingString(originalValue, out string translatedValue))
            {
                modifiedValue = translatedValue;
            }

            //Disable wrapping if all conditions are satisfied
            if (instance is TextMeshProUGUI textMeshProUGUI && modifiedValue.Contains("!Wrapping!"))
            {
                textMeshProUGUI.textWrappingMode = TextWrappingModes.NoWrap;
                modifiedValue = modifiedValue.Replace("!Wrapping!", "");
            }

            // Modify the value as needed
            return modifiedValue;
        }
    }
}