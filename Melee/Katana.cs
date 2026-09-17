using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class WeaponPatch
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetKatana(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "Katana")
        {
            __instance.maxWallJumps = 2;
        }
    }
}