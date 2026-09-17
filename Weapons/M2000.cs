using HarmonyLib;
using System;

[HarmonyPatch(typeof(Weapon), "Awake")]
public static class M2000DamagePatch
{
    static void Postfix(Weapon __instance)
    {
        var name = __instance?.behaviour?.weaponName;
        if (string.Equals(name, "M2000", StringComparison.OrdinalIgnoreCase))
        {
            __instance.damage = 150f / 25f;
        }
    }
}
