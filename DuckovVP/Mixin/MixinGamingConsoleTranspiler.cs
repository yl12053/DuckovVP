using System.Collections.Generic;
using System.Reflection.Emit;
using Duckov.MiniGames;
using DuckovVP.Views;
using FeatherMod.Utils;
using HarmonyLib;
using UnityEngine;

namespace DuckovVP.Mixin;

[HarmonyPatch(typeof(GamingConsole))]
public class MixinGamingConsoleTranspiler
{
    [HarmonyPatch("OnInteractStart")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> InteractHUDTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        Debug.Log("Start transpiling");
        var method = AccessTools.Method(typeof(GamingConsoleHUD), nameof(GamingConsoleHUD.Show));
        var newMethod = AccessTools.Method(typeof(MixinGamingConsoleTranspiler), nameof(ShowReplace));
        var codes = new List<CodeInstruction>(instructions);
        bool found = false;
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Call && codes[i].operand as System.Reflection.MethodInfo == method)
            {
                codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
                i++;

                codes[i] = new CodeInstruction(OpCodes.Call, newMethod);
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogError("Transpiler not found.");
        }
        return codes;
    }

    public static void ShowReplace(GamingConsole instance)
    {
        if (instance.SelectedGame.ID.Equals(new Identifier(ModBehaviour.MODID, "hascd").ToString()))
        {
            ViewUtils.ExtraHUD?.LocalShow();
            return;
        }
        if (instance.SelectedGame.ID.Equals(new Identifier(ModBehaviour.MODID, "empty").ToString()))
        {
            return;
        }
        GamingConsoleHUD.Show();
    }
}