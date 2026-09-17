using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class SmithCarbine
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetCarbine(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "Smith Carbine")
        {
            __instance.damage = 75f / 25f;
            __instance.headMultiplier = 2;
        }
    }
}
