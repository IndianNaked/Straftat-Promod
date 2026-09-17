using HarmonyLib;

[HarmonyPatch(typeof(Shotgun), "Awake")]
public static class ShotgunPatch
{
    [HarmonyPostfix]
    public static void Postfix(Shotgun __instance)
    {
        if (__instance.behaviour.weaponName == "shotgun")
        {
            __instance.damage = 12f / 25f;
            Traverse.Create(__instance).Field("bulletAmount").SetValue(16);
            __instance.headMultiplier = 1;
        }
        if (__instance.behaviour.weaponName == "sawed off")
        {

            Traverse.Create(__instance).Field("bulletAmount").SetValue(8);

        }
    }
}
