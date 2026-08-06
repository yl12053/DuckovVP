using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Duckov.MiniGames;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using LibVLCSharp.Shared;
using UnityEngine;
using Channel = FMOD.Channel;
using Debug = UnityEngine.Debug;

namespace DuckovVP.Console;

public class PlayerBehaviour: MiniGameBehaviour
{
    private static Texture2D TextureS = new(500, 500);
    private static Texture2D TextureM = new(500, 500);
    static PlayerBehaviour() 
    {
        TextureS.LoadImage(
            File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "assets/gui/speaker.png")));
        TextureM.LoadImage(
            File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "assets/gui/speaker_mute.png")));
    }

    private bool isPlaySuccess = false;
    private string? currentPlay;
    
    private Transform disp;
    private bool isAudio;

    private Texture2D? AlbumTexture;
    
    private Renderer renderer;
    private Renderer? AlbumRenderer;
    private MediaPlayer? mediaPlayer;
    private Media? media;
    private Texture2D? texture;
    private IntPtr? nativeBuffer;
    private RingBuffer<short> buffer;
    private Sound? ISound;
    private Channel? IChannel;
    private ChannelGroup? IChannelSub;
    private volatile bool needsUpdate = false;

    private GCHandle thisHandle;

    private readonly ConcurrentQueue<Action> executionQueue = new();
    private ConcurrentQueue<IntPtr> toFree = new();

    private Renderer PM5544;
    private Renderer PauseRenderer;
    private Renderer PauseRenderer2;
    private bool _doRenderPauseIcon = true;
    
    private Renderer? TimeLineRenderer;
    private Renderer? TimeLineRenderer2;
    private Renderer? IndicatorRenderer;
    private Renderer? IndicatorRenderer2;
    private Transform? IndicatorTransform;

    private Renderer? MuteRenderer;
    private Renderer? MuteRenderer2;

    private Renderer? SpeakerRenderer;
    private Renderer? SpeakerRenderer2;
    private Renderer? VolShadowRenderer;
    private Renderer? VolBackRenderer;
    private Renderer? VolFrontRenderer;
    private Renderer? VolSpeakerIconRenderer;
    private Renderer? VolSpeakerIconShadowRenderer;
    private Transform? VolFrontTransform;

    private TextScroller? TextName;
    private TextScroller? TextArtist;

    private static readonly HttpClient _httpClient = new();

    private volatile bool isDone = false;

    private CancellationTokenSource? _fadeCts;
    private CancellationTokenSource? _fadeCtsMus;

    private bool isSwitching = false;

    private bool _wasPaused;

    private float _vol = 1f;
    private float volumeMultiplier
    {
        get => _vol;
        set
        {
            _vol = value;
            IChannel?.setVolume(volumeMultiplier);
        }
    }
    
    private static volatile float musicBusVolumeMultiplier = 1f;
    private static Bus bus = RuntimeManager.GetBus("bus:/Master/Music");

    public static float MusicBusVolumeMultiplier
    {
        get => musicBusVolumeMultiplier;
        set
        {
            musicBusVolumeMultiplier = value;
            bus.setVolume(GameManager.Instance.audioManager.musicBus.Volume);
        }
    }

    private int _width;
    private int _height;

    public const float MAX_WIDTH = 427f;
    public const float MAX_HEIGHT = 316.5f;
    private static readonly Vector3 DefaultSize = new(MAX_WIDTH, -MAX_HEIGHT, 1f);
    
    public async UniTask FadeTo(float fullduration, bool isFadeout, CancellationToken ct = default)
    {
        _fadeCts?.Cancel();
        _fadeCts = new CancellationTokenSource();

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_fadeCts.Token, ct);
        var step = 1f / fullduration;
        if (isFadeout) step = -step;
        try
        {
            bool shouldStop = false;
            while (!shouldStop && IChannel != null)
            {
                var mul = volumeMultiplier;
                mul += step * Time.deltaTime * 1000;
                if (mul >= 1f)
                {
                    mul = 1f;
                    shouldStop = true;
                }
                else if (mul <= 0f)
                {
                    mul = 0f;
                    shouldStop = true;
                }
                
                volumeMultiplier = mul;
                if (shouldStop) return;
                await UniTask.Yield(PlayerLoopTiming.Update, linkedCts.Token);
            }
        }
        catch (OperationCanceledException)
        {

        }
        finally
        {
            linkedCts.Dispose();
        }
    }
    
    public async UniTask FadeMus(float fullduration, bool isFadeout, CancellationToken ct = default)
    {
        _fadeCtsMus?.Cancel();
        _fadeCtsMus = new CancellationTokenSource();

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_fadeCtsMus.Token, ct);
        var step = 1f / fullduration;
        if (isFadeout) step = -step;
        try
        {
            bool shouldStop = false;
            while (!shouldStop)
            {
                var mul = MusicBusVolumeMultiplier;
                mul += step * Time.deltaTime * 1000;
                if (mul >= 1f)
                {
                    mul = 1f;
                    shouldStop = true;
                }
                else if (mul <= 0f)
                {
                    mul = 0f;
                    shouldStop = true;
                }
                
                MusicBusVolumeMultiplier = mul;
                if (shouldStop) return;
                await UniTask.Yield(PlayerLoopTiming.Update, linkedCts.Token);
            }
        }
        catch (OperationCanceledException)
        {

        }
        finally
        {
            linkedCts.Dispose();
        }
    }
    
    protected override void OnUpdate(float deltaTime)
    {
        while (executionQueue.TryDequeue(out var action)) action?.Invoke();
        if (needsUpdate)
        {
            if (nativeBuffer.HasValue && texture != null) texture.LoadRawTextureData(nativeBuffer.Value, texture.height * texture.width * 4);
            texture?.Apply();
            needsUpdate = false;
        }

        if (mediaPlayer != null && IndicatorTransform != null)
        {
            var oldpos = IndicatorTransform.localPosition;
            oldpos.x = 341.6f * mediaPlayer.Position - 170.8f;
            IndicatorTransform.localPosition = oldpos;
        }

        if (isSwitching) return;
        var console = Game.Console.Console;
        if (console != null)
        {
            var cd = console.Slots["cd"].Content;
            var rawV = cd?.GetVariableEntry("Path")?.GetString();
            bool needRefresh = true;
            if (rawV?.StartsWith("DuckovVPRaw:") ?? false)
            {
                var comp = rawV[12..];
                needRefresh = !comp.Equals(currentPlay);
            }
            if (needRefresh) 
            {
                Game.Console.CreateGame(Game.Console.SelectedGame);
                isSwitching = true;
            }
        }
    }

    private static unsafe void Play(IntPtr data, IntPtr samples, uint count, long pts)
    {
        PlayerBehaviour zthis = (PlayerBehaviour) GCHandle.FromIntPtr(data).Target;
        ReadOnlySpan<short> audioSpan = new ReadOnlySpan<short>((void*)samples, (int) count * 2);
        zthis.buffer.TryEnqueue(audioSpan);
    }

    private static void Pause(IntPtr data, long pts)
    {
        PlayerBehaviour zthis = (PlayerBehaviour) GCHandle.FromIntPtr(data).Target;
        if (zthis.IChannel.HasValue) zthis.IChannel.Value.setPaused(true);
    }
    
    private static void Resume(IntPtr data, long pts)
    {
        PlayerBehaviour zthis = (PlayerBehaviour) GCHandle.FromIntPtr(data).Target;
        if (zthis.IChannel.HasValue) zthis.IChannel.Value.setPaused(false);
    }
    
    private static void Flush(IntPtr data, long pts)
    {
        PlayerBehaviour zthis = (PlayerBehaviour) GCHandle.FromIntPtr(data).Target;
        zthis.buffer.Clear();
    }
    
    private static IntPtr Lock(IntPtr opaque, IntPtr planes)
    {
        PlayerBehaviour zthis = (PlayerBehaviour) GCHandle.FromIntPtr(opaque).Target;
        unsafe
        {
            var ptr = zthis.nativeBuffer.Value;
            ((void**)planes)[0] = (void*) ptr;
            return ptr;
        }
    }

    private static void Display(IntPtr opaque, IntPtr picture)
    {
        PlayerBehaviour zthis = (PlayerBehaviour) GCHandle.FromIntPtr(opaque).Target;
        zthis.needsUpdate = true;
    }

    private uint FormatCallback(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        Marshal.Copy("RV32"u8.ToArray(), 0, chroma, 4);
        pitches = width * 4;
        lines = height;
        opaque = GCHandle.ToIntPtr(thisHandle);
        var tcs = new UniTaskCompletionSource();
        _width = (int) width;
        _height = (int) height;
        uint size = pitches * lines;
        executionQueue.Enqueue(() =>
        {
            CreateTexture(_width, _height, size);
            RefreshSize();
            tcs.TrySetResult();
        });
        tcs.Task.AsTask().GetAwaiter().GetResult();
        return 1;
    }

    private MediaPlayer.LibVLCVideoFormatCb fmtDelegate;
    private GCHandle fmtGCHandle;

    private static void VideoCleanup(ref IntPtr opaque)
    {
        GCHandle handle;
        try
        {
            handle = GCHandle.FromIntPtr(opaque);
        }
        catch (NullReferenceException npe)
        {
            return;
        }
        PlayerBehaviour zthis = (PlayerBehaviour) handle.Target;
        while (zthis.toFree.TryDequeue(out var obj))
        {
            Marshal.FreeHGlobal(obj);
        }
        if (zthis.nativeBuffer.HasValue)
        {
            Marshal.FreeHGlobal(zthis.nativeBuffer.Value);
            zthis.nativeBuffer = null;
        }
        handle.Free();
    }
    
    private bool Assign(string comp, out Transform? tmp, out Renderer? renderers)
    {
        tmp = Game.gameObject.transform.Find(comp);
        if (tmp == null) 
        {
            Debug.LogError($"No game object names {comp}");
            renderers = null;
            return false;
        }
        renderers = tmp.GetComponent<Renderer>();
        if (renderers == null)
        {
            Debug.LogError($"No renderer for {comp}");
            return false;
        }
        return true;
    }

    private CancellationTokenSource? _ctsFadeOutBL;
    async UniTask FadeOutBaseLine(CancellationToken tk)
    {
        if (isAudio) return;
        _ctsFadeOutBL?.Cancel();
        _ctsFadeOutBL = new CancellationTokenSource();
        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tk, _ctsFadeOutBL.Token))
        {
            await UniTask.Delay(5000, cancellationToken: linkedCts.Token);
            TimeLineRenderer.enabled = false;
            TimeLineRenderer2.enabled = false;
            IndicatorRenderer.enabled = false;
            IndicatorRenderer2.enabled = false;
        };
    }

    private CancellationTokenSource? _ctsFadeOutVol;

    async UniTask FadeOutVol(CancellationToken tk)
    {
        _ctsFadeOutVol?.Cancel();
        _ctsFadeOutVol = new CancellationTokenSource();
        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tk, _ctsFadeOutVol.Token))
        {
            await UniTask.Delay(5000, cancellationToken: linkedCts.Token);
            VolShadowRenderer.enabled = false;
            VolBackRenderer.enabled = false;
            VolFrontRenderer.enabled = false;
            VolSpeakerIconRenderer.enabled = false;
            VolSpeakerIconShadowRenderer.enabled = false;
        }
    }

    private void SetVolumeBar()
    {
        if (VolFrontTransform == null) return;
        VolFrontTransform.localPosition = new Vector3(180f, Mathf.Lerp(-35f, 25f, VolumeOfMachine), 4.6f);
        VolFrontTransform.localScale = new Vector3(10f, 120f * VolumeOfMachine, 1f);
    }
    
    protected override void Start()
    {
        base.Start();

        bool success = Assign("screen", out disp, out renderer);
        success &= Assign("album", out _, out AlbumRenderer);

        if (ModBehaviour.vlc == null)
        {
            Debug.LogError("VLC not initialized!");
            return;
        }

        success &= Assign("pm5544", out _, out PM5544);
        success &= Assign("pause", out _, out PauseRenderer);
        success &= Assign("pause2", out _, out PauseRenderer2);

        IndicatorTransform = Game.gameObject.transform.Find("indicator");
        if (IndicatorTransform == null) 
        {
            Debug.LogError("Cant find indicator");
            return;
        }
        IndicatorRenderer = IndicatorTransform.GetComponent<Renderer>();
        if (IndicatorRenderer == null) 
        {
            Debug.LogError("No Renderer for indicator");
            return;
        }

        var in2 = IndicatorTransform.Find("indicatorShadow");
        if (in2 == null) 
        {
            Debug.LogError("Cant find indicator shadow");
            return;
        }

        IndicatorRenderer2 = in2.GetComponent<Renderer>();
        if (IndicatorRenderer2 == null)
        {
            Debug.LogError("No renderer for indicator shadow renderer");
            return;
        }
        
        success &= Assign("baseLine", out _, out TimeLineRenderer);
        success &= Assign("baseLineShadow", out _, out TimeLineRenderer2);
        success &= Assign("mute", out _, out MuteRenderer);
        success &= Assign("mute2", out _, out MuteRenderer2);
        success &= Assign("speakerIcon", out _, out SpeakerRenderer);
        success &= Assign("speakerShadow", out _, out SpeakerRenderer2);
        success &= Assign("volShadow", out _, out VolShadowRenderer);
        success &= Assign("volBack", out _, out VolBackRenderer);
        success &= Assign("volBar", out VolFrontTransform, out VolFrontRenderer);
        success &= Assign("speakerIcon", out _, out VolSpeakerIconRenderer);
        success &= Assign("speakerShadow", out _, out VolSpeakerIconShadowRenderer);
        var fname = Game.gameObject.transform.Find("FName");
        if (fname == null) 
        {
            Debug.LogError("Cant find file name disp");
            return;
        }

        TextName = fname.GetComponent<TextScroller>();
        if (TextName == null)
        {
            Debug.LogError("Cant find textname");
            return;
        }
        TextName.text = "Unnamed CD";
        TextName.Restart();
        
        var aname = Game.gameObject.transform.Find("AName");
        if (aname == null) 
        {
            Debug.LogError("Cant find artist name disp");
            return;
        }

        TextArtist = aname.GetComponent<TextScroller>();
        if (TextArtist == null)
        {
            Debug.LogError("Cant find textartist");
            return;
        }
        TextArtist.text = "Unknown Artist";
        TextArtist.Restart();
        
        if (!success) return;

        var console = Game.Console.Console;
        if (console != null)
        {
            var cd = console.Slots["cd"].Content;
            var rawV = cd?.GetVariableEntry("Path")?.GetString();
            isPlaySuccess = rawV?.StartsWith("DuckovVPRaw:") ?? false;
            if (isPlaySuccess)
            {
                currentPlay = rawV[12..];
            }
            else
            {
                PM5544.enabled = true;
            }
        }
        MuteRenderer.enabled = IsMuteMachine;
        MuteRenderer2.enabled = IsMuteMachine;
        SpeakerRenderer.material.mainTexture = IsMuteMachine ? TextureM : TextureS;
        SpeakerRenderer2.material.mainTexture = IsMuteMachine ? TextureM : TextureS;
        SetVolumeBar();
        
        GamingConsole.OnGamingConsoleInteractChanged += GamingConsoleInteractChanged;

        thisHandle = GCHandle.Alloc(this, GCHandleType.Pinned);
        
        buffer = new RingBuffer<short>(131072);
        IntPtr contextPtr = GCHandle.ToIntPtr(thisHandle);
        CREATESOUNDEXINFO exinfo = new()
        {
            cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO)),
            format = SOUND_FORMAT.PCM16,
            defaultfrequency = 44100,
            decodebuffersize = 1024 * 2 * sizeof(short),
            length = 1024 * 2 * sizeof(short),
            numchannels = 2,
            pcmreadcallback = HandleCallback,
            pcmsetposcallback = setpos,
            userdata = contextPtr
        };
        RuntimeManager.CoreSystem.createSound((string) null, MODE.CREATESTREAM | MODE.OPENUSER | MODE.LOOP_NORMAL,
            ref exinfo, out var sound);
        var bus = RuntimeManager.GetBus("bus:/Master/SFX");
        bus.getChannelGroup(out var channelGroup);
        var result = RuntimeManager.CoreSystem.createChannelGroup("VPChild", out var subchan);
        Channel channel;
        if (result == RESULT.OK)
        {
            channelGroup.addGroup(subchan);
            result = RuntimeManager.CoreSystem.playSound(sound, subchan, false, out channel);
            if (result == RESULT.OK)
            {
                channel.setVolume(ModBehaviour.Instance?.IsOnGameConsole ?? false ? 1f : 0f);
                subchan.setVolume(VolumeOfMachine);
                subchan.setMute(IsMuteMachine);
                ISound = sound;
                IChannel = channel;
                IChannelSub = subchan;
            }
            else
            {
                subchan.release();
            }
        }
        if (result != RESULT.OK)
        {
            Debug.LogError(result);
            ISound = null;
            IChannel = null;
            IChannelSub = null;
        }
        
        fmtDelegate = FormatCallback;
        fmtGCHandle = GCHandle.Alloc(fmtDelegate, GCHandleType.Pinned);

        InitializeMedia();
    }

    private void InitializeMedia()
    {
        if (currentPlay != null) ModBehaviour.Instance.Enqueue(async UniTask () =>
        {
            var parsed = false;
            if (mediaPlayer != null) mediaPlayer.Dispose();
            if (media != null) media.Dispose();
            media = new Media(ModBehaviour.vlc, new Uri(currentPlay).AbsoluteUri, FromType.FromLocation);
            media.ParsedChanged += (sender, e) =>
            {
                if (e.ParsedStatus == MediaParsedStatus.Done)
                {
                    string? artworkUrl = media.Meta(MetadataType.ArtworkURL);
                    if (!string.IsNullOrEmpty(artworkUrl))
                    {
                        var ct = this.GetCancellationTokenOnDestroy();
                        async UniTask Task()
                        {
                            try
                            {
                                Uri uri = new(artworkUrl);
                                byte[]? bytes = null;
                                if (uri.IsFile)
                                {
                                    string localPath = uri.LocalPath;
                                    if (File.Exists(localPath))
                                    {
                                        bytes = await File.ReadAllBytesAsync(localPath, ct);
                                    }
                                    else
                                    {
                                        return;
                                    }
                                } else if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                                {
                                    var response = await _httpClient.GetAsync(uri, ct);
                                    response.EnsureSuccessStatusCode();
                                    bytes = await response.Content.ReadAsByteArrayAsync();
                                }
                                if (bytes == null) return;
                                AlbumTexture = new(50, 50);
                                AlbumTexture.LoadImage(bytes);
                                AlbumRenderer.material.mainTexture = AlbumTexture;
                            }
                            catch (Exception se)
                            {
                                Debug.LogException(se);
                            }
                        }
                        executionQueue.Enqueue(() =>
                        {
                            StartCoroutine(Task().ToCoroutine());
                        });
                    }

                    string? titleString = media.Meta(MetadataType.Title);
                    if (!string.IsNullOrEmpty(artworkUrl))
                    {
                        executionQueue.Enqueue(() =>
                        {
                            TextName.text = titleString;
                            TextName.Restart();
                        });
                    }

                    string? artistString = media.Meta(MetadataType.Artist);
                    if (string.IsNullOrEmpty(artistString)) artistString = media.Meta(MetadataType.AlbumArtist);
                    if (!string.IsNullOrEmpty(artistString))
                    {
                        executionQueue.Enqueue(() =>
                        {
                            TextArtist.text = artistString;
                            TextArtist.Restart();
                        });
                    }
                }
            };
            mediaPlayer = new MediaPlayer(ModBehaviour.vlc);
            mediaPlayer.Media = media;
            mediaPlayer.SetVideoCallbacks(Lock, null, Display);
            mediaPlayer.SetVideoFormatCallbacks(fmtDelegate, VideoCleanup);
            mediaPlayer.SetAudioFormat("S16N", 44100, 2);
            mediaPlayer.SetAudioCallbacks(Play, Pause, Resume, Flush, null);
            mediaPlayer._audioUserData = GCHandle.ToIntPtr(thisHandle);
            mediaPlayer.EndReached += (sender, events) =>
            {
                executionQueue.Enqueue(() =>
                {
                    PM5544.enabled = true;
                    isDone = true;
                });
            };
            mediaPlayer.EncounteredError += (sender, events) =>
            {
                executionQueue.Enqueue(() =>
                {
                    isPlaySuccess = false;
                    PM5544.enabled = true;
                });
            };
            mediaPlayer.Playing += (sender, events) =>
            {
                if (!parsed)
                {
                    parsed = true;
                    isAudio = mediaPlayer.VideoTrackCount == 0;
                    if (isAudio)
                    {
                        executionQueue.Enqueue(() =>
                        {
                            renderer.enabled = false;
                            TimeLineRenderer.enabled = true;
                            TimeLineRenderer2.enabled = true;
                            IndicatorRenderer.enabled = true;
                            IndicatorRenderer2.enabled = true;
                        });
                    }
                }
                if (Interlocked.CompareExchange(ref _needDoublePause, 0, 1) == 1)
                {
                    executionQueue.Enqueue(() =>
                    {
                        IEnumerator task()
                        {
                            _doRenderPauseIcon = false;
                            mediaPlayer.Pause();
                            yield return new WaitForSeconds(0.3f);
                            mediaPlayer.Pause();
                            _doRenderPauseIcon = true;
                        }
                        executionQueue.Enqueue(() => StartCoroutine(task()));
                    });
                }
                executionQueue.Enqueue(() =>
                {
                    PauseRenderer.enabled = false;
                    PauseRenderer2.enabled = false;
                    if (_wasPaused)
                    {
                        StartCoroutine(FadeOutBaseLine(this.GetCancellationTokenOnDestroy()).ToCoroutine());
                    }
                });
            };
            mediaPlayer.Paused += (sender, events) =>
            {
                if (!_doRenderPauseIcon)
                {
                    executionQueue.Enqueue(() =>
                    {
                        PauseRenderer.enabled = false;
                        PauseRenderer2.enabled = false;
                    });
                    return;
                }

                executionQueue.Enqueue(() =>
                {
                    PauseRenderer.enabled = true;
                    PauseRenderer2.enabled = true;
                    TimeLineRenderer.enabled = true;
                    TimeLineRenderer2.enabled = true;
                    IndicatorRenderer.enabled = true;
                    IndicatorRenderer2.enabled = true;
                });
                _wasPaused = true;
            };
            isDone = false;
            executionQueue.Enqueue(() => PM5544.enabled = false);
            mediaPlayer.Play();
            media.Parse(MediaParseOptions.ParseNetwork | MediaParseOptions.FetchLocal | MediaParseOptions.FetchNetwork);
        }).Forget();
    }

    private void GamingConsoleInteractChanged(bool attached)
    {
        if (attached)
        {
            Debug.Log($"Is Done? {isDone}");
            if (isDone)
            {
                _doRenderPauseIcon = false;
                Replay();
            }
        }
        FadeTo(1000f, !attached, this.GetCancellationTokenOnDestroy()).Forget();
        FadeMus(1000f, attached, this.GetCancellationTokenOnDestroy()).Forget();
    }

    private int _needDoublePause = 0;
    
    private void Replay()
    {
        if (!isPlaySuccess) return;
        isDone = false;
        PM5544.enabled = false;
        PauseRenderer.enabled = false;
        PauseRenderer2.enabled = false;
        if (!_doRenderPauseIcon)
        {
            _ctsFadeOutBL?.Cancel();
            TimeLineRenderer.enabled = false;
            TimeLineRenderer2.enabled = false;
            IndicatorRenderer.enabled = false;
            IndicatorRenderer2.enabled = false;
            _ctsFadeOutVol?.Cancel();
            VolShadowRenderer.enabled = false;
            VolBackRenderer.enabled = false;
            VolFrontRenderer.enabled = false;
            VolSpeakerIconRenderer.enabled = false;
            VolSpeakerIconShadowRenderer.enabled = false;
        }
        ModBehaviour.Instance.Enqueue(async UniTask () =>
        {
            if (mediaPlayer == null) return;
            if (media == null) return;
            _needDoublePause = 1;
            mediaPlayer.Media = media;
            mediaPlayer.Time = 0;
            mediaPlayer.Play();
        }).Forget();
    }
    
    private void CreateTexture(int width, int height, uint size)
    {
        if (!nativeBuffer.HasValue && texture != null && texture.width == width && texture.height == height) return;
        var oldTexture = texture;
        texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
        texture.Apply();
        var oldBuffer = nativeBuffer;
        nativeBuffer = Marshal.AllocHGlobal((IntPtr) size);
        renderer.material.mainTexture = texture;
        if (oldBuffer != null) Marshal.FreeHGlobal(oldBuffer.Value);
        if (oldTexture != null) Destroy(oldTexture);
    }
    
    public static RESULT HandleCallback(IntPtr soundPtr, IntPtr data, uint datalen)
    {
        if (datalen == 0) return RESULT.OK;
        Sound sound = new Sound(soundPtr);
                
        sound.getUserData(out IntPtr userDataPtr);
                
        GCHandle handle = GCHandle.FromIntPtr(userDataPtr);
                
        PlayerBehaviour player = (PlayerBehaviour) handle.Target;
        if (player == null)
        {
            return RESULT.OK;
        }

        player.buffer.Dequeue(data, (int) (datalen / sizeof(short)));

        return RESULT.OK;
    }
    
    public static RESULT setpos(IntPtr sound, int sub, uint pos, TIMEUNIT postype)
    {
        return RESULT.OK;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        GamingConsole.OnGamingConsoleInteractChanged -= GamingConsoleInteractChanged;
        
        if (IChannel.HasValue)
        {
            IChannel.Value.stop();
            IChannel = null;
        }
        
        if (ISound.HasValue)
        {
            ISound.Value.release();
            ISound = null;
        }
        
        ModBehaviour.Instance.Enqueue(async UniTask () =>
        {
            if (mediaPlayer != null)
            {
                if (mediaPlayer.IsPlaying)
                {
                    mediaPlayer.Stop();
                }
                
                mediaPlayer.Dispose();
                mediaPlayer = null;
            }
            
            if (media != null)
            {
                media.Dispose();
                media = null;
            }
            
            fmtGCHandle.Free();
            thisHandle.Free();

            buffer.Dispose();
            
            GC.KeepAlive(this);
        }).Forget();
    }

    public bool ModeOfEnlargeIsStretch
    {
        get => Game.Console.Console.Variables.GetBool("mode_of_enlarge_is_stretch", false);
        set
        {
            Game.Console.Console.Variables.SetBool("mode_of_enlarge_is_stretch", value);
            RefreshSize();
        }
    }

    public bool IsMuteMachine
    {
        get => Game.Console.Console.Variables.GetBool("mute", false);
        set
        {
            Game.Console.Console.Variables.SetBool("mute", value);
            if (IChannelSub.HasValue) IChannelSub.Value.setMute(value);
            executionQueue.Enqueue(() =>
            {
                MuteRenderer.enabled = value;
                MuteRenderer2.enabled = value;
                SpeakerRenderer.material.mainTexture = IsMuteMachine ? TextureM : TextureS;
                SpeakerRenderer2.material.mainTexture = IsMuteMachine ? TextureM : TextureS;
            });
        }
    }

    public float VolumeOfMachine
    {
        get => Math.Clamp(Game.Console.Console.Variables.GetFloat("volume", 1f), 0f, 1f);
        set
        {
            Game.Console.Console.Variables.SetFloat("volume", Math.Clamp(value, 0, 1f));
            if (IChannelSub.HasValue) IChannelSub.Value.setVolume(Math.Clamp(value, 0, 1f));
            executionQueue.Enqueue(SetVolumeBar);
        }
    }

    public void RefreshSize()
    {
        if (_height == 0 | _width == 0 || ModeOfEnlargeIsStretch)
        {
            disp.localScale = DefaultSize;
            return;
        }

        var sw = _width / MAX_WIDTH;
        var sh = _height / MAX_HEIGHT;
        var s = Mathf.Max(sw, sh);

        disp.localScale = new Vector3(_width / s, -_height / s, 1f);
    }

    private bool _isTravaling = false;
    private bool _isPlayingBeforeTravel = false;
    
    protected void OnGUI()
    {
        if (ModBehaviour.Instance == null) return;
        Config cfg = ModBehaviour.Instance.Cfg;
        Event e = Event.current;
        if (e != null && e.isKey && e.type == UnityEngine.EventType.KeyDown)
        {
            Debug.Log($"Key: ${e.type} {e.keyCode}");
            if (e.keyCode == KeyCode.None) return;
            if (e.type == UnityEngine.EventType.KeyDown)
            {
                if (!ModBehaviour.Instance.IsOnGameConsole) return;
                if (e.keyCode == cfg.VolumeUp)
                {
                    VolumeOfMachine += .05f;
                    _ctsFadeOutVol?.Cancel();
                    VolShadowRenderer.enabled = true;
                    VolBackRenderer.enabled = true;
                    VolFrontRenderer.enabled = true;
                    VolSpeakerIconRenderer.enabled = true;
                    VolSpeakerIconShadowRenderer.enabled = true;
                    StartCoroutine(FadeOutVol(this.GetCancellationTokenOnDestroy()).ToCoroutine());
                    return;
                }
                if (e.keyCode == cfg.VolumeDown)
                {
                    VolumeOfMachine -= .05f;
                    _ctsFadeOutVol?.Cancel();
                    VolShadowRenderer.enabled = true;
                    VolBackRenderer.enabled = true;
                    VolFrontRenderer.enabled = true;
                    VolSpeakerIconRenderer.enabled = true;
                    VolSpeakerIconShadowRenderer.enabled = true;
                    StartCoroutine(FadeOutVol(this.GetCancellationTokenOnDestroy()).ToCoroutine());
                    return;
                }
                if (e.keyCode == cfg.SkipForward)
                {
                    if (mediaPlayer != null)
                    {
                        mediaPlayer.Time += 5000;
                        _ctsFadeOutBL?.Cancel();
                        TimeLineRenderer.enabled = true;
                        TimeLineRenderer2.enabled = true;
                        IndicatorRenderer.enabled = true;
                        IndicatorRenderer2.enabled = true;
                        StartCoroutine(FadeOutBaseLine(this.GetCancellationTokenOnDestroy()).ToCoroutine());
                    }
                    return;
                }
                if (e.keyCode == cfg.SkipBackward)
                {
                    if (mediaPlayer != null)
                    {
                        mediaPlayer.Time -= 5000;
                        _ctsFadeOutBL?.Cancel();
                        TimeLineRenderer.enabled = true;
                        TimeLineRenderer2.enabled = true;
                        IndicatorRenderer.enabled = true;
                        IndicatorRenderer2.enabled = true;
                        StartCoroutine(FadeOutBaseLine(this.GetCancellationTokenOnDestroy()).ToCoroutine());
                    }
                    return;
                }
                if (e.keyCode == cfg.Pause)
                {
                    mediaPlayer?.Pause();
                    return;
                }
                if (e.keyCode == cfg.ToStart)
                {
                    mediaPlayer?.Pause();
                    Replay();
                    return;
                }
                if (e.keyCode == cfg.Mute)
                {
                    IsMuteMachine = !IsMuteMachine;
                    _ctsFadeOutVol?.Cancel();
                    VolShadowRenderer.enabled = true;
                    VolBackRenderer.enabled = true;
                    VolFrontRenderer.enabled = true;
                    VolSpeakerIconRenderer.enabled = true;
                    VolSpeakerIconShadowRenderer.enabled = true;
                    StartCoroutine(FadeOutVol(this.GetCancellationTokenOnDestroy()).ToCoroutine());
                    return;
                }

                if (e.keyCode == cfg.SwitchStretch)
                {
                    ModeOfEnlargeIsStretch = !ModeOfEnlargeIsStretch;
                    return;
                }
            }
        }
    }
}