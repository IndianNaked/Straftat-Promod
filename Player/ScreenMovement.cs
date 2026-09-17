using HarmonyLib;

[HarmonyPatch(typeof(Screenshake))]
public static class ScreenMovement
{
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    public static void ScreenShake(Screenshake __instance)
    {
        __instance.duration = 0f;
        __instance.bigFallDuration = 0f;
        __instance.smallFallDuration = 0f;
    }
}

public static class ScreenWobble
{
    [HarmonyPatch("Start")]
    [HarmonyPostfix]

    public static void Wobble(Wobble __instance)
    {
        __instance.MaxWobble = 0f;
        __instance.WobbleSpeed = 0f;
    }
}

public static class Effects
{
    [HarmonyPatch(typeof(CameraEffect), "Awake")]
    [HarmonyPostfix]
    public static void DisableCameraEffect(CameraEffect __instance)
    {

        CameraEffect.Intensity = 0f;

        var speedField = AccessTools.Field(typeof(CameraEffect), "speed");
        var suppSpeedField = AccessTools.Field(typeof(CameraEffect), "suppSpeed");
        var suppStartField = AccessTools.Field(typeof(CameraEffect), "suppStart");

        if (speedField != null) speedField.SetValue(__instance, 0f);
        if (suppSpeedField != null) suppSpeedField.SetValue(__instance, 0f);
        if (suppStartField != null) suppStartField.SetValue(__instance, 0f);
    }
}

