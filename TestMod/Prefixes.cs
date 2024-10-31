using HarmonyLib;
using TMPro;
using UnityEngine.UI;

namespace FromJianghuENMod
{
    [HarmonyPatch(typeof(TextMeshProUGUI), "Awake")]
    static class TMPro_TextMeshProUGUI_Awake_Patch
    {
        static void Prefix(TextMeshProUGUI __instance)
        {

            if (__instance.name == "Label")
            {
                __instance.fontSizeMin = 14;
            }
            else if (__instance.name == "Content")
            {
                __instance.enableAutoSizing = true;
                __instance.fontSizeMin = 19;
                __instance.fontSizeMax = 24;
                __instance.textWrappingMode = TextWrappingModes.Normal;
                __instance.alignment = TextAlignmentOptions.TopJustified;
                __instance.characterSpacing += 2;
                __instance.wordSpacing += 2;
                __instance.SetVerticesDirty();
                __instance.SetLayoutDirty();
            }
            else
            {
                __instance.enableAutoSizing = true;
                __instance.fontSizeMin = 16;
                __instance.fontSizeMax = 20;
            }
        }
    }
    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    static class TMPro_TextMeshProUGUI_Enable
    {
        static void Prefix(TextMeshProUGUI __instance)
        {
            Helpers.TryPrintOutInfo(__instance);
            if (ModSettings.TryGetApplicableObjectResizer(__instance, out ObjectResizerInfo resizer))
            {
                resizer.ApplyObjectResizer(__instance);
            }
        }
    }
    [HarmonyPatch(typeof(Image), "OnEnable")]
    static class Image_Awake
    {
        static void Postfix(Image __instance)
        {
            if (!__instance) return;

            Helpers.TryPrintOutInfo(__instance);

            if (ModSettings.TryGetApplicableObjectResizer(__instance, out ObjectResizerInfo resizer))
            {
                resizer.ApplyObjectResizer(__instance);
            }
        }
    }
    [HarmonyPatch(typeof(LayoutGroup), "OnEnable")]
    static class LayoutGroup_Enable
    {
        static void Postfix(LayoutGroup __instance)
        {
            if (!__instance) return;

            Helpers.TryPrintOutInfo(__instance);

            if (ModSettings.TryGetApplicableLayoutGroupChanger(__instance, out LayoutGroupChangerInfo layoutChanger))
            {
                layoutChanger.ApplyLayoutChanger(__instance);
            }
            if (ModSettings.TryGetApplicableObjectResizer(__instance, out ObjectResizerInfo resizer))
            {
                resizer.ApplyObjectResizer(__instance);
            }
        }
    }    [HarmonyPatch(typeof(Text), "OnEnable")]
    static class Text_OnEnable
    {
        static void Postfix(LayoutGroup __instance)
        {
            if (!__instance) return;

            Helpers.TryPrintOutInfo(__instance);
      
            if (ModSettings.TryGetApplicableObjectResizer(__instance, out ObjectResizerInfo resizer))
            {
                resizer.ApplyObjectResizer(__instance);
            }
        }
    }
}
