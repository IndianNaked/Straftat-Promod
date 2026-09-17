using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class BayshorePatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetBayshore(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "Bayshore")
        {
            __instance.maxWallJumps = 2;
        }
    }
}