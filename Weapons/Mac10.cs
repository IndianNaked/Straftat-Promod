using HarmonyLib;
using System;

[HarmonyPatch(typeof(Weapon), "Awake")]
public static class Mac10DamagePatch
{
    [HarmonyPostfix]
    public static void Postfix(Weapon __instance)
    {
        var name = __instance?.behaviour?.weaponName;
        if (name == null)
        {
            return;
        }
        if (string.Equals(name, "mac10", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "mac 10", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "mac-10", StringComparison.OrdinalIgnoreCase))
        {
            __instance.damage = 8f / 25f;
        }
    }
}
