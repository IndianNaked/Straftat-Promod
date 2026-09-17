using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("com.glizzmn.hitmarkers", "PM Hitmarkers", "1.0.0")]
public sealed class HitmarkersPlugin : BaseUnityPlugin
{
    private Harmony harmony;

    private void Awake()
    {
        HitmarkerConfig.Bind(Config);
        harmony = new Harmony("com.glizzmn.hitmarkers");
        PatchHitmarkers();
    }

    private void PatchHitmarkers()
    {
        var asm = typeof(HitmarkersPlugin).Assembly;
        var types = asm.GetTypes();
        for (int i = 0; i < types.Length; i++)
        {
            var type = types[i];
            if (!Attribute.IsDefined(type, typeof(HitmarkersPatchAttribute), false))
            {
                continue;
            }
            harmony.CreateClassProcessor(type).Patch();
        }
    }
}

public enum HitmarkerShape
{
    Cross = 0,
    Plus = 1
}

public static class HitmarkerConfig
{
    public static ConfigEntry<bool> EnableCustomHitmarkers;
    public static ConfigEntry<bool> HideVanillaHitmarkers;
    public static ConfigEntry<HitmarkerShape> Shape;
    public static ConfigEntry<int> Length;
    public static ConfigEntry<int> Thickness;
    public static ConfigEntry<int> Gap;
    public static ConfigEntry<bool> EnableOutline;
    public static ConfigEntry<int> OutlineThickness;
    public static ConfigEntry<bool> EnableBarrelHitmarker;
    public static ConfigEntry<Color> Color;
    public static ConfigEntry<Color> HeadshotColor;
    public static ConfigEntry<Color> OutlineColor;
    public static ConfigEntry<Color> HeadshotOutlineColor;
    public static ConfigEntry<Color> KillColor;
    public static ConfigEntry<Color> KillOutlineColor;
    public static ConfigEntry<Color> BarrelColor;
    public static ConfigEntry<Color> BarrelOutlineColor;

    public const int LengthMin = 4;
    public const int LengthMax = 40;
    public const int ThicknessMin = 2;
    public const int ThicknessMax = 12;
    public const int GapMin = 0;
    public const int GapMax = 20;
    public const int OutlineThicknessMin = 0;
    public const int OutlineThicknessMax = 6;

    public static void Bind(ConfigFile config)
    {
        EnableCustomHitmarkers = config.Bind(
            "General",
            "EnableCustomHitmarkers",
            true,
            "Enable custom hitmarkers."
        );

        HideVanillaHitmarkers = config.Bind(
            "General",
            "HideVanillaHitmarkers",
            true,
            "Hide the game's default hitmarkers when custom hitmarkers are enabled."
        );

        Shape = config.Bind(
            "Style",
            "Shape",
            HitmarkerShape.Cross,
            "Hitmarker shape."
        );

        Length = config.Bind(
            "Style",
            "Length",
            16,
            "Line length (even numbers only)."
        );

        Thickness = config.Bind(
            "Style",
            "Thickness",
            4,
            "Line thickness (even numbers only)."
        );

        Gap = config.Bind(
            "Style",
            "Gap",
            4,
            "Distance from center (even numbers only)."
        );

        EnableOutline = config.Bind(
            "Style",
            "EnableOutline",
            true,
            "Enable outline behind the hitmarker."
        );

        OutlineThickness = config.Bind(
            "Style",
            "OutlineThickness",
            2,
            "Outline thickness (even numbers only)."
        );

        EnableBarrelHitmarker = config.Bind(
            "Style",
            "EnableBarrelHitmarker",
            true,
            "Enable barrel and physics prop hitmarkers."
        );

        Color = config.Bind(
            "Colors",
            "Color",
            new Color(1f, 1f, 1f, 1f),
            "Hitmarker color."
        );

        HeadshotColor = config.Bind(
            "Colors",
            "HeadshotColor",
            new Color(1f, 0.25f, 0.25f, 1f),
            "Headshot hitmarker color."
        );

        OutlineColor = config.Bind(
            "Colors",
            "OutlineColor",
            new Color(0f, 0f, 0f, 0.9f),
            "Outline color."
        );

        HeadshotOutlineColor = config.Bind(
            "Colors",
            "HeadshotOutlineColor",
            new Color(0.1f, 0f, 0f, 0.9f),
            "Headshot outline color."
        );

        KillColor = config.Bind(
            "Colors",
            "KillColor",
            new Color(0.2f, 1f, 0.2f, 1f),
            "Kill hitmarker color."
        );

        KillOutlineColor = config.Bind(
            "Colors",
            "KillOutlineColor",
            new Color(0f, 0.2f, 0f, 0.9f),
            "Kill hitmarker outline color."
        );

        BarrelColor = config.Bind(
            "Colors",
            "BarrelColor",
            new Color(0.75f, 0.78f, 0.82f, 1f),
            "Physics prop/barrel hitmarker color."
        );

        BarrelOutlineColor = config.Bind(
            "Colors",
            "BarrelOutlineColor",
            new Color(0.05f, 0.06f, 0.07f, 0.95f),
            "Physics prop/barrel hitmarker outline color."
        );

        HookEven(Length, LengthMin, LengthMax);
        HookEven(Thickness, ThicknessMin, ThicknessMax);
        HookEven(Gap, GapMin, GapMax);
        HookEven(OutlineThickness, OutlineThicknessMin, OutlineThicknessMax);
    }

    private static void HookEven(ConfigEntry<int> entry, int min, int max)
    {
        entry.SettingChanged += (_, __) => EnforceEven(entry, min, max);
        EnforceEven(entry, min, max);
    }

    private static void EnforceEven(ConfigEntry<int> entry, int min, int max)
    {
        int value = Mathf.Clamp(entry.Value, min, max);
        if ((value & 1) != 0)
        {
            value = (value == max) ? value - 1 : value + 1;
        }
        if (value != entry.Value)
        {
            entry.Value = value;
        }
    }

    public static int GetLength()
    {
        return ClampEven(Length.Value, LengthMin, LengthMax);
    }

    public static int GetThickness()
    {
        return ClampEven(Thickness.Value, ThicknessMin, ThicknessMax);
    }

    public static int GetGap()
    {
        return ClampEven(Gap.Value, GapMin, GapMax);
    }

    public static int GetOutlineThickness()
    {
        return ClampEven(OutlineThickness.Value, OutlineThicknessMin, OutlineThicknessMax);
    }

    private static int ClampEven(int value, int min, int max)
    {
        value = Mathf.Clamp(value, min, max);
        if ((value & 1) != 0)
        {
            value = (value == max) ? value - 1 : value + 1;
        }
        return value;
    }
}
