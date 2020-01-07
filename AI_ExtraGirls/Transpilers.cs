using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AIProject;
using AIProject.UI;
using ConfigScene;
using HarmonyLib;
using UnityEngine;

namespace AI_ExtraGirls
{
    public static class Transpilers
    {
        private static IEnumerable<CodeInstruction> ChangeCharaCount(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_I4_4);
            if (index <= 0)
            {
                AI_ExtraGirls.Logger.LogMessage("Failed transpiling 'ChangeCharaCount' Character count index not found!");
                AI_ExtraGirls.Logger.LogWarning("Failed transpiling 'ChangeCharaCount' Character count index not found!");
                return il;
            }

            il[index].opcode = OpCodes.Ldc_I4_S;
            il[index].operand = AI_ExtraGirls.girlCount;

            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(MapUIContainer), "GetActorColor")]
        public static IEnumerable<CodeInstruction> MapUIContainer_GetActorColor_RemoveActorColorCheckError(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();

            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo)?.Name == "LogError");
            if (index <= 0)
            {
                AI_ExtraGirls.Logger.LogMessage("Failed transpiling 'MapUIContainer_GetActorColor_RemoveActorColorCheckError' LogError index not found!");
                AI_ExtraGirls.Logger.LogWarning("Failed transpiling 'MapUIContainer_GetActorColor_RemoveActorColorCheckError' LogError index not found!");
                return il;
            }

            for (int i = -6; i < 1; i++)
                il[index + i].opcode = OpCodes.Nop;
            
            return il;
        }
                
        [HarmonyTranspiler, HarmonyPatch(typeof(MiniMapControler), "GirlIconInit")]
        public static IEnumerable<CodeInstruction> MiniMapControler_GirlIconInit_ClampIconIDs(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();

            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Ldfld && (instruction.operand as FieldInfo)?.Name == "TrajectoryGirl");
            if (index <= 0)
            {
                AI_ExtraGirls.Logger.LogMessage("Failed transpiling 'MiniMapControler_GirlIconInit_ClampIconIDs' TrajectoryGirl index not found!");
                AI_ExtraGirls.Logger.LogWarning("Failed transpiling 'MiniMapControler_GirlIconInit_ClampIconIDs' TrajectoryGirl index not found!");
                return il;
            }
            
            il.InsertRange(index + 3, new []
            {
                new CodeInstruction(OpCodes.Ldc_I4_0), 
                new CodeInstruction(OpCodes.Ldc_I4_S, AI_ExtraGirls.defaultGirlCount - 1), 
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new [] {typeof(int), typeof(int), typeof(int)})), 
            });

            return il;
        }
        
        [HarmonyTranspiler, HarmonyPatch(typeof(CharaMigrateUI), "SetActiveControl")]
        public static IEnumerable<CodeInstruction> CharaMigrateUI_SetActiveControl_ChangeCharaCount(IEnumerable<CodeInstruction> instructions) => ChangeCharaCount(instructions);
        
        [HarmonyTranspiler, HarmonyPatch(typeof(CharaLookEditUI), "SetActiveControl")]
        public static IEnumerable<CodeInstruction> CharaLookEditUI_SetActiveControl_ChangeCharaCount(IEnumerable<CodeInstruction> instructions) => ChangeCharaCount(instructions);

        [HarmonyTranspiler, HarmonyPatch(typeof(CharaChangeUI), "SetActiveControl")]
        public static IEnumerable<CodeInstruction> CharaChangeUI_SetActiveControl_ChangeCharaCount(IEnumerable<CodeInstruction> instructions) => ChangeCharaCount(instructions);

        [HarmonyTranspiler, HarmonyPatch(typeof(GraphicSystem), "Read")] 
        public static IEnumerable<CodeInstruction> GraphicSystem_Read_ChangeCharaCount(IEnumerable<CodeInstruction> instructions) => ChangeCharaCount(instructions);
    }
}