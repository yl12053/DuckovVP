using HarmonyLib;
using SodaCraft.Localizations;

namespace DuckovVP.Mixin;

[HarmonyPatch(typeof(LocalizationManager))]
public class MixinLocalizationManager
{
    [HarmonyPatch("TryGetOverrideText")]
    [HarmonyPrefix]
    public static bool TryGetOverrideText(ref bool __result, string key, out string value)
    {
        if (key.StartsWith("DuckovVPRaw:"))
        {
            value = key[12..];
            __result = true;
            return false;
        }

        value = "";
        return true;
    }
}