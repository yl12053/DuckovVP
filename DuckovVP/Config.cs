using System;
using ModSetting.Api;
using Unity.VisualScripting;
using UnityEngine;

namespace DuckovVP;

public class Config
{
    public KeyCode SkipBackward = KeyCode.LeftArrow;
    public KeyCode SkipForward = KeyCode.RightArrow;
    public KeyCode VolumeUp = KeyCode.UpArrow;
    public KeyCode VolumeDown = KeyCode.DownArrow;
    public KeyCode Pause = KeyCode.Return;
    public KeyCode ToStart = KeyCode.Home;
    public KeyCode Mute = KeyCode.Q;
    public KeyCode SwitchStretch = KeyCode.S;

    private SettingsBuilder? _settingsBuilder;

    public static Config Default = new();

    private Config()
    {
        _settingsBuilder = null;
    }

    public Config(SettingsBuilder settingsBuilder): this()
    {
        this._settingsBuilder = settingsBuilder;
        
        if (settingsBuilder.HasConfig())
        {
            settingsBuilder.GetSavedValue(nameof(SkipBackward), out SkipBackward);
            settingsBuilder.GetSavedValue(nameof(SkipForward), out SkipForward);
            settingsBuilder.GetSavedValue(nameof(VolumeUp), out VolumeUp);
            settingsBuilder.GetSavedValue(nameof(VolumeDown), out VolumeDown);
            settingsBuilder.GetSavedValue(nameof(Pause), out Pause);
            settingsBuilder.GetSavedValue(nameof(ToStart), out ToStart);
            settingsBuilder.GetSavedValue(nameof(Mute), out Mute);
            settingsBuilder.GetSavedValue(nameof(SwitchStretch), out SwitchStretch);
        }

        RefreshUI();
    }

    public event Action<string, KeyCode> Announce; 

    public void RefreshUI()
    {
        _settingsBuilder?.Clear((s) =>
        {
            _settingsBuilder
                .AddKeybinding(nameof(SkipBackward), ModBehaviour.GetName($"DuckovVP.gui.key.{nameof(SkipBackward)}"),
                    SkipBackward, Default.SkipBackward, (k) =>
                    {
                        SkipBackward = k;
                        Announce("SkipBackward", k);
                    })
                .AddKeybinding(nameof(SkipForward), ModBehaviour.GetName($"DuckovVP.gui.key.{nameof(SkipForward)}"),
                    SkipForward, Default.SkipForward, (k) =>
                    {
                        SkipForward = k;
                        Announce("SkipForward", k);
                    })
                .AddKeybinding(nameof(VolumeUp), ModBehaviour.GetName($"DuckovVP.gui.key.{nameof(VolumeUp)}"), VolumeUp,
                    Default.VolumeUp, (k) =>
                    {
                        VolumeUp = k;
                        Announce("VolumeUp", k);
                    })
                .AddKeybinding(nameof(VolumeDown), ModBehaviour.GetName($"DuckovVP.gui.key.{nameof(VolumeDown)}"),
                    VolumeDown, Default.VolumeDown, (k) =>
                    {
                        VolumeDown = k;
                        Announce("VolumeDown", k);
                    })
                .AddKeybinding(nameof(Pause), ModBehaviour.GetName($"DuckovVP.gui.key.{nameof(Pause)}"), Pause,
                    Default.Pause, (k) =>
                    {
                        Pause = k;
                        Announce("Pause", k);
                    })
                .AddKeybinding(nameof(ToStart), ModBehaviour.GetName($"DuckovVP.gui.key.{nameof(ToStart)}"), ToStart,
                    Default.ToStart, (k) =>
                    {
                        ToStart = k;
                        Announce("ToStart", k);
                    })
                .AddKeybinding(nameof(Mute), ModBehaviour.GetName($"DuckovVP.gui.key.{nameof(Mute)}"), Mute,
                    Default.Mute, (k) =>
                    {
                        Mute = k;
                        Announce("Mute", k);
                    })
                .AddKeybinding(nameof(SwitchStretch), ModBehaviour.GetName($"DuckovVP.gui.key.{nameof(SwitchStretch)}"), SwitchStretch,
                    Default.SwitchStretch, (k) =>
                    {
                        SwitchStretch = k;
                        Announce("SwitchStretch", k);
                    });
        });
    }
}