using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.IO;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using LitJson;
using static System.Net.Mime.MediaTypeNames;
using TMPro;
using System.Xml.Linq;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UIWidgets;

namespace FromJianghuENMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class FromJianghuENMod : BaseUnityPlugin
    {


        public const string pluginGuid = "Cadenza.IWOL.EnMod";
        public const string pluginName = "FJ ENMod Continued";
        public const string pluginVersion = "0.5";
        public static Dictionary<string, string> translationDict;
        public static List<string> untranslated = new List<string>();
        public static List<string> obsolete = new List<string>();
        public static List<string> matched = new List<string>();
        Dictionary<string, string> keystoupdate = new Dictionary<string, string>();
        public static Dictionary<string, string> FileToDictionary(string dir)
        {
            Debug.Log(BepInEx.Paths.PluginPath);

            Dictionary<string, string> dict = new Dictionary<string, string>();

            IEnumerable<string> lines = File.ReadLines(Path.Combine(BepInEx.Paths.PluginPath, "Translations", dir));

            foreach (string line in lines)
            {

                var arr = line.Split('¤');
                if (arr[0] != arr[1])
                {
                    var pair = new KeyValuePair<string, string>(Regex.Replace(arr[0], @"\t|\n|\r", ""), arr[1]);

                    if (!dict.ContainsKey(pair.Key))
                        dict.Add(pair.Key, pair.Value);
                    else
                        Debug.Log($"Found a duplicated line while parsing {dir}: {pair.Key}");
                }
            }

            return dict;

        }
        private static Harmony harmony;
        public void Awake()
        {
            UnityEngine.Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            FromJianghuENMod.translationDict = FileToDictionary("KV.txt");
            Logger.LogInfo("Hello World ! Welcome to Cadenza's plugin !");
            harmony = new Harmony("Cadenza.IWOL.EnMod");
            harmony.PatchAll();
        }


        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }
        //------------------------------------------------------------------------------------------
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.F1) == true)
            {
                Debug.Log("Cleaning a few things...");

                if (File.Exists(Path.Combine(BepInEx.Paths.PluginPath, "untranslated.txt")))
                {
                    File.Delete(Path.Combine(BepInEx.Paths.PluginPath, "untranslated.txt"));
                }
                if (File.Exists(Path.Combine(BepInEx.Paths.PluginPath, "obsolete.txt")))
                {
                    File.Delete(Path.Combine(BepInEx.Paths.PluginPath, "obsolete.txt"));
                }
                if (File.Exists(Path.Combine(BepInEx.Paths.PluginPath, "NewKV.txt")))
                {
                    File.Delete(Path.Combine(BepInEx.Paths.PluginPath, "NewKV.txt"));
                }
                Debug.Log("Exporting untranslated strings...");
                foreach (var text in untranslated.Distinct())
                {
                    if (Helpers.IsChinese(text) && text != null && text != "" && text != @"\r" && text != @"\n" && text != "\r\n")
                    {
                        using (StreamWriter tw = new StreamWriter(Path.Combine(BepInEx.Paths.PluginPath, "untranslated.txt"), append: true))
                        {
                            tw.Write(text.Replace("\r", "\\r").Replace("\n", "\\n") + Environment.NewLine);
                        }
                    }
                }
                Debug.Log("Successfully (I hope) exported untranslated strings... !");
                Debug.Log("Exporting obsolete strings...");
                foreach (KeyValuePair<string, string> kvp in translationDict)
                {
                    if (!matched.Distinct().Contains(kvp.Key) && !untranslated.Distinct().Contains(kvp.Key) && kvp.Key != "" && kvp.Key != null)
                    {
                        using (StreamWriter tw = new StreamWriter(Path.Combine(BepInEx.Paths.PluginPath, "obsolete.txt"), append: true))
                        {
                            tw.Write(kvp.Key + "¤" + kvp.Value + Environment.NewLine);
                        }
                    }
                }
                Debug.Log("Successfully (I hope) exported obsolete strings... !");
                Debug.Log("Creating your new KV...! ");
                foreach (var matchedline in matched.Distinct())
                {
                    if (translationDict.ContainsKey(matchedline) && matchedline != null && matchedline != "" && matchedline != @"\r" && matchedline != @"\n" && matchedline != "\r\n")
                    {

                        using (StreamWriter tw = new StreamWriter(Path.Combine(BepInEx.Paths.PluginPath, "NewKV.txt"), append: true))
                        {
                            tw.Write(matchedline + "¤" + translationDict[matchedline] + Environment.NewLine);
                        }
                    }
                }
                Debug.Log("Successfully (I hope) created a new KV !");
                System.Media.SystemSounds.Beep.Play();
                System.Threading.Thread.Sleep(1000);
                System.Media.SystemSounds.Asterisk.Play();
                System.Threading.Thread.Sleep(1000);
                System.Media.SystemSounds.Exclamation.Play();
                System.Media.SystemSounds.Beep.Play();
                System.Threading.Thread.Sleep(1000);
                System.Media.SystemSounds.Asterisk.Play();
                System.Threading.Thread.Sleep(1500);
                System.Media.SystemSounds.Exclamation.Play();
                System.Threading.Thread.Sleep(1500);
                System.Media.SystemSounds.Question.Play();
                System.Threading.Thread.Sleep(1500);
                System.Media.SystemSounds.Beep.Play();
                System.Threading.Thread.Sleep(1000);
                System.Media.SystemSounds.Beep.Play();
                System.Threading.Thread.Sleep(1000);

            }


            if (Input.GetKeyUp(KeyCode.F2) == true)
            {


                var alltext = UnityEngine.UI.Text.FindObjectsOfType<UnityEngine.UI.Text>();
                var alltmp = TextMeshProUGUI.FindObjectsOfType<TextMeshProUGUI>();
                foreach (var x in alltext)
                {

                    if (translationDict.ContainsValue(x.text) && !keystoupdate.ContainsValue(x.text))
                    {
                        keystoupdate.Add(translationDict.FirstOrDefault(zz => zz.Value == x.text).Key, x.text);
                        Debug.Log("KeyToUpdate filled with " + translationDict.FirstOrDefault(z => z.Value == x.text).Key + "¤" + x.text);
                    }


                }

                foreach (var y in alltmp)
                {
                    if (translationDict.ContainsValue(y.text) && !keystoupdate.ContainsValue(y.text))
                    {
                        keystoupdate.Add(translationDict.First(z => z.Value == y.text).Key, y.text);
                        Debug.Log("KeyToUpdate filled with " + translationDict.FirstOrDefault(z => z.Value == y.text).Key + "¤" + y.text);
                    }
                }
                translationDict.Clear();
                Awake();
                LocalizationManager.Instance.SetLanguageID(0);
                foreach (var x in alltext)
                {
                    var chstring = keystoupdate.FirstOrDefault(zz => zz.Value == x.text).Key;
                    if (x.text != "" && x.text != null)
                    {
                        if (keystoupdate.ContainsValue(x.text) && translationDict.ContainsKey(chstring))
                        {
                            Debug.Log("Old Value = " + x.text);
                            Debug.Log("CH key = " + chstring);
                            Debug.Log("Newvalue = " + translationDict[chstring]);

                            if (x.text != translationDict[chstring])
                            {
                                x.text = translationDict[chstring];
                                keystoupdate.Remove(chstring);
                            }
                        }
                    }
                }
                foreach (var y in alltmp)
                {
                    if (y.text != "" && y.text != null)
                    {
                        var chstring2 = keystoupdate.FirstOrDefault(zz => zz.Value == y.text).Key;
                        if (keystoupdate.ContainsValue(y.text) && translationDict.ContainsKey(chstring2))
                        {

                            Debug.Log("Old Value = " + y.text);
                            Debug.Log("CH key = " + chstring2);
                            Debug.Log("Newvalue = " + translationDict[chstring2]);

                            if (y.text != translationDict[chstring2])
                            {
                                y.text = translationDict[chstring2];
                                keystoupdate.Remove(chstring2);
                            }
                        }
                    }
                }
            }

        }
    }



[HarmonyPatch(typeof(UnityEngine.UI.InputField), "ActivateInputFieldInternal")]
static class UnityEngine_UI_InputField_Patch
{
    static AccessTools.FieldRef<UnityEngine.UI.InputField, int> m_CharacterLimitRef =
    AccessTools.FieldRefAccess<UnityEngine.UI.InputField, int>("m_CharacterLimit");
    static void Prefix(UnityEngine.UI.InputField __instance)
    {
        var m_CharacterLimit = m_CharacterLimitRef(__instance);
        m_CharacterLimitRef(__instance) = 13;

    }
}
[HarmonyPatch(typeof(TMPro.TextMeshProUGUI), "Awake")]
static class TMPro_TextMeshProUGUI_Awake_Patch
{

    static void Prefix(TMPro.TextMeshProUGUI __instance)
    {
        __instance.enableAutoSizing = true;
        // __instance.fontSizeMax = 16;
        __instance.fontSizeMax = 16;
    }
}

static class CharacterDetailUI_InitPropertyItem_Patch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CharacterDetailUI), "InitPropertyItem");
    }
    static AccessTools.FieldRef<CharacterDetailUI, TransformContainer> mContainerRef =
    AccessTools.FieldRefAccess<CharacterDetailUI, TransformContainer>("mContainer");
    static void Postfix(CharacterDetailUI __instance)
    {
        var mContainer = mContainerRef(__instance);

        mContainer["Name"].TextMeshProUGUI.autoSizeTextContainer = true;
    }
}

[HarmonyPatch(typeof(KnowledgeDetailUI), "ShowInfo")]
static class KnowledgeDetailUI_Showinfo
{

    static void Postfix(KnowledgeDetailUI __instance, KnowledgeSkillInfo knowledgeSkillInfo)
    {
        __instance.TeachLevelText.SetText("Teach (ask to teach) upper limit：{0}", (float)knowledgeSkillInfo.MaxTeachLevel);
    }
}

[HarmonyPatch(typeof(CharacterImageConfigUI), "UpdateCharmText")]
static class CharacterImageConfigUI_UpdateCharmText
{


    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // init our IL codes of current method
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count - 1; i++)
        {
            /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
            // find location of "nMods" string in parameters
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "魅力范围：{0}~{1}")
            {


                //Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "Charm Range：{0}~{1}";
                //Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            else
            {
                //                    Debug.Log("None");
            }
        }
        return codes.AsEnumerable();
    }


}

[HarmonyPatch(typeof(PanelResourceDetail), "Show")]
static class PanelResourceDetail_Show
{


    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // init our IL codes of current method
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count - 1; i++)
        {
            /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
            // find location of "nMods" string in parameters
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#ffffff>白色品质食物</color>")
            {

                Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#ffffff>White quality food</color>";
                Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#00ff00>绿色品质食物</color>")
            {


                Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#00ff00>Green quality food</color>";
                Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#00b0f0>蓝色品质食物</color>")
            {


                //                    Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#00b0f0>Blue quality food</color>";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#9933FF>紫色品质食物</color>")
            {


                //                    Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#9933FF>Purple quality food</color>";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#FF9900>橙色品质食物</color>")
            {


                //                    Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#FF9900>Orange quality food</color>";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            else
            {
                //                    Debug.Log("None");
            }
        }
        return codes.AsEnumerable();
    }


}


[HarmonyPatch(typeof(PanelSaveArchive), "OnDeleteBtn")]
static class PanelSaveArchive_OnDeleteBtn

{

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // init our IL codes of current method
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count - 1; i++)
        {
            //                /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
            // find location of "nMods" string in parameters
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "将删除存档<color=#ff0000>Save{0}</color>，是否继续？")
            {


                //                    Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#ff0000>Save{0}</color> will be deleted，continue ？";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            else
            {
                //                    Debug.Log("None");
            }
        }
        return codes.AsEnumerable();
    }


}

[HarmonyPatch(typeof(PanelArchive), "OnDeleteSlotBtn")]
static class PanelArchive_OnDeleteSlotBtn

{

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // init our IL codes of current method
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count - 1; i++)
        {
            //                /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
            // find location of "nMods" string in parameters
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "将删除存档<color=#ff0000>Save{0}</color>，是否继续？")
            {


                //                    Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#ff0000>Save{0}</color> will be deleted，continue ？";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            else
            {
                //                    Debug.Log("None");
            }
        }
        return codes.AsEnumerable();
    }


}

[HarmonyPatch(typeof(PanelSaveArchive), "OnSaveBtn")]
static class PanelSaveArchive_OnSaveBtn

{

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // init our IL codes of current method
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count - 1; i++)
        {
            //                /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
            // find location of "nMods" string in parameters
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "将覆盖存档<color=#ff0000>Save{0}</color>，是否继续？")
            {


                //                    Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#ff0000>Save{0}</color> will be overwritten，continue ？";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            else
            {
                //                    Debug.Log("None");
            }
        }
        return codes.AsEnumerable();
    }


}



[HarmonyPatch(typeof(PanelMapPopMenu), "OnBuildBtn")]
static class PanelMapPopMenue_OnBuildBtn
{


    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // init our IL codes of current method
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count - 1; i++)
        {
            /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
            // find location of "nMods" string in parameters
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "没有可建造的建筑")
            {


                //                   Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "没有可建造的建筑";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            else
            {
                // Debug.Log("None");
            }
        }
        return codes.AsEnumerable();
    }


}

[HarmonyPatch(typeof(MartialMoveInfo), "GetDesc")]
static class MartialMoveInfo_GetDesc
{


    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // init our IL codes of current method
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count - 1; i++)
        {
            /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
            // find location of "nMods" string in parameters
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#808080>{1}（突破瓶颈{0}）</indent></color>")
            {


                //                   Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#808080>{1}（Breakthrough Bottleneck {0}）</indent></color>";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#808080>{1}（突破瓶颈{0}）</indent></color>")
            {


                //                   Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#808080>·<indent=1em>{1}（Breakthrough Bottleneck {0}）</indent></color>";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }
            else
            {
                //Debug.Log("None");
            }
        }
        return codes.AsEnumerable();
    }


}

[HarmonyPatch(typeof(DefenceEffect), "GetDesc", new Type[] { typeof(MartialSkillInfo), typeof(int) })]
static class DefenceEffect_GetFinalDesc
{


    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // init our IL codes of current method
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count - 1; i++)
        {
            /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
            // find location of "nMods" string in parameters
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#808080>{1}（突破瓶颈{0}）</color>")
            {


                //                   Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#808080>{1}（Breakthrough Bottleneck {0}）</color>";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }

            else
            {
                //Debug.Log("None");
            }
        }
        return codes.AsEnumerable();
    }


}

[HarmonyPatch(typeof(AttackEffect), "GetDesc", new Type[] {typeof(MartialSkillInfo), typeof(int)})]
static class AttackEffect_GetFinalDesc
{


    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // init our IL codes of current method
        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count - 1; i++)
        {
            /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
            // find location of "nMods" string in parameters
            if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#808080>{1}（突破瓶颈{0}）</color>")
            {


                //                   Debug.Log("ConditionalOperand = " + codes[i].operand + "  i = " + i);
                codes[i].operand = "<color=#808080>{1}（Breakthrough Bottleneck {0}）</color>";
                //                    Debug.Log("ChangedOperand = " + codes[i].operand + "  i = " + i);


                //Debug.Log("Edit Done !" + dict[codes[i].operand.ToString()]);

            }

            else
            {
                //Debug.Log("None");
            }
        }
        return codes.AsEnumerable();
    }


}
/*
[HarmonyPatch]
static class JSONObject_Patch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(LitJson.JsonMapper), "ToObject", new[] { typeof(string), typeof(bool) }).MakeGenericMethod(new[] { typeof(string) });
    }
    static void Postfix(ref string __result)
    {
        try
        { 
        Debug.Log("zozozo = " + __result);
        }
        catch
        {
            Debug.Log("huh ?");
        }
                    if(FromJianghuENMod.translationDict.ContainsKey(result))
                    {
                        Debug.Log("Trying to Translate from Json : " + result);
                        __result = FromJianghuENMod.translationDict[result];
                        Debug.Log("Found Matching String : " + FromJianghuENMod.translationDict[result]);

    }

    }
*/

[HarmonyPatch(typeof(LitJson.JsonMapper), "ReadValue", new[] { typeof(Type), typeof(LitJson.JsonStreamReader) })]
static class JSONObject_Patch
{
    static void Postfix(LitJson.JsonMapper __instance, ref object __result)
    {
        try
        {
            var result = __result.ToString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"");

            if (Helpers.IsChinese(result) && FromJianghuENMod.translationDict.ContainsKey(result) && __result != null && result != "")
            {
                try
                {

                    FromJianghuENMod.matched.Add(result);
                    //Debug.Log("Trying to Translate from Json : " + result);
                    __result = FromJianghuENMod.translationDict[result];
                    //Debug.Log("Replaced String : " + __result);
                }

                catch
                {
                }
            }
            else
            {
                if (Helpers.IsChinese(result) && result != null && result != "" && !FromJianghuENMod.translationDict.ContainsKey(result))
                {

                    FromJianghuENMod.untranslated.Add(result);
                }

            }
        }

        catch
        {
            //Debug.Log("huh ?");
        }
    }

}

[HarmonyPatch(typeof(LitJson.JsonMapper), "ReadValue", new[] { typeof(Type), typeof(LitJson.JsonReader) })]
static class JSONObject_Patch2
{
    static void Postfix(LitJson.JsonMapper __instance, ref object __result)
    {
        try
        {
            var result = __result.ToString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"");

            if (Helpers.IsChinese(result) && FromJianghuENMod.translationDict.ContainsKey(result) && __result != null && __result != "")
            {
                try
                {
                    FromJianghuENMod.matched.Add(result);
                    //Debug.Log("Trying to Translate from Json : " + result);
                    __result = FromJianghuENMod.translationDict[result];
                    //Debug.Log("Replaced String : " + __result);
                }

                catch
                {
                }
            }
            else
            {
                if (Helpers.IsChinese(result) && __result != null && __result != "" && !FromJianghuENMod.translationDict.ContainsKey(result))
                {

                    FromJianghuENMod.untranslated.Add(result);
                }

            }
        }

        catch
        {
            //Debug.Log("huh ?");
        }
    }

}

[HarmonyPatch(typeof(LitJson.JsonMapper), "ReadValue", new[] { typeof(WrapperFactory), typeof(LitJson.JsonReader) })]
static class JSONObject_Patch3
{
    static void Postfix(LitJson.JsonMapper __instance, ref object __result)
    {
        try
        {
            var result = __result.ToString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"");

            if (Helpers.IsChinese(result) && FromJianghuENMod.translationDict.ContainsKey(result) && __result != null && __result != "")
            {
                try
                {
                    FromJianghuENMod.matched.Add(result);
                    //Debug.Log("Trying to Translate from Json : " + result);
                    __result = FromJianghuENMod.translationDict[result];
                    //Debug.Log("Replaced String : " + __result);
                }

                catch
                {
                }
            }
            else
            {
                if (Helpers.IsChinese(result) && __result != null && __result != "" && !FromJianghuENMod.translationDict.ContainsKey(result))
                {
                    FromJianghuENMod.untranslated.Add(result);
                }

            }
        }
        catch
        {

        }
    }
}
[HarmonyPatch(typeof(LitJson.JsonMapper), "ReadValue", new[] { typeof(WrapperFactory), typeof(LitJson.JsonStreamReader) })]
static class JSONObject_Patch4
{
    static void Postfix(LitJson.JsonMapper __instance, ref object __result)
    {
        try
        {
            var result = __result.ToString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"");

            if (Helpers.IsChinese(result) && FromJianghuENMod.translationDict.ContainsKey(result) && __result != null && __result != "")
            {
                try
                {
                    FromJianghuENMod.matched.Add(result);
                    //Debug.Log("Trying to Translate from Json : " + result);
                    __result = FromJianghuENMod.translationDict[result];
                    //Debug.Log("Replaced String : " + __result);
                }

                catch
                {
                }
            }
            else
            {
                if (Helpers.IsChinese(result) && __result != null && __result != "" && !FromJianghuENMod.translationDict.ContainsKey(result))
                {
                    FromJianghuENMod.untranslated.Add(result);
                }

            }
        }

        catch
        {
            //Debug.Log("huh ?");
        }


    }

}

[HarmonyPatch(typeof(TimeModel), "GetChineseTime")]
static class TimeModel_GetChineseTime
    {
        static void Postfix(TimeModel __instance, ref string __result) 
        {
            TimeSpan timeSpan = new TimeSpan(Convert.ToInt64(SingletonMonoBehaviour<TimeModel>.Instance.mUserData.mGameNow * 10000000.0));
            //Debug.Log("Chinese Time : " + __instance.mUserData.Hours + " // " + __instance.mUserData.Quarters);
            //Debug.Log("Other Time : " + __instance.GameDateTime.Hours + " // " + timeSpan.Minutes);
            __result = __instance.GameDateTime.Hours + " h, " + timeSpan.Minutes + " min";
        }
    }
public static class Helpers
{
    public static readonly Regex cjkCharRegex = new Regex(@"\p{IsCJKUnifiedIdeographs}");
    public static bool IsChinese(string s)
    {
        return cjkCharRegex.IsMatch(s);
    }
}

}