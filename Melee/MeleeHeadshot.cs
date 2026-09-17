using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

[HarmonyPatch(typeof(Weapon))]
public static class MeleeHeadshots
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void SetMeleeHeadMultiplier(Weapon __instance)
    {
        string name = __instance.behaviour.weaponName;

        if (name == "Baseball Bat"
            || name == "Couperet"
            || name == "Curved Knife"
            || name == "Flamberge"
            || name == "GodSword"
            || name == "Impetus"
            || name == "Javal Mahmaer"
            || name == "Katana"
            || name == "Nizeh"
            || name == "Oklahoma"
            || name == "Stylus")
        {
            __instance.headMultiplier = 1;
        }
    }
}

