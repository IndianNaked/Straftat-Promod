using System;
using HarmonyLib;

namespace StraftatMods
{

    [HarmonyPatch(typeof(PlayerHealth), "Awake___UserLogic")]
    public static class PlayerHealthPatch
    {

        [HarmonyPostfix]
        public static void Awake(PlayerHealth __instance)
        {
            
            __instance.health = 8f;
        }
    }
}
