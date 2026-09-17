using System;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace StraftatMods
{
    [BepInPlugin("com.glizzmn.promod", "ProMod", "1.0.0")]
    public sealed class PluginMain : BaseUnityPlugin
    {
        private const int LocalVersion = 3;
        private const string VersionApiUrl = "https://api.github.com/repos/GlizzmnDEV/RotateEnvironment/contents/version.md?ref=main";

        private Harmony harmony;

        private void Awake()
        {
            Logger.LogInfo("ProMod loaded.");
            harmony = new Harmony("com.glizzmn.promod");
            PatchAllProMod();
        }

        private void OnGUI()
        {
            ProModBanner.OnGUI();
        }

        private void PatchAllProMod()
        {
            var asm = Assembly.GetExecutingAssembly();
            var types = asm.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (!Attribute.IsDefined(type, typeof(HarmonyPatch), false))
                {
                    continue;
                }
                if (Attribute.IsDefined(type, typeof(DamageNumbersPatchAttribute), false))
                {
                    continue;
                }
                if (Attribute.IsDefined(type, typeof(HitmarkersPatchAttribute), false))
                {
                    continue;
                }
                if (Attribute.IsDefined(type, typeof(HitSoundsPatchAttribute), false))
                {
                    continue;
                }
                harmony.CreateClassProcessor(type).Patch();
            }
        }
    }
}
