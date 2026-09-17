using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class Elephant
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetElephant(Weapon __instance)
    {
        if (__instance.behaviour.weaponName == "Elephant")
        {
            __instance.damage = 150f / 25f;
        }
    }
}