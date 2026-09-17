using System;
using HarmonyLib;

namespace StraftatMods
{

    [HarmonyPatch(typeof(Weapon), "Awake___UserLogic")]
    public static class WeaponNoSpreadPatch
    {

        [HarmonyPostfix]
        public static void Awake(Weapon __instance)
        {
            __instance.minSpread = 0f;
            __instance.maxSpread = 0f;
            __instance.notAimingAccuracy = 0f;
        }
    }
}
