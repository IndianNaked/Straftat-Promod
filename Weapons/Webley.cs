using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class Webley
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetWebley(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "Webley")
        {
            __instance.damage = 2f;
            __instance.headMultiplier = 2;
            __instance.requireBothHands = false;
        }
    }
}
