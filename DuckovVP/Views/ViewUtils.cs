using Duckov.ItemBuilders;
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
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace DuckovVP.Views;

public class ViewUtils
{
    public static BurnerView burnerView;

    private static T? FindType<T>(string name) where T: UnityEngine.Object
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
        // var slotCollection = itemHolder.AddComponent<SlotCollection>();

        var dummyItem = ItemBuilder.New()
            .Slot("CD",
                FeatherMod.ItemUtils.GetTargetTag(TagLookup.GetNativeMayNotExist(new(ModBehaviour.MODID, "CD"))))
            .Instantiate();
        dummyItem.gameObject.transform.SetParent(itemHolder.transform);
        // slotCollection.Add(dummyItem.Slots["CD"]);
        // dummyItem.Slots["CD"].collection = slotCollection;

        var basesInteract = canvas.Find("MasterKeysRegisterView").Find("Content").Find("InteractionPanel");
        var interactPanel = Object.Instantiate(basesInteract.gameObject, contentElementGlob.transform, false);
        interactPanel.transform.localPosition = new(1200, -44.36f, 0);
        interactPanel.transform.Find("Name").GetComponentInChildren<TextLocalizor>().Key = "gui.duckovVP.burner";
        interactPanel.transform.Find("SlotDisplay").GetComponentInChildren<SlotDisplay>().Target =
            dummyItem.Slots["CD"];
        var btnDone = interactPanel.transform.Find("BtnContainer").GetComponentInChildren<Button>();
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
        textDesc.text = "abcd";
        textDesc.color = new(0xf8 / 255f, 0xf2 / 255f, 0xea / 255f, 1f);
        textDesc.horizontalAlignment = HorizontalAlignmentOptions.Center;
        textDesc.verticalAlignment = VerticalAlignmentOptions.Top;
        textDesc.fontSize = 14.7f;
        textDesc.fontStyle = FontStyles.Bold;
        
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