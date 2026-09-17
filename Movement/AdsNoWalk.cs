using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StraftatMods
{

    [HarmonyPatch(typeof(FirstPersonController))]
    public static class FirstPersonController_AdsSprint_Patch
    {
        static readonly FieldInfo F_move = AccessTools.Field(typeof(FirstPersonController), "move");
        static readonly FieldInfo F_isLeaning = AccessTools.Field(typeof(FirstPersonController), "isLeaning");
        static readonly FieldInfo F_isCrouching = AccessTools.Field(typeof(FirstPersonController), "isCrouching");
        static readonly FieldInfo F_funcSprint = AccessTools.Field(typeof(FirstPersonController), "funcSprint");
        static readonly FieldInfo F_isSprinting = AccessTools.Field(typeof(FirstPersonController), "isSprinting");
        static readonly FieldInfo F_isSlideSprinting = AccessTools.Field(typeof(FirstPersonController), "isSlideSprinting");
        static readonly FieldInfo F_isAiming = AccessTools.Field(typeof(FirstPersonController), "isAiming");

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        static void Post_Update(FirstPersonController __instance)
        {
            try
            {
                if (!__instance.sync___get_value_canMove()) return;

                bool isAiming = F_isAiming != null && (bool)F_isAiming.GetValue(__instance);
                if (!isAiming) return;

                bool isLeaning = F_isLeaning != null && (bool)F_isLeaning.GetValue(__instance);
                bool isCrouching = F_isCrouching != null && (bool)F_isCrouching.GetValue(__instance);
                bool funcSprint = F_funcSprint != null && (bool)F_funcSprint.GetValue(__instance);

                var moveAction = F_move?.GetValue(__instance) as InputAction;
                bool moving = moveAction != null && moveAction.ReadValue<Vector2>() != Vector2.zero;

                bool newIsSprinting = (!isLeaning && moving && !isCrouching && funcSprint);
                bool newIsSlideSprinting = (!isLeaning && moving && funcSprint);

                F_isSprinting?.SetValue(__instance, newIsSprinting);
                F_isSlideSprinting?.SetValue(__instance, newIsSlideSprinting);
            }
            catch (Exception e)
            {
                Debug.LogError($"ADS Sprint Fix: {e}");
            }
        }
    }
}
