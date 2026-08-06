using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Duckov.MiniGames;
using Duckov.UI;
using DuckovVP.Console;
using DuckovVP.Views;
using FeatherMod;
using FeatherMod.Events;
using FeatherMod.Events.GameEvents;
using HarmonyLib;
using LibVLCSharp.Shared;
using ModSetting;
using ModSetting.Api;
using UnityEngine;
using ItemUtils = DuckovVP.Items.ItemUtils;

namespace DuckovVP;

public class ModBehaviour : Duckov.Modding.ModBehaviour, IHasModid
{
    public static ModBehaviour? Instance { get; private set; }
    private Harmony? harmony;
    public static LibVLC? vlc { get; private set; }

    public const string MODID = "DuckovVP";

    public volatile bool isActivated = false;
    private ConcurrentQueue<Func<UniTask>>? vlcTasks;

    public bool IsOnGameConsole { get; private set; }

    public UniTask Enqueue(Func<UniTask> task)
    {
        if (!isActivated) return UniTask.CompletedTask;

        var tcs = new UniTaskCompletionSource();

        vlcTasks.Enqueue(async UniTask () =>
        {
            try
            {
                await task();
                tcs.TrySetResult();
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        });

        return tcs.Task;
    }

    public string GetModid()
    {
        return MODID;
    }

    private Config? _config;
    public Config Cfg => _config ?? Config.Default;

    protected override void OnAfterSetup()
    {
        base.OnAfterSetup();
        GamingConsole.OnGamingConsoleInteractChanged += Change;
        Instance = this;
        isActivated = true;

        I18n.InitI18n(GetModid());
        var settingsBuilder = SettingsBuilder.Create(info);
        _config = new Config(settingsBuilder);
        EventBusManager.Instance.Sync.Register<LanguageChangedEvent>(evt => Cfg.RefreshUI(), -1);

        vlcTasks = new();
        var t = new Thread(() =>
        {
            var localTasks = vlcTasks;
            while (isActivated)
            {
                while (localTasks.TryDequeue(out var task)) task().GetAwaiter().GetResult();
            }

            GC.KeepAlive(this);
        });
        t.Name = "VLCT";
        t.Start();

        harmony = new Harmony(GetModid());
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Enqueue(async UniTask () =>
        {
            if (!Core.LibVLCLoaded)
            {
                Core.Initialize();
            }

            if (vlc == null)
            {
                vlc = new LibVLC("--verbose=2", "--aout=amem", "--vout=vmem");
                vlc.Log += (sender, args) => { Debug.Log(args.FormattedLog); };
            }
        }).AsTask().GetAwaiter().GetResult();

        ItemUtils.Init();
        GamingConsoleUtils.Init();
    }

    protected override void OnBeforeDeactivate()
    {
        base.OnBeforeDeactivate();

        harmony?.UnpatchAll();
        harmony = null;
        Enqueue(async UniTask () =>
        {
            if (vlc != null)
            {
                vlc.Dispose();
                vlc = null;
            }
        }).Forget();

        vlcTasks = null;
        isActivated = false;

        Setting.Clear();

        GamingConsole.OnGamingConsoleInteractChanged -= Change;
    }

    private void Change(bool val)
    {
        IsOnGameConsole = val;
    }

    public static string GetName(string raw)
    {
        return SodaCraft.Localizations.LocalizationManager.TryGetOverrideText(raw, out string name) ? name : raw;
    }

    protected void OnGUI()
    {
        UnityEngine.Event e = UnityEngine.Event.current;
        if (e != null && e.isKey)
        {
            if (e.type == UnityEngine.EventType.KeyDown && e.keyCode == KeyCode.F11)
            {
                if (ViewUtils.burnerView != null)
                {
                    ViewUtils.burnerView.gameObject.SetActive(true);
                    ViewUtils.burnerView.Open(null);
                }
            }
        }
    }
}