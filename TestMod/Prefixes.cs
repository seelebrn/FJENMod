using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

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
            if (__instance.name == "Content")
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
            if (__instance.name != "Label" || __instance.name != "Content")
            {
                __instance.enableAutoSizing = true;
                __instance.fontSizeMin = 16;
                __instance.fontSizeMax = 20;
            }
        }
    }

}
