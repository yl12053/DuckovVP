using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Duckov;
using Duckov.UI;
using Duckov.UI.Animations;
using FeatherMod.Utils;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using SodaCraft.Localizations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuckovVP.Views;

public class BurnerView: View
{
    private InventoryDisplay[] inventories;
    private InventoryDisplay userInventory;
    private InventoryDisplay userStorage;

    public Item keySlotItem;
    public SlotDisplay registerSlotDisplay;
    public ItemDetailsDisplay detailsDisplay;
    public FadeGroup detailsFadeGroup;
    public Button doneButton;
    private Slot? KeySlot
    {
        get
        {
            if (keySlotItem == null) return null;
            if (keySlotItem.Slots == null) return null;
            return keySlotItem.Slots["CD"];
        }
    }
    
    private Item CharacterItem => LevelManager.Instance?.MainCharacter?.CharacterItem;

    public TMP_InputField inputField;
    
    protected override void Awake()
    {
        base.Awake();
        var invs = GetComponentsInChildren<InventoryDisplay>();
        if (invs == null)
        {
            Debug.LogError("No inv");
            return;
        }
        foreach (var invObj in invs)
        {
            if (invObj.gameObject.name.Equals("InventoryDisplay"))
            {
                userInventory = invObj;
            } else if (invObj.gameObject.name.Equals("InventoryDisplay_PlayerStorage"))
            {
                userStorage = invObj;
            }
        }
        // submitButton.onClick.AddListener(OnSubmitButtonClicked);
        // succeedIndicator.SkipHide();
        // detailsFadeGroup.SkipHide();
        registerSlotDisplay.onSlotDisplayDoubleClicked += OnSlotDoubleClicked;
        userInventory.onDisplayDoubleClicked += OnInventoryItemDoubleClicked;
        userStorage.onDisplayDoubleClicked += OnInventoryItemDoubleClicked;
    }

    private bool ShouldOperate(Item e)
    {
        if (e == null) return false;
        return KeySlot?.CanPlug(e) ?? false;
    }
    
    private bool CanOperate(Item e)
    {
        if (e == null) return true;
        return KeySlot?.CanPlug(e) ?? false;
    }
    
    protected override void OnOpen()
    {
        UnregisterEvents();
        base.OnOpen();
        Item character = CharacterItem;
        if (character == null)
        {
            Debug.LogError("Character item not exist");
            Close();
            return;
        }
        
        if (userInventory == null)
        {
            Close();
            return;
        }

        userInventory.ShowOperationButtons = false;
        userInventory.Setup(character.Inventory, ShouldOperate, CanOperate);
        if (PlayerStorage.Inventory != null)
        {
            userStorage.ShowOperationButtons = false;
            userStorage.gameObject.SetActive(true);
            userStorage.Setup(PlayerStorage.Inventory, ShouldOperate, CanOperate);
        }
        else
        {
            userStorage.gameObject.SetActive(false);
        }

        registerSlotDisplay.Setup(KeySlot);

        inputField.text = "";
        inputField.interactable = false;
        doneButton.interactable = false;
        RegisterEvents();
    }

    protected override void OnClose()
    {
        UnregisterEvents();
        detailsFadeGroup.Hide();
        base.OnClose();
        if (KeySlot != null && KeySlot.Content != null)
        {
            var content = KeySlot.Content;
            content.Detach();
            ItemUtilities.SendToPlayerCharacterInventory(content);
        }
    }
    
    private UnityAction<string> OnPathChanged;
    private UnityAction OnWriteDone; 
    private new void RegisterEvents()
    {
        KeySlot.onSlotContentChanged += OnSlotContentChanged;
        ItemUIUtilities.OnSelectionChanged += OnItemSelectionChanged;
        if (OnPathChanged == null) OnPathChanged = OnPathChangedMethod;
        inputField.onValueChanged.AddListener(OnPathChanged);
        if (OnWriteDone == null) OnWriteDone = OnWrite;
        doneButton.onClick.AddListener(OnWriteDone);
    }

    private new void UnregisterEvents()
    {
        KeySlot.onSlotContentChanged -= OnSlotContentChanged;
        ItemUIUtilities.OnSelectionChanged -= OnItemSelectionChanged;
        if (OnPathChanged != null) inputField.onValueChanged.RemoveListener(OnPathChanged);
        if (OnWriteDone != null) doneButton.onClick.RemoveListener(OnWriteDone);
    }
    
    private void OnPathChangedMethod(string path)
    {
        StartCoroutine(OnPathChangedAsync(path, this.GetCancellationTokenOnDestroy()).ToCoroutine());
    }
    

    private CancellationTokenSource? _pathCts;
    private async UniTask OnPathChangedAsync(string newPath, CancellationToken token)
    {
        _pathCts?.Cancel();
        CancellationTokenSource localCts = new(); 
        _pathCts = CancellationTokenSource.CreateLinkedTokenSource(token, localCts.Token);
        if (string.IsNullOrWhiteSpace(inputField.text))
        {
            doneButton.interactable = false;
            return;
        }

        try
        {
            if (Uri.TryCreate(newPath, UriKind.Absolute, out Uri? result))
            {
                if (result.IsFile)
                {
                    var localPath = result.LocalPath;
                    doneButton.interactable = await Task.Run(() => File.Exists(localPath), _pathCts.Token);
                    return;
                }
                doneButton.interactable = true;
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        doneButton.interactable = false;
    }
    
    private void OnInventoryItemDoubleClicked(InventoryDisplay display, InventoryEntry entry, PointerEventData data)
    {
        if (!entry.Editable)
        {
            return;
        }
        Item item = entry.Item;
        if (!(item == null) && (KeySlot?.CanPlug(item) ?? false))
        {
            item.Detach();
            KeySlot.Plug(item, out var unpluggedItem);
            if (unpluggedItem != null)
            {
                ItemUtilities.SendToPlayer(unpluggedItem);
            }
        }
    }
    
    private void OnSlotDoubleClicked(SlotDisplay display)
    {
        Item item = display.GetItem();
        if (!(item == null))
        {
            item.Detach();
            ItemUtilities.SendToPlayer(item);
        }
    }
    
    private void OnItemSelectionChanged()
    {
        if (ItemUIUtilities.SelectedItem != null)
        {
            detailsDisplay.Setup(ItemUIUtilities.SelectedItem);
            detailsFadeGroup.Show();
        }
        else
        {
            detailsFadeGroup.Hide();
        }
    }
    
    private void OnSlotContentChanged(Slot slot)
    {
        // HideSuccessIndication();
        if (slot?.Content != null)
        {
            inputField.interactable = true;
            var str = slot.Content.GetVariableEntry("Path")?.GetString();
            if (str == null || !str.StartsWith("DuckovVPRaw:"))
            {
                inputField.text = "";
            }
            else
            {
                inputField.text = str[12..];
            }
            OnPathChangedMethod(inputField.text);
            AudioManager.PlayPutItemSFX(slot.Content);
        }
        else
        {
            inputField.interactable = false;
            inputField.text = "";
            doneButton.interactable = false;
        }
    }

    private void OnWrite()
    {
        var item = KeySlot?.Content;
        if (item == null) return;
        item.GetVariableEntry("Path").SetString("DuckovVPRaw:" + inputField.text);
        NotificationText.Push("gui.duckovVP.successWrite".ToPlainText());
    }
}