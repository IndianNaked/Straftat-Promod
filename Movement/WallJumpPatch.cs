using System;
using HarmonyLib;

namespace StraftatMods
{

    [HarmonyPatch(typeof(FirstPersonController), "Awake___UserLogic")]
    public static class WallJump
    {

        [HarmonyPostfix]
        public static void Awake(FirstPersonController __instance)
        {
            __instance.maxWallJumps = 2;
        }
    }
}
