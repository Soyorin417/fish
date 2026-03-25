using System;
using System.Collections.Generic;
using Game.Gacha;
using Game.Inventory;
using Game.Inventory.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour, IInventoryView
{
    [Header("Service")]
    [FormerlySerializedAs("inventoryManagerSource")]
    [SerializeField] private MonoBehaviour inventoryServiceSource;

    [Header("UI References")]
    public GameObject panelRoot;
    public Transform slotContainer;
    public GameObject slotPrefab;

    [Header("Info Panel")]
    public Image itemIcon;
    public TMP_Text itemNameText;
    public TMP_Text itemDescText;
    public TMP_Text itemAmountText;

    [Header("Buttons")]
    public Button closeButton;
    public Button useButton;
    public Button dropButton;
    public Button selectButton;

    [Header("Gacha")]
    public string gachaTicketItemId = "fish_025";
    public GameObject gachaPanelRoot;
    public GachaRollController gachaRollController;
    public bool closeInventoryWhenOpenGacha = true;

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private IInventoryService inventoryService;
    private InventoryToggle inventoryToggle;
    private string selectedItemId;
    private InventorySlotUI currentSelectedSlot;
    private Action<InventoryItem> selectionCallback;

    public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

    private void Start()
    {
        ResolveInventoryService();
        ResolveInventoryToggle();
        ResolveGachaController();

        if (gachaRollController != null)
        {
            gachaRollController.onRollCompleted -= HandleGachaRollCompleted;
            gachaRollController.onRollCompleted += HandleGachaRollCompleted;
        }

        if (useButton != null)
        {
            useButton.onClick.AddListener(UseSelectedItem);
        }

        if (dropButton != null)
        {
            dropButton.onClick.AddListener(DropSelectedItem);
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(ConfirmSelectedItemSelection);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(RequestClose);
        }

        if (inventoryService != null)
        {
            inventoryService.OnInventoryChanged += RefreshUI;
        }

        RefreshUI();
        ClearSelectionDetails();
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void RefreshUI()
    {
        if (!ResolveInventoryService())
        {
            return;
        }

        if (slotContainer == null)
        {
            Debug.LogError("InventoryUI slotContainer is not assigned.");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("InventoryUI slotPrefab is not assigned.");
            return;
        }

        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        slotUIs.Clear();
        currentSelectedSlot = null;

        foreach (InventoryItem item in inventoryService.Items)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            if (slotUI == null)
            {
                Debug.LogError("Inventory slot prefab is missing InventorySlotUI.");
                continue;
            }

            slotUI.SetData(item, SelectItem);
            slotUIs.Add(slotUI);

            if (!string.IsNullOrWhiteSpace(selectedItemId) &&
                item != null &&
                item.itemId == selectedItemId)
            {
                currentSelectedSlot = slotUI;
            }
        }

        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(true);
        }

        Debug.Log(
            "InventoryUI.RefreshUI entryCount=" + inventoryService.Items.Count +
            " callbackAssigned=" + (selectionCallback != null));

        SyncSelectionDetails();
    }

    public void SelectItem(InventorySlotUI slot, ItemDataRuntime itemData)
    {
        Debug.Log(
            "InventoryUI.SelectItem(ItemDataRuntime) slot=" + (slot != null ? slot.name : "(null)") +
            " itemId=" + (itemData != null ? itemData.itemId : "(null)"));

        if (itemData == null)
        {
            SelectItem(slot, (InventoryItem)null);
            return;
        }

        InventoryItem selectedItem = null;
        if (inventoryService != null && !string.IsNullOrWhiteSpace(itemData.itemId))
        {
            selectedItem = inventoryService.FindItem(itemData.itemId);
        }

        SelectItem(slot, selectedItem);
    }

    public void SelectItem(InventorySlotUI slot, InventoryItem item)
    {
        Debug.Log(
            "InventoryUI.SelectItem(InventoryItem) slot=" + (slot != null ? slot.name : "(null)") +
            " itemId=" + (item != null ? item.itemId : "(null)") +
            " callbackAssigned=" + (selectionCallback != null));

        if (currentSelectedSlot != null && currentSelectedSlot != slot)
        {
            currentSelectedSlot.SetSelected(false);
        }

        currentSelectedSlot = slot;
        selectedItemId = item != null ? item.itemId : null;

        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(true);
        }

        SyncSelectionDetails();

        if (selectionCallback == null)
        {
            Debug.LogWarning("InventoryUI.SelectItem has no external selectionCallback to notify.");
            return;
        }

        selectionCallback.Invoke(item);
    }

    public void ClearSelection()
    {
        selectedItemId = null;

        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(false);
            currentSelectedSlot = null;
        }

        ClearSelectionDetails();
    }

    public void SetSelectionCallback(Action<InventoryItem> callback)
    {
        selectionCallback = callback;
        Debug.Log("InventoryUI.SetSelectionCallback assigned=" + (callback != null));
    }

    public void ClearSelectionCallback()
    {
        selectionCallback = null;
        Debug.Log("InventoryUI.ClearSelectionCallback");
    }

    private void ClearSelectionDetails()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (itemNameText != null)
        {
            itemNameText.text = string.Empty;
        }

        if (itemDescText != null)
        {
            itemDescText.text = string.Empty;
        }

        if (itemAmountText != null)
        {
            itemAmountText.text = string.Empty;
        }
    }

    private void RequestClose()
    {
        ResolveInventoryToggle();
        if (inventoryToggle != null && inventoryToggle.IsOpen())
        {
            inventoryToggle.CloseInventory();
            return;
        }

        Hide();
    }

    private void UseSelectedItem()
    {
        InventoryItem selectedItem = FindSelectedItem();
        if (selectedItem == null || inventoryService == null)
        {
            Debug.LogWarning("InventoryUI.UseSelectedItem ignored because no item is selected.");
            return;
        }

        if (selectedItem.itemId == gachaTicketItemId)
        {
            TryUseGachaTicket(selectedItem);
            return;
        }

        inventoryService.RemoveItem(selectedItem.itemId, 1, ResolveStackable(selectedItem.itemId));
        Debug.Log("InventoryUI.UseSelectedItem consumed regular item itemId=" + selectedItem.itemId);
    }

    private void TryUseGachaTicket(InventoryItem selectedItem)
    {
        if (selectedItem == null || string.IsNullOrWhiteSpace(selectedItem.itemId))
        {
            Debug.LogWarning("InventoryUI.TryUseGachaTicket received an invalid selected item.");
            return;
        }

        if (selectedItem.amount <= 0 || inventoryService.GetItemCount(selectedItem.itemId) <= 0)
        {
            Debug.LogWarning("InventoryUI.TryUseGachaTicket found no ticket left for itemId=" + selectedItem.itemId);
            return;
        }

        ResolveGachaController();
        if (gachaRollController == null)
        {
            Debug.LogWarning("InventoryUI.TryUseGachaTicket aborted because gachaRollController is not assigned.");
            return;
        }

        GameObject panelToOpen = ResolveGachaPanelRoot();
        if (panelToOpen == null)
        {
            Debug.LogWarning("InventoryUI.TryUseGachaTicket could not find a GachaPanel root to open.");
            return;
        }

        bool removed = inventoryService.RemoveItem(selectedItem.itemId, 1, ResolveStackable(selectedItem.itemId));
        if (!removed)
        {
            Debug.LogWarning("InventoryUI.TryUseGachaTicket failed to consume ticket itemId=" + selectedItem.itemId);
            return;
        }

        Debug.Log("InventoryUI.TryUseGachaTicket consumed ticket itemId=" + selectedItem.itemId + " x1");

        if (closeInventoryWhenOpenGacha)
        {
            RequestClose();
        }
        else
        {
            Hide();
        }

        panelToOpen.SetActive(true);

        bool started = gachaRollController.PlayRandomRoll();
        if (!started)
        {
            inventoryService.AddItem(selectedItem.itemId, 1, ResolveStackable(selectedItem.itemId));
            Debug.LogWarning("InventoryUI.TryUseGachaTicket refunded ticket because gacha roll failed to start.");
        }
    }

    private void DropSelectedItem()
    {
        InventoryItem selectedItem = FindSelectedItem();
        if (selectedItem == null || inventoryService == null)
        {
            return;
        }

        inventoryService.RemoveItem(selectedItem.itemId, selectedItem.amount, ResolveStackable(selectedItem.itemId));
    }

    private void ConfirmSelectedItemSelection()
    {
        InventoryItem selectedItem = FindSelectedItem();
        if (selectedItem == null)
        {
            Debug.LogWarning("InventoryUI.ConfirmSelectedItemSelection has no selected item.");
            return;
        }

        Debug.Log(
            "InventoryUI.ConfirmSelectedItemSelection itemId=" + selectedItem.itemId +
            " callbackAssigned=" + (selectionCallback != null));

        selectionCallback?.Invoke(selectedItem);
    }

    private void HandleGachaRollCompleted(string resultFishId)
    {
        if (inventoryService == null) return;
        if (string.IsNullOrWhiteSpace(resultFishId)) return;
        if (resultFishId == gachaTicketItemId) return;

        bool added = inventoryService.AddItem(resultFishId, 1, ResolveStackable(resultFishId));
        if (!added)
        {
            Debug.LogWarning("Failed to add gacha reward: " + resultFishId);
            return;
        }

        Debug.Log("Added gacha reward: " + resultFishId);
        RefreshUI();
    }

    private InventoryItem FindSelectedItem()
    {
        if (inventoryService == null || string.IsNullOrWhiteSpace(selectedItemId))
        {
            return null;
        }

        return inventoryService.FindItem(selectedItemId);
    }

    private void SyncSelectionDetails()
    {
        InventoryItem selectedItem = FindSelectedItem();
        if (selectedItem == null || string.IsNullOrWhiteSpace(selectedItem.itemId))
        {
            ClearSelection();
            return;
        }

        ItemDataRuntime itemData = ItemDatabaseRuntime.FindById(selectedItem.itemId);
        if (itemData == null)
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }

            if (itemNameText != null)
            {
                itemNameText.text = selectedItem.itemId;
            }

            if (itemDescText != null)
            {
                itemDescText.text = "未找到对应 ItemDataRuntime 配置";
            }

            if (itemAmountText != null)
            {
                itemAmountText.text = "x" + selectedItem.amount;
            }

            return;
        }

        if (itemIcon != null)
        {
            Sprite icon = itemData.LoadIcon();
            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = itemData.itemName;
        }

        if (itemDescText != null)
        {
            itemDescText.text = itemData.description;
        }

        if (itemAmountText != null)
        {
            itemAmountText.text = "x" + selectedItem.amount;
        }
    }

    private bool ResolveInventoryService()
    {
        if (inventoryService != null)
        {
            return true;
        }

        inventoryService = inventoryServiceSource as IInventoryService;
        if (inventoryService == null && inventoryServiceSource != null)
        {
            Debug.LogError("inventoryServiceSource does not implement IInventoryService.");
        }

        if (inventoryService != null)
        {
            return true;
        }

        foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (behaviour is IInventoryService service)
            {
                inventoryServiceSource = behaviour;
                inventoryService = service;
                return true;
            }
        }

        Debug.LogError("InventoryUI could not resolve an IInventoryService in the scene.");
        return false;
    }

    private void ResolveInventoryToggle()
    {
        if (inventoryToggle == null)
        {
            inventoryToggle = FindObjectOfType<InventoryToggle>(true);
        }
    }

    private void ResolveGachaController()
    {
        if (gachaRollController == null)
        {
            gachaRollController = FindObjectOfType<GachaRollController>(true);
        }
    }

    private GameObject ResolveGachaPanelRoot()
    {
        if (gachaPanelRoot != null)
        {
            return gachaPanelRoot;
        }

        if (gachaRollController != null)
        {
            gachaPanelRoot = gachaRollController.gameObject;
        }

        return gachaPanelRoot;
    }

    private static bool ResolveStackable(string itemId)
    {
        ItemDataRuntime itemData = ItemDatabaseRuntime.FindById(itemId);
        return itemData == null || itemData.stackable;
    }

    private void SetVisible(bool visible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }
        else
        {
            gameObject.SetActive(visible);
        }

        if (!visible)
        {
            ClearSelection();
        }
    }

    private void OnDestroy()
    {
        if (gachaRollController != null)
        {
            gachaRollController.onRollCompleted -= HandleGachaRollCompleted;
        }

        if (useButton != null)
        {
            useButton.onClick.RemoveListener(UseSelectedItem);
        }

        if (dropButton != null)
        {
            dropButton.onClick.RemoveListener(DropSelectedItem);
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(ConfirmSelectedItemSelection);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(RequestClose);
        }

        if (inventoryService != null)
        {
            inventoryService.OnInventoryChanged -= RefreshUI;
        }
    }
}
