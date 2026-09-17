using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class Prophet
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetDualLauncher(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "Prophet")
        {
            __instance.damage = 150f / 25f;
            __instance.currentAmmo = 20;
            __instance.timeBetweenFire = 0.2f;
        }
    }
}