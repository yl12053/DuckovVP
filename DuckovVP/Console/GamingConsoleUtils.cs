using System.IO;
using System.Reflection;
using Duckov.MiniGames;
using FeatherMod.Minigame;
using FeatherMod.Utils;
using ItemStatsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuckovVP.Console;

public static class GamingConsoleUtils
{
    private static readonly Item EmptyItem;
    private static readonly Item FilledItem;

    static GamingConsoleUtils()
    {
        var obj = new GameObject($"{ModBehaviour.MODID}_CustomRoot_FakeCartridge");
        obj.SetActive(false);
        Object.DontDestroyOnLoad(obj);
        EmptyItem = obj.AddComponent<Item>();
        FilledItem = obj.AddComponent<Item>();
    }
    
    public static string GetCartridgeGameID(Item item)
    {
        var CD = item.Slots["cd"].Content;
        Debug.Log($"CD is null? {CD == null}");
        return new Identifier(ModBehaviour.MODID, CD == null ? "empty" : "hascd").ToString();
    }

    public static Item GetFakeCartridge(Item item)
    {
        var CD = item.Slots["cd"].Content;
        return CD == null ? EmptyItem : FilledItem;
    }

    public static void Init()
    {
        InitScreenSaver();
        InitPlayer();
    }
    
    public static void InitScreenSaver()
    {
        Texture2D texture = new(480, 217);
        texture.LoadImage(
            File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "assets/gui/dvd.png")));
        
        var minigameBase = MinigameUtil.NewMinigameBase(new Identifier(ModBehaviour.MODID, "empty"), out var camera, out var ui);
        minigameBase.GetComponent<MiniGame>().tickTiming = MiniGame.TickTiming.Update;
        camera.orthographic = true;
        camera.orthographicSize = 160f;
        camera.cullingMask = -1;

        ui.clearFlags = CameraClearFlags.Nothing;

        var icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        icon.layer = 30;
        icon.name = "icon";
        icon.transform.SetParent(minigameBase.transform, false);
        icon.transform.localScale = new Vector3(96f, 217f / 5f, 1f);

        var renderer = icon.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("UI/Default"));
        renderer.material.mainTexture = texture;
        Object.DestroyImmediate(icon.GetComponent<Collider>());

        PhysicsMaterial2D bounceMat = new PhysicsMaterial2D("DVD_Bounce");
        bounceMat.bounciness = 1f;
        bounceMat.friction = 0f;
        
        var collider = icon.AddComponent<BoxCollider2D>();
        collider.sharedMaterial = bounceMat;

        var rb = icon.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.drag = 0;
        rb.angularDrag = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        GameObject boundary = new GameObject("boundary");
        boundary.layer = 30;
        boundary.transform.SetParent(minigameBase.transform, false);
        EdgeCollider2D edgeCol = boundary.AddComponent<EdgeCollider2D>();

        Vector2 bottomLeft = new(-213, -159.7f);
        Vector2 topLeft = new(-213, 159.7f);
        Vector2 topRight = new(213, 159.7f);
        Vector2 bottomRight = new(213f, -159.7f);
        edgeCol.points = new[]
        {
            bottomLeft,
            topLeft,
            topRight,
            bottomRight,
            bottomLeft
        };

        icon.AddComponent<IconBehaviour>();
        
        MinigameUtil.RegisterMinigame(new Identifier(ModBehaviour.MODID, "empty"), minigameBase);
    }

    public static void InitPlayer()
    {
        Texture2D texture = new(1280, 960);
        texture.LoadImage(
            File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "assets/gui/PM5544.png")));
        
        Texture2D textureP = new(1024, 1024);
        textureP.LoadImage(
            File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "assets/gui/pause.png")));
        
        Texture2D textureM = new(512, 512);
        textureM.LoadImage(
            File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "assets/gui/mute.png")));

        Texture2D textureB = new(900, 563);
        textureB.LoadImage(
            File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "assets/gui/bg.jpg")));
        
        Texture2D textureA = new(97, 97);
        textureA.LoadImage(
            File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "assets/gui/album.png")));
        
        var minigameBase = MinigameUtil.NewMinigameBase(new Identifier(ModBehaviour.MODID, "hascd"), out var camera, out var ui);
        minigameBase.GetComponent<MiniGame>().tickTiming = MiniGame.TickTiming.Update;
        camera.orthographic = true;
        camera.orthographicSize = 160f;
        camera.cullingMask = -1;
        
        ui.clearFlags = CameraClearFlags.Nothing;
        
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.layer = 30;
        bg.name = "bg";
        bg.transform.SetParent(minigameBase.transform, false);
        bg.transform.localScale = new Vector3(427f, 316.5f, 1f);
        bg.transform.localPosition = new Vector3(0, 2, 6);
        var brenderer = bg.GetComponent<Renderer>();
        brenderer.material = new Material(Shader.Find("UI/Default"));
        brenderer.material.mainTexture = textureB;
        Object.DestroyImmediate(bg.GetComponent<Collider>());

        var album = GameObject.CreatePrimitive(PrimitiveType.Quad);
        album.layer = 30;
        album.name = "album";
        album.transform.SetParent(minigameBase.transform, false);
        album.transform.localScale = new Vector3(120f, 120f, 1f);
        album.transform.localPosition = new Vector3(-110f, 30f, 5.9f);
        var arenderer = album.GetComponent<Renderer>();
        arenderer.material = new Material(Shader.Find("UI/Default"));
        arenderer.material.mainTexture = textureA;
        Object.DestroyImmediate(album.GetComponent<Collider>());
        
        var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.layer = 30;
        screen.name = "screen";
        screen.transform.SetParent(minigameBase.transform, false);
        screen.transform.localScale = new Vector3(427f, -316.5f, 1f);
        screen.transform.localPosition = new Vector3(0, 2, 5);
        var renderer = screen.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("UI/Default"));
        
        var pm5544 = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pm5544.layer = 30;
        pm5544.name = "pm5544";
        pm5544.transform.SetParent(minigameBase.transform, false);
        pm5544.transform.localScale = new Vector3(427f, -316.5f, 1f);
        pm5544.transform.localPosition = new Vector3(0, 2, 4);
        var rendererpm = pm5544.GetComponent<Renderer>();
        rendererpm.material = new Material(Shader.Find("UI/Default"));
        rendererpm.material.mainTexture = texture;
        Object.DestroyImmediate(pm5544.GetComponent<Collider>());

        var pause = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pause.layer = 30;
        pause.name = "pause";
        pause.transform.SetParent(minigameBase.transform, false);
        pause.transform.localScale = new Vector3(80, 80, 1);
        pause.transform.localPosition = new Vector3(0, 0, 4.8f);
        var rendererpause = pause.GetComponent<Renderer>();
        rendererpause.material = new Material(Shader.Find("UI/Default"));
        rendererpause.material.mainTexture = textureP;
        rendererpause.enabled = false;
        Object.DestroyImmediate(pause.GetComponent<Collider>());

        var pause2 = Object.Instantiate(pause, minigameBase.transform, false);
        pause2.name = "pause2";
        pause2.transform.localPosition += new Vector3(2, -2, 0.1f);
        var rendererpause2 = pause2.GetComponent<Renderer>();
        rendererpause2.material = new Material(Shader.Find("UI/Default"));
        rendererpause2.material.mainTexture = textureP;
        rendererpause2.enabled = false;
        rendererpause2.material.color = Color.black;

        var baseLine = GameObject.CreatePrimitive(PrimitiveType.Quad);
        baseLine.layer = 30;
        baseLine.name = "baseLine";
        baseLine.transform.SetParent(minigameBase.transform, false);
        baseLine.transform.localScale = new Vector3(427f * 0.8f, 2f, 2f);
        baseLine.transform.localPosition = new Vector3(0f, -130f, 4.6f);
        var renderBL = baseLine.GetComponent<Renderer>();
        renderBL.material = new Material(Shader.Find("UI/Default"));
        renderBL.enabled = false;
        Object.DestroyImmediate(baseLine.GetComponent<Collider>());
        var bl2 = Object.Instantiate(baseLine, minigameBase.transform, false);
        bl2.transform.localPosition += new Vector3(1, -1, +0.1f);
        bl2.name = "baseLineShadow";
        var renderBL2 = bl2.GetComponent<Renderer>();
        renderBL2.material = new Material(Shader.Find("UI/Default"));
        renderBL2.material.color = Color.black;
        var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.layer = 30;
        indicator.name = "indicator";
        indicator.transform.SetParent(minigameBase.transform, false);
        indicator.transform.localScale = new Vector3(5f, 5f, 1f);
        indicator.transform.localPosition = new Vector3(0f, -130f, 4.4f);
        var renderInd = indicator.GetComponent<Renderer>();
        renderInd.material = new Material(Shader.Find("UI/Default"));
        renderInd.enabled = false;
        Object.DestroyImmediate(indicator.GetComponent<Collider>());
        var indicator2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator2.layer = 30;
        indicator2.name = "indicatorShadow";
        indicator2.transform.SetParent(indicator.transform, false);
        indicator2.transform.localScale = new Vector3(1f, 1f, 1f);
        indicator2.transform.localPosition = new Vector3(0.2f, -0.2f, 0.1f);
        var renderINs = indicator2.GetComponent<Renderer>();
        renderINs.material = new Material(Shader.Find("UI/Default"));
        renderINs.material.color = Color.black;
        renderINs.enabled = false;
        Object.DestroyImmediate(indicator2.GetComponent<Collider>());

        var muteIcon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        muteIcon.layer = 30;
        muteIcon.name = "mute";
        muteIcon.transform.SetParent(minigameBase.transform, false);
        muteIcon.transform.localScale = new Vector3(30, 30, 1);
        muteIcon.transform.localPosition = new Vector3(180f, 130f, 4.8f);
        var renderMute = muteIcon.GetComponent<Renderer>();
        renderMute.material = new Material(Shader.Find("UI/Default"));
        renderMute.material.mainTexture = textureM;
        renderMute.enabled = false;
        Object.DestroyImmediate(renderMute.GetComponent<Collider>());
        var muteIcon2 = Object.Instantiate(muteIcon, minigameBase.transform, false);
        muteIcon2.name = "mute2";
        muteIcon2.transform.localPosition += new Vector3(2, -2, 0.1f);
        var renderMute2 = muteIcon2.GetComponent<Renderer>();
        renderMute2.material = new Material(Shader.Find("UI/Default"));
        renderMute2.material.mainTexture = textureM;
        renderMute2.material.color = Color.black;
        renderMute2.enabled = false;

        var volBarBack = GameObject.CreatePrimitive(PrimitiveType.Quad);
        volBarBack.layer = 30;
        volBarBack.name = "volBack";
        volBarBack.transform.SetParent(minigameBase.transform, false);
        volBarBack.transform.localScale = new Vector3(10f, 120f, 1);
        volBarBack.transform.localPosition = new Vector3(180f, 25f, 4.7f);
        var renderVolBarBack = volBarBack.GetComponent<Renderer>();
        renderVolBarBack.material = new Material(Shader.Find("UI/Default"));
        renderVolBarBack.material.color = Color.gray;
        renderVolBarBack.enabled = false;
        Object.DestroyImmediate(volBarBack.GetComponent<Collider>());
        var volBarShadow = Object.Instantiate(volBarBack, minigameBase.transform, false);
        volBarShadow.name = "volShadow";
        volBarShadow.transform.localPosition += new Vector3(2f, -2f, 0.1f);
        var renderVolBarShadow = volBarShadow.GetComponent<Renderer>();
        renderVolBarShadow.material = new Material(Shader.Find("UI/Default"));
        renderVolBarShadow.material.color = Color.black;
        var volBarFront = Object.Instantiate(volBarBack, minigameBase.transform, false);
        volBarFront.name = "volBar";
        volBarFront.transform.localPosition += new Vector3(0f, 0f, -0.1f);
        var renderVolBarFront = volBarFront.GetComponent<Renderer>();
        renderVolBarFront.material = new Material(Shader.Find("UI/Default"));
        renderVolBarFront.material.color = Color.white;

        var volSpeakerIcon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        volSpeakerIcon.layer = 30;
        volSpeakerIcon.name = "speakerIcon";
        volSpeakerIcon.transform.SetParent(minigameBase.transform, false);
        volSpeakerIcon.transform.localScale = new Vector3(30f, 30f, 1f);
        volSpeakerIcon.transform.localPosition = new Vector3(180f, -50f, 4.7f);
        var renderSpeakerIcon = volSpeakerIcon.GetComponent<Renderer>();
        renderSpeakerIcon.material = new Material(Shader.Find("UI/Default"));
        renderSpeakerIcon.enabled = false;
        Object.DestroyImmediate(volSpeakerIcon.GetComponent<Collider>());
        var volSpeakerShadow = Object.Instantiate(volSpeakerIcon, minigameBase.transform, false);
        volSpeakerShadow.name = "speakerShadow";
        volSpeakerShadow.transform.localPosition += new Vector3(2, -2, 0.1f);
        var renderSpeakerShadow = volSpeakerShadow.GetComponent<Renderer>();
        renderSpeakerShadow.material = new Material(Shader.Find("UI/Default"));
        renderSpeakerShadow.material.color = Color.black;
        
        minigameBase.AddComponent<PlayerBehaviour>();
        
        GameObject textObj = new GameObject("FName");
        textObj.transform.SetParent(minigameBase.transform, false);
        textObj.layer = 30;
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.fontSize = 250;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.havePropertiesChanged = true;
        tmp.ForceMeshUpdate();
        textObj.transform.localScale = new Vector3(1f, 1f, 1);
        var rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.sizeDelta = new(240f, 250f);
        textObj.transform.position = new Vector3(80f, 80f, 5.6f);
        textObj.AddComponent<TextScroller>();
        
        GameObject textObj2 = new GameObject("AName");
        textObj2.transform.SetParent(minigameBase.transform, false);
        textObj2.layer = 30;
        TextMeshPro tmpa = textObj2.AddComponent<TextMeshPro>();
        tmpa.fontSize = 200;
        tmpa.alignment = TextAlignmentOptions.Left;
        tmpa.havePropertiesChanged = true;
        tmpa.ForceMeshUpdate();
        textObj2.transform.localScale = new Vector3(1f, 1f, 1);
        var recta = textObj2.GetComponent<RectTransform>();
        recta.anchorMin = Vector2.zero;
        recta.anchorMax = Vector2.zero;
        recta.sizeDelta = new(240f, 250f);
        textObj2.transform.position = new Vector3(80f, 50f, 5.6f);
        textObj2.AddComponent<TextScroller>();
        
        MinigameUtil.RegisterMinigame(new Identifier(ModBehaviour.MODID, "hascd"), minigameBase);
    }
}