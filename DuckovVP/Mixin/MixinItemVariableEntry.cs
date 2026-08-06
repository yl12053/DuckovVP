using Duckov.UI;
using Duckov.Utilities;
using HarmonyLib;

namespace DuckovVP.Mixin;

[HarmonyPatch(typeof(ItemVariableEntry))]
public class MixinItemVariableEntry
{
    [HarmonyPatch("Refresh")]
    [HarmonyPostfix]
    public static void PostfixRefresh(ItemVariableEntry __instance)
    {
        var target = __instance.target;
        if (target.DataType == CustomDataType.String && target.GetString().StartsWith("DuckovVPRaw:"))
        {
            var disp = target.GetString()[12..];
            if (__instance.value.GetPreferredValues(disp).x <= 287)
            {
                __instance.value.text = disp;
            }

            for (var i = 0; i < disp.Length; i++)
            {
                var tryDisp = "..." + target.GetString()[i..];
                if (__instance.value.GetPreferredValues(tryDisp).x <= 287)
                {
                    __instance.value.text = tryDisp;
                    return;
                }
            }
        }
    }
}