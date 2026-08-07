using System;
using System.Collections.Generic;
using Duckov.ItemBuilders;
using Duckov.MiniGames;
using Duckov.UI;
using Duckov.UI.Animations;
using Duckov.Utilities;
using FeatherMod.Items;
using ItemStatsSystem.Items;
using LeTai.Asset.TranslucentImage;
using LeTai.TrueShadow;
using SodaCraft.Localizations;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using Object = UnityEngine.Object;

namespace DuckovVP.Views;

public class ViewUtils
{
    public static BurnerView burnerView;

    public static GamingConsoleHUD? ExtraHUD = null;
    
    private static T? FindType<T>(string name) where T: Object
    {
        T? res = null;
        foreach (var tex in Resources.FindObjectsOfTypeAll<T>())
        {
            if (tex != null && tex.name == name)
            {
                res = tex;
                break;
            }
        }

        return res;
    }

    private static Sprite? _procedural;
    private static Sprite? procedural
    {
        get {
            if (_procedural == null)
            {
                _procedural = FindType<Sprite>("procedural_ui_image_default_sprite");
            }

            return _procedural;
        }
    }

    private static Material? _pui;

    private static Material? PuiMaterial
    {
        get
        {
            if (_pui == null)
            {
                _pui = FindType<Material>("UI/Procedural UI Image");
            }
            return _pui;
        }
    }

    public static TMP_InputField MakeInput(
        string name, 
        out RectTransform rect, 
        Transform? parent = null)
    {
        GameObject baseField = new(name);
        if (parent != null) baseField.transform.SetParent(parent, false);
        baseField.AddComponent<CanvasRenderer>();
        rect = baseField.AddComponent<RectTransform>();
        var field = baseField.AddComponent<TMP_InputField>();
        var proc = baseField.AddComponent<ProceduralImage>();
        proc.sprite = procedural;
        proc.material = PuiMaterial;
        
        var mod = baseField.AddComponent<UniformModifier>();
        mod.Radius = 14;
        proc.ModifierType = typeof(UniformModifier);

        var shadow = baseField.AddComponent<TrueShadow>();
        shadow.Size = 8;
        shadow.OffsetDistance = 3;
        shadow.Inset = true;
        shadow.ColorBleedMode = ColorBleedMode.Black;
        
        baseField.AddComponent<ButtonAnimation>();

        GameObject area = new("Text Area");
        area.transform.SetParent(baseField.transform, false);
        area.transform.localPosition = Vector3.zero;
        area.transform.localScale = Vector3.one;
        var areaRect = area.AddComponent<RectTransform>();
        areaRect.anchorMin = Vector2.zero;
        areaRect.anchorMax = Vector2.one;
        areaRect.sizeDelta = new(-20, -5);
        field.textViewport = areaRect;
        var areaMask = area.AddComponent<RectMask2D>();
        areaMask.padding = new(-8, -5, -8, -5);

        GameObject text = new("Text");
        text.transform.SetParent(area.transform, false);
        text.transform.localPosition = Vector3.zero;
        var textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.color = new Color(0x62 / 255f, 0x73 / 255f, 0x80 / 255f, 1);
        textComponent.fontSize = 18;
        textComponent.horizontalAlignment = HorizontalAlignmentOptions.Center;
        textComponent.verticalAlignment = VerticalAlignmentOptions.Top;
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        GameObject placeholder = new("Text");
        placeholder.transform.SetParent(area.transform, false);
        placeholder.transform.localPosition = Vector3.zero;
        var placeholderComponent = placeholder.AddComponent<TextMeshProUGUI>(); 
        placeholderComponent.color = new Color(0x62 / 255f, 0x73 / 255f, 0x80 / 255f, 1);
        placeholderComponent.fontSize = 18;
        placeholderComponent.horizontalAlignment = HorizontalAlignmentOptions.Center;
        placeholderComponent.verticalAlignment = VerticalAlignmentOptions.Top;
        var placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        
        field.textComponent = textComponent;
        field.placeholder = placeholderComponent;

        return field;
    }

    private static InputActionMap _map;
    private static Dictionary<string, InputActionReference> refMap = new();
    public static InputActionMap map {
        get
        {
            if (_map == null)
            {
                var assets = PlayerInput.all[0].actions;
                assets.Disable();
                try
                {
                    _map = new("DuckovVP");
                    assets.AddActionMap(_map);
                }
                finally
                {
                    assets.Enable();
                }
            }

            return _map;
        }
    }
    public static InputActionReference CreateKeyActionReference(string name, KeyCode targetKey)
    {
        InputActionReference actionReference;
        map.asset.Disable();
        try
        {
            var action = map.AddAction(name, InputActionType.Button);
            var bindingPath = ConversionUtils.KeyCodeToKey(targetKey);
            action.AddBinding(bindingPath, groups: "Keyboard&Mouse");

            map.Enable();
            actionReference = InputActionReference.Create(action);

            Debug.Log(
                $"Creating: {actionReference.action.bindings[0].ToDisplayString(out _, out _, InputBinding.DisplayStringOptions.IgnoreBindingOverrides)}");
        }
        finally
        {
            map.asset.Enable();
        }

        return actionReference;
    }

    public static string? OverrideDisplay(string keyCode)
    {
        switch (keyCode)
        {
            case "Left Arrow": return "←";
            case "Right Arrow": return "→";
            case "Up Arrow": return "↑";
            case "Down Arrow": return "↓";
            case "Home": return "Home";
        }

        return null;
    }
    
    public static void ViewsInit()
    {
        var tsrc = LevelManager.Instance.GameCamera.GetComponentInChildren<TranslucentImageSource>();
        if (tsrc == null)
        {
            Debug.LogError("No TSRC Found!");
        }

        var sprite = FindType<Sprite>("Frame_Basic_Rectangle");
        if (sprite == null)
        {
            Debug.LogError("Sprite load failed!");
        }
        
        Sprite? upframe = FindType<Sprite>("UpFrameMask16");
        if (upframe == null)
        {
            Debug.LogError("UpFrame load failed!");
        }
        
        Sprite? downframe = FindType<Sprite>("DownFrameMask16");
        if (downframe == null)
        {
            Debug.LogError("DownFrame load failed!");
        }

        Material? material = FindType<Material>("Default-Translucent");
        if (material == null)
        {
            Debug.LogError("No material found");
        }

        var canvas = LevelManager.Instance.transform.Find("GameplayUICanvas");
        
        var GamingConsoleHUDCopy = Object.Instantiate(GamingConsoleHUD.Instance, canvas, false);
        ExtraHUD = GamingConsoleHUDCopy;
        var HUDRect = GamingConsoleHUDCopy.GetComponent<RectTransform>();
        HUDRect.offsetMax = HUDRect.offsetMin = Vector2.zero;
        var HUDContent = GamingConsoleHUDCopy.transform.Find("Content");
        Object.DestroyImmediate(HUDContent.Find("Start").gameObject);
        Object.DestroyImmediate(HUDContent.Find("Select").gameObject);
        Object.DestroyImmediate(HUDContent.Find("A").gameObject);
        Object.DestroyImmediate(HUDContent.Find("B").gameObject);
        var baseOnCopy = HUDContent.Find("Axis");
        var wsad = baseOnCopy.Find("WSAD");
        for (int i = 1; i <= 3; i++)
        {
            Object.DestroyImmediate(wsad.Find($"InputIndicator_{i}").gameObject);
        }

        var IndicatorTemplate = Object.Instantiate(wsad.Find("InputIndicator").GetComponent<InputIndicator>(), null);
        Object.DestroyImmediate(wsad.Find("InputIndicator").gameObject);

        void Create(string name, params (string, KeyCode, Func<Action<KeyCode>, Action>)[] keys)
        {
            var lr = Object.Instantiate(baseOnCopy, HUDContent, false);
            var lrbase = lr.Find("WSAD");
            foreach (var key in keys)
            {
                var elem = Object.Instantiate(IndicatorTemplate, lrbase, false);
                var distList = elem.AddComponent<DestroyListener>();
                distList.Destroy = () => { };
                if (!refMap.TryGetValue(key.Item1, out var reference))
                {
                    reference = CreateKeyActionReference(key.Item1, key.Item2);
                    var actionOnDest = key.Item3(key1 =>
                    {
                        var action = reference.action;
                        action.Disable();
                        try
                        {
                            action.Reset();
                            action.AddBinding(ConversionUtils.KeyCodeToKey(key1), groups: "Keyboard&Mouse");
                            elem.Refresh();
                        }
                        finally
                        {
                            action.Enable();
                        }
                    });
                    distList.Destroy += actionOnDest;
                    refMap.Add(key.Item1, reference);
                }

                Action<InputIndicator> onAfterRefreshIndi = (elems) =>
                {
                    if (elems == elem)
                    {
                        var rep = OverrideDisplay(elems.text.text);
                        if (rep != null)
                        {
                            elems.text.text = rep;
                            elems.ShowText();
                        }
                    }
                };
                InputIndicator.OnAfterRefresh += onAfterRefreshIndi;
                distList.Destroy += () => InputIndicator.OnAfterRefresh -= onAfterRefreshIndi;
                elem.Setup(reference, 0);
            }
            var textComp = lr.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
            var loc = textComp.AddComponent<TextLocalizor>();
            loc.Key = name;
            loc.tmpText = textComp;
        }

        (string, KeyCode, Func<Action<KeyCode>, Action>) CreateTuple(string name, KeyCode value)
        {
            Func<Action<KeyCode>, Action> func = action =>
            {
                Action<string, KeyCode> wrapEvent = (key, code) =>
                {
                    if (key == name)
                    {
                        action(code);
                    }
                };
                ModBehaviour.Instance.Cfg.Announce += wrapEvent;
                return () => ModBehaviour.Instance.Cfg.Announce -= wrapEvent;
            };
            return (name, value, func);
        }
        
        Create("gui.duckovVP.seek", 
            CreateTuple("SkipBackward", ModBehaviour.Instance.Cfg.SkipBackward),
            CreateTuple("SkipForward", ModBehaviour.Instance.Cfg.SkipForward)
        );
        Create("gui.duckovVP.vol", 
            CreateTuple("VolumeUp", ModBehaviour.Instance.Cfg.VolumeUp), 
            CreateTuple("VolumeDown", ModBehaviour.Instance.Cfg.VolumeDown)
        );
        Create("gui.duckovVP.pause", 
            CreateTuple("Pause", ModBehaviour.Instance.Cfg.Pause) 
        );
        Create("gui.duckovVP.replay", 
            CreateTuple("ToStart", ModBehaviour.Instance.Cfg.ToStart) 
        );
        Create("gui.duckovVP.mute", 
            CreateTuple("Mute", ModBehaviour.Instance.Cfg.Mute) 
        );
        Create("gui.duckovVP.mode", 
            CreateTuple("SwitchStretch", ModBehaviour.Instance.Cfg.SwitchStretch) 
        );
        
        Object.DestroyImmediate(IndicatorTemplate.gameObject);
        Object.DestroyImmediate(baseOnCopy.gameObject);
        
        var baseView = new GameObject("DuckovVP:Burner");
        baseView.SetActive(false);
        baseView.layer = 5;
        baseView.transform.SetParent(canvas, false);
        baseView.transform.localPosition = Vector3.zero;
        var baseRt = baseView.AddComponent<RectTransform>();
        baseRt.anchorMin = Vector2.zero;
        baseRt.anchorMax = Vector2.one;
        baseRt.offsetMin = Vector2.zero;
        baseRt.offsetMax = Vector2.zero;
        baseRt.sizeDelta = Vector2.zero;
        baseView.AddComponent<CanvasRenderer>();

        var contentElementGlob = new GameObject("Content");
        contentElementGlob.transform.SetParent(baseView.transform, false);
        contentElementGlob.transform.localPosition = new(0, 45, 0);
        var cntRt = contentElementGlob.AddComponent<RectTransform>();
        cntRt.anchorMin = Vector2.zero;
        cntRt.anchorMax = Vector2.one;
        cntRt.offsetMin = new(80, 120);
        cntRt.offsetMax = new(-80, -30);
        cntRt.sizeDelta = new(-160, -150);
        cntRt.AddComponent<CanvasGroup>();

        var invElement = new GameObject("EquipmentAndInventory");
        invElement.transform.SetParent(contentElementGlob.transform, false);
        invElement.transform.localPosition = new(-1200, 0, 0);
        var invRect = invElement.AddComponent<RectTransform>();
        invRect.anchorMin = Vector2.zero;
        invRect.anchorMax = new(0, 1);
        invRect.offsetMin = Vector2.zero;
        invRect.offsetMax = new(640, 0);
        invRect.sizeDelta = new(640, 0);

        var baseTitle = canvas.Find("MasterKeysRegisterView").Find("Content").Find("EquipmentAndInventory").Find("Title");
        var title = Object.Instantiate(baseTitle.gameObject, invElement.transform, false);
        var trect = title.GetComponent<RectTransform>();
        trect.offsetMin = new(5, -74);
        trect.offsetMax = new(-5, 0);
        var exitBtn = title.transform.Find("ButtonContainer").Find("ExitButton").gameObject.GetComponent<Button>();
        
        var contentElement = new GameObject("Content");
        contentElement.transform.SetParent(invElement.transform, false);
        contentElement.transform.localPosition = new(5, 561, 0);
        contentElement.AddComponent<CanvasRenderer>();
        var cntLrt = contentElement.AddComponent<RectTransform>();
        cntLrt.anchorMin = Vector2.zero;
        cntLrt.anchorMax = new(0, 1);
        cntLrt.offsetMin = new(5, 0);
        cntLrt.offsetMax = new(645, -84);
        cntLrt.sizeDelta = new(640, -84);
        
        var translucentImage = contentElement.AddComponent<TranslucentImage>();
        translucentImage.source = tsrc;
        translucentImage.sprite = sprite;
        translucentImage.material = material;
        translucentImage.spriteBlending = 0.2f;
        translucentImage.type = Image.Type.Sliced;
        translucentImage.color = new Color(0x32 / 255f, 0x59 / 255f, 0x78 / 255f, 1f);

        var bases = canvas.Find("MasterKeysRegisterView").Find("Content").Find("EquipmentAndInventory").Find("Content")
            .Find("Scroll View");
        Object.Instantiate(bases.gameObject, contentElement.transform, false);

        var itemHolder = new GameObject("KeySlotItem");
        itemHolder.transform.SetParent(baseView.transform, false);
        itemHolder.AddComponent<RectTransform>();

        var dummyItem = ItemBuilder.New()
            .Slot("CD",
                FeatherMod.ItemUtils.GetTargetTag(TagLookup.GetNativeMayNotExist(new(ModBehaviour.MODID, "CD"))))
            .Instantiate();
        dummyItem.gameObject.transform.SetParent(itemHolder.transform);

        var basesInteract = canvas.Find("MasterKeysRegisterView").Find("Content").Find("InteractionPanel");
        var interactPanel = Object.Instantiate(basesInteract.gameObject, contentElementGlob.transform, false);
        interactPanel.transform.localPosition = new(1200, -44.36f, 0);
        interactPanel.transform.Find("Name").GetComponentInChildren<TextLocalizor>().Key = "gui.duckovVP.burner";
        interactPanel.transform.Find("SlotDisplay").GetComponentInChildren<SlotDisplay>().Target =
            dummyItem.Slots["CD"];
        var btnDone = interactPanel.transform.Find("BtnContainer").GetComponentInChildren<Button>();
        var btnContainer = btnDone.gameObject;
        btnContainer.AddComponent<ButtonAnimation>();
        
        var interactRect = interactPanel.GetComponent<RectTransform>();
        interactRect.anchorMin = new(1, 0);
        interactRect.anchorMax = Vector2.one;
        interactRect.offsetMin = new(-578, 0);
        interactRect.offsetMax = new(0, -88.72f);
        
        Object.DestroyImmediate(interactPanel.transform.Find("SlotDisplay").Find("ItemDisplayContainer").Find("RecordExistIndicator").gameObject);

        var inputField = MakeInput("Field", out var fieldRect, interactPanel.transform);
        fieldRect.sizeDelta = new(550, 30);
        fieldRect.anchoredPosition -= new Vector2(0, 80);
        var detailPanelBase = canvas.Find("MasterKeysRegisterView").Find("Content").Find("ItemDetails");
        var detailPanel = Object.Instantiate(detailPanelBase.gameObject, contentElementGlob.transform, false);

        var fieldDesc = new GameObject("FieldDesc");
        fieldDesc.transform.SetParent(interactPanel.transform, false);
        var rectDesc = fieldDesc.AddComponent<RectTransform>();
        rectDesc.sizeDelta = new(0, 0);
        rectDesc.anchoredPosition = new(0, -100);
        var textDesc = fieldDesc.AddComponent<TextMeshProUGUI>();
        textDesc.color = new(0xf8 / 255f, 0xf2 / 255f, 0xea / 255f, 1f);
        textDesc.horizontalAlignment = HorizontalAlignmentOptions.Center;
        textDesc.verticalAlignment = VerticalAlignmentOptions.Top;
        textDesc.fontSize = 14.7f;
        textDesc.fontStyle = FontStyles.Bold;
        var fieldTranslator = fieldDesc.AddComponent<TextLocalizor>();
        fieldTranslator.tmpText = textDesc;
        fieldTranslator.Key = "gui.duckovVP.path";
        
        var viewBehaviour = baseView.AddComponent<BurnerView>();
        viewBehaviour.viewTabs = null;
        viewBehaviour.exitButton = exitBtn;
        viewBehaviour.keySlotItem = dummyItem;
        viewBehaviour.registerSlotDisplay = interactPanel.GetComponentInChildren<SlotDisplay>();
        viewBehaviour.detailsDisplay = detailPanel.GetComponent<ItemDetailsDisplay>();
        viewBehaviour.detailsFadeGroup = detailPanel.GetComponent<FadeGroup>();
        viewBehaviour.inputField = inputField;
        viewBehaviour.doneButton = btnDone;
        
        baseView.transform.SetAsFirstSibling();
        burnerView = viewBehaviour;
        ManagedUIElement.onClose += HandleClose;
        baseView.SetActive(true);
    }
    
    private static void HandleClose(ManagedUIElement e)
    {
        if (e != burnerView) return;
        e.gameObject.SetActive(false);
    }
}