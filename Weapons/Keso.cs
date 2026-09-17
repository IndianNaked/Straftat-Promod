using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class Keso
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetKeso(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "Keso")
        {
            __instance.requireBothHands = false;
        }
    }
}
