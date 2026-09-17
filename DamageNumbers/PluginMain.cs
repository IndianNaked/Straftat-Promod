using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("com.glizzmn.damagenumbers", "PM DamageNumbers", "1.0.0")]
public sealed class DamageNumbersPlugin : BaseUnityPlugin
{
    private Harmony harmony;

    private void Awake()
    {
        DamageNumbersConfig.Bind(Config);
        harmony = new Harmony("com.glizzmn.damagenumbers");
        PatchDamageNumbers();
    }

    private void PatchDamageNumbers()
    {
        var asm = typeof(DamageNumbersPlugin).Assembly;
        var types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++)
        {
            var type = types[i];
            if (!Attribute.IsDefined(type, typeof(DamageNumbersPatchAttribute), false))
            {
                continue;
            }
            harmony.CreateClassProcessor(type).Patch();
        }
    }
}

public static class DamageNumbersConfig
{
    public static ConfigEntry<float> LifetimeSeconds;
    public static ConfigEntry<float> NumberScale;
    public static ConfigEntry<Color> BodyColor;
    public static ConfigEntry<Color> HeadshotColor;
    public static ConfigEntry<bool> EnableRollingDamage;
    public static ConfigEntry<float> RollingWindowSeconds;
    public static ConfigEntry<bool> EnableOutline;
    public static ConfigEntry<Color> OutlineColor;
    public static ConfigEntry<float> OutlineWidth;
    public static ConfigEntry<bool> EnableBackdrop;
    public static ConfigEntry<Color> BackdropColor;

    public const float LifetimeMin = 0.2f;
    public const float LifetimeMax = 5.0f;
    public const float ScaleMin = 0.5f;
    public const float ScaleMax = 4.0f;
    public const float RollingWindowMin = 0.1f;
    public const float RollingWindowMax = 2.0f;
    public const float OutlineWidthMin = 0f;
    public const float OutlineWidthMax = 0.5f;
    public const float BackdropScale = 0.5f;

    public static void Bind(ConfigFile config)
    {
        LifetimeSeconds = config.Bind(
            "Display",
            "LifetimeSeconds",
            1.0f,
            "How long damage numbers stay on screen in seconds."
        );

        NumberScale = config.Bind(
            "Display",
            "NumberScale",
            1.0f,
            "Visual scale multiplier for damage numbers."
        );

        BodyColor = config.Bind(
            "Colors",
            "BodyColor",
            new Color(1f, 1f, 1f, 1f),
            "Color for regular damage numbers."
        );

        HeadshotColor = config.Bind(
            "Colors",
            "HeadshotColor",
            new Color(1f, 0.25f, 0.25f, 1f),
            "Color for headshot damage numbers."
        );
        OutlineColor = config.Bind(
            "Style",
            "OutlineColor",
            new Color(0f, 0f, 0f, 0.9f),
            "Outline color for damage numbers."
        );
        OutlineWidth = config.Bind(
            "Style",
            "OutlineWidth",
            0.15f,
            "Outline width for damage numbers."
        );
        EnableOutline = config.Bind(
            "Style",
            "EnableOutline",
            true,
            "Enable text outline for damage numbers."
        );
        BackdropColor = config.Bind(
            "Style",
            "BackdropColor",
            new Color(0f, 0f, 0f, 0.45f),
            "Color of the circle backdrop behind damage numbers."
        );
        EnableBackdrop = config.Bind(
            "Style",
            "EnableBackdrop",
            true,
            "Enable a circle backdrop behind damage numbers."
        );

        EnableRollingDamage = config.Bind(
            "RollingDamage",
            "EnableRollingDamage",
            false,
            "When enabled, rapid damage stacks into a single floating number per target."
        );

        RollingWindowSeconds = config.Bind(
            "RollingDamage",
            "RollingWindowSeconds",
            0.4f,
            "How long (seconds) to wait for another hit before the rolling number fades."
        );
    }

    public static float GetLifetimeSeconds()
    {
        return Mathf.Clamp(LifetimeSeconds.Value, LifetimeMin, LifetimeMax);
    }

    public static float GetNumberScale()
    {
        return Mathf.Clamp(NumberScale.Value, ScaleMin, ScaleMax);
    }

    public static float GetRollingWindowSeconds()
    {
        return Mathf.Clamp(RollingWindowSeconds.Value, RollingWindowMin, RollingWindowMax);
    }

    public static float GetOutlineWidth()
    {
        return Mathf.Clamp(OutlineWidth.Value, OutlineWidthMin, OutlineWidthMax);
    }
}
