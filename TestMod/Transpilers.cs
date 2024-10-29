using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace FromJianghuENMod
{

    [HarmonyPatch(typeof(CharacterImageConfigUI), "UpdateCharmText")]
    static class CharacterImageConfigUI_UpdateCharmText
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // init our IL codes of current method
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
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

    [HarmonyPatch(typeof(DefenceEffect), "GetDesc", new Type[] { typeof(MartialSkillInfo), typeof(int), typeof(List<string>) })]
    static class DefenceEffect_GetFinalDesc
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // init our IL codes of current method
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count - 1; i++)
            {
                /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
                // find location of "nMods" string in parameters
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#808080>·<indent=1em>突破瓶颈{0}：{1}</indent></color>")
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

    [HarmonyPatch(typeof(AttackEffect), "GetDesc", new Type[] { typeof(MartialSkillInfo), typeof(int), typeof(List<string>) })]
    static class AttackEffect_GetFinalDesc
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // init our IL codes of current method
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count - 1; i++)
            {
                /*Debug.Log("InitialALLOperand = " + codes[i].operand);*/
                // find location of "nMods" string in parameters
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "<color=#808080>·<indent=1em>突破瓶颈{0}：{1}</indent></color>")
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
}
