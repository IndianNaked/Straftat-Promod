using System;
using HarmonyLib;

namespace StraftatMods
{

    [HarmonyPatch(typeof(PlayerPickup), "Update")]
    public static class PlayerPickupPatch
    {

        [HarmonyPostfix]
        public static void Awake(PlayerPickup __instance)
        {
            FirstPersonController value = Traverse.Create(__instance).Field("playerController").GetValue<FirstPersonController>();
            value.maxWallJumps = 2;
        }
    }
}
