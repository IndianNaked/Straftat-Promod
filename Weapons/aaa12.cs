using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class aaa12
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void Setaaa12(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "AAA-12")
        {
            __instance.damage = 15f / 25f;
        }
    }
}