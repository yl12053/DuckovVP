using Duckov;
using DuckovVP.Console;
using HarmonyLib;

namespace DuckovVP.Mixin;

[HarmonyPatch(typeof(AudioManager.Bus))]
public class MixinBus
{
    [HarmonyPatch("Volume", MethodType.Getter)]
    [HarmonyPostfix]
    public static void PostfixVolume(ref float __result, AudioManager.Bus __instance)
    {
        if (__instance.Name.Equals("Master/Music"))
        {
            __result *= PlayerBehaviour.MusicBusVolumeMultiplier;
        }
    }
}