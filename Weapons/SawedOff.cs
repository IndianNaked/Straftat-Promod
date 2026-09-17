using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class SawedOff
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetSawed(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "sawed off")
        {
            __instance.requireBothHands = false;
        }
    }
}
