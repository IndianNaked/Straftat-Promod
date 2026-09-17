using System.Text.RegularExpressions;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace StraftatMods
{
    public static class ProModBanner
    {
        public static void OnGUI()
        {
        }
    }

    public static class ProModStartScreen
    {
        private const string TitleText = "ProMod";
        private const string SubText = "by Glizzmn";
        private static readonly Regex VersionRegex = new Regex("(?i)\\b(version|v)\\s*\\d", RegexOptions.Compiled);

        public static void Apply(MenuController controller)
        {
            if (controller == null || controller.startMenu == null)
            {
                return;
            }

            var texts = controller.startMenu.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts == null || texts.Length == 0)
            {
                return;
            }

            TextMeshProUGUI target = null;
            float bestSize = -1f;
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i];
                if (t == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(t.text) && t.fontSize > bestSize)
                {
                    bestSize = t.fontSize;
                    target = t;
                }

                if (!string.IsNullOrEmpty(t.text) && VersionRegex.IsMatch(t.text))
                {
                    t.gameObject.SetActive(false);
                }
            }

            if (target == null)
            {
                return;
            }

            target.text = TitleText + "\n<size=30%>" + SubText + "</size>";
            target.alignment = TextAlignmentOptions.Center;
        }
    }

    [HarmonyPatch(typeof(MenuController), "Start")]
    public static class ProMod_MenuController_Start
    {
        private static void Postfix(MenuController __instance)
        {
            ProModStartScreen.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(MenuController), "OpenStartMenu")]
    public static class ProMod_MenuController_OpenStartMenu
    {
        private static void Postfix(MenuController __instance)
        {
            ProModStartScreen.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(PauseManager), "ShowInfoPopup")]
    public static class ProMod_HideNonVanillaPopup
    {
        private static bool Prefix(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }
            return !text.StartsWith("Non-vanilla friendly mods detected");
        }
    }
}
