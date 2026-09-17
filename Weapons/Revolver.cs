using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class Revolver
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetRev(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "Revolver")
        {
            __instance.requireBothHands = false;
        }
    }
}
