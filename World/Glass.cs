using HarmonyLib;

[HarmonyPatch(typeof(ShatterableGlass))]
public static class Glass
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void PatchGlass(ShatterableGlass __instance)
    {
        __instance.Force = 1000f;
    }
}