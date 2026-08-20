using System.Collections.Generic;
using System.Reflection.Emit;
using Duckov.MiniGames;
using DuckovVP.Console;
using DuckovVP.Views;
using FeatherMod.Minigame;
using FeatherMod.Utils;
using HarmonyLib;
using ItemStatsSystem;
using UnityEngine;

namespace DuckovVP.Mixin;

[HarmonyPatch(typeof(GamingConsole))]
public class MixinGamingConsole
{
    [HarmonyPatch("CatridgeGameID", MethodType.Getter)]
    [HarmonyPrefix]
    public static bool PrefixGameID(ref string __result, GamingConsole __instance)
    {
        var console = __instance.Console;
        if (console == null) return true;
        if (!console.Constants.GetBool(ModBehaviour.MODID + "CustomGameCon", false)) return true;
        __result = GamingConsoleUtils.GetCartridgeGameID(console);
        return false;
    }
    
    [HarmonyPatch("Cartridge", MethodType.Getter)]
    [HarmonyPrefix]
    public static bool PrefixCartridge(ref Item __result, GamingConsole __instance) {
        var console = __instance.Console;
        if (console == null) return true;
        if (!console.Constants.GetBool(ModBehaviour.MODID + "CustomGameCon", false)) return true;
        __result = GamingConsoleUtils.GetFakeCartridge(console);
        return false;
    }


}