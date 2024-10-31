using HarmonyLib;
using LitJson;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FromJianghuENMod
{
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
            TransformContainer mContainer = mContainerRef(__instance);

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

    [HarmonyPatch(typeof(JsonMapper), "ReadValue", new[] { typeof(Type), typeof(JsonStreamReader) })]
    static class JSONObject_Patch
    {
        static void Postfix(JsonMapper __instance, ref object __result)
        {
            JSONPostfixProcessor.ProcessJsonResult(ref __result);
        }
    }

    [HarmonyPatch(typeof(JsonMapper), "ReadValue", new[] { typeof(Type), typeof(JsonReader) })]
    static class JSONObject_Patch2
    {
        static void Postfix(JsonMapper __instance, ref object __result)
        {
            JSONPostfixProcessor.ProcessJsonResult(ref __result);
        }
    }

    [HarmonyPatch(typeof(JsonMapper), "ReadValue", new[] { typeof(WrapperFactory), typeof(JsonReader) })]
    static class JSONObject_Patch3
    {
        static void Postfix(JsonMapper __instance, ref object __result)
        {
            JSONPostfixProcessor.ProcessJsonResult(ref __result);
        }
    }

    [HarmonyPatch(typeof(JsonMapper), "ReadValue", new[] { typeof(WrapperFactory), typeof(JsonStreamReader) })]
    static class JSONObject_Patch4
    {
        static void Postfix(JsonMapper __instance, ref object __result)
        {
            JSONPostfixProcessor.ProcessJsonResult(ref __result);
        }
    }
    public class JSONPostfixProcessor
    {
        public static void ProcessJsonResult(ref object __result)
        {
            if (__result is string result && !string.IsNullOrEmpty(result))
            {
                if (FromJianghuENMod.TryTranslatingString(result, out string translated))
                {
                    try
                    {
                        FromJianghuENMod.matched.Add(result);
                        __result = translated;
                    }
                    catch (Exception e)
                    {
                        FJDebug.LogError($"Error translating JSON : {e.Message}");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(TimeModel), "GetChineseTime")]
    static class TimeModel_GetChineseTime
    {
        static void Postfix(TimeModel __instance, ref string __result)
        {
            TimeSpan timeSpan = new(Convert.ToInt64(SingletonMonoBehaviour<TimeModel>.Instance.mUserData.mGameNow * 10000000.0));
            //Debug.Log("Chinese Time : " + __instance.mUserData.Hours + " // " + __instance.mUserData.Quarters);
            //Debug.Log("Other Time : " + __instance.GameDateTime.Hours + " // " + timeSpan.Minutes);
            __result = $"{__instance.GameDateTime.Hours}h, {timeSpan.Minutes}m";
        }
    }

    [HarmonyPatch(typeof(Character), "GetFullName")]
    static class Character_GetFullName
    {
        static void Postfix(Character __instance, ref string __result)
        {
            string NewName = "";
            //Debug.Log(__instance.Name);
            string name = __instance.Name;
            string pattern = @"^([A-Z][a-z]+)([A-Z])";
            foreach (object result in Regex.Matches(name, pattern))
            {
                //Debug.Log("Match : " + result.ToString());
                NewName = Regex.Replace(name, pattern, "$1 $2");

            }
            if (NewName != "" && NewName != null)
            {
                __instance.Name = NewName;
            }
        }
    }

    [HarmonyPatch(typeof(CorpseData), "GetName")]
    static class CorpseData_GetName
    {
        static void Postfix(CorpseData __instance, ref string __result)
        {
            string NewName = "";
            Debug.Log("Name : " + __result);
            string name = __result;
            string pattern = @"([A-Z][a-z]+)([A-Z])";
            foreach (object result in Regex.Matches(name, pattern))
            {
                Debug.Log("Match : " + result.ToString());
                NewName = Regex.Replace(name, pattern, "$1 $2");

            }
            if (NewName != "" && NewName != null)
            {
                __result = NewName;
            }
        }
    }

    [HarmonyPatch(typeof(LocalizationManager), "GetChineseNumber")]
    static class LocalizationManager_Text_GetChineseNumber
    {
        static void Postfix(LocalizationManager __instance, ref string __result)
        {
            Debug.Log("Result GCN : " + __result);
            __result = __result.Replace("ten", "");
            if (__result.Length == 1)
            {
                __result = __result + "0";
            }
        }
    }
}
