using DuckovVP.Views;
using HarmonyLib;
using UnityEngine;

namespace DuckovVP.Mixin;

[HarmonyPatch(typeof(LevelConfig))]
public class MixinLevelConfig
{
    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void PostfixAwake(LevelConfig __instance)
    {
        ViewUtils.ViewsInit();
    }
}