using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class MinigunMod
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetMinigun(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "minigun")
        {
            __instance.damage = 10f / 25f;
        }
    }
}
