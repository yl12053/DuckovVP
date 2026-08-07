using Duckov.MiniGames;
using DuckovVP.Views;
using HarmonyLib;

namespace DuckovVP.Mixin;

[HarmonyPatch(typeof(GamingConsoleHUD))]
public class MixinGamingConsoleHUD
{
    [HarmonyPatch("Hide")]
    [HarmonyPostfix]
    public static void PostfixHide()
    {
        ViewUtils.ExtraHUD?.LocalHide();
    }
}