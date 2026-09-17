using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class Bukanee
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetBuk(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "Bukanee")
        {
            __instance.requireBothHands = false;
        }
    }
}
