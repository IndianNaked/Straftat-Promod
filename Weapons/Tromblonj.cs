using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class Tromblonj
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetTromb(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "tromblonj")
        {
            __instance.requireBothHands = true;
        }
    }
}
