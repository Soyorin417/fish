using System;
using System.Collections.Generic;
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

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private IInventoryService inventoryService;
    private ItemData selectedItemData;
    private InventorySlotUI currentSelectedSlot;
    private Action<InventoryItem> selectionCallback;

    public bool IsVisible => panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;

    private void Start()
    {
        ResolveInventoryService();

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

            if (selectedItemData != null && item != null && item.itemData == selectedItemData)
            {
                currentSelectedSlot = slotUI;
            }
        }

        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(true);
        }

        SyncSelectionDetails();
    }

    public void SelectItem(InventorySlotUI slot, ItemData itemData)
    {
        InventoryItem selectedItem = slot != null ? slot.CurrentItem : null;
        SelectItem(slot, selectedItem);
    }

    public void SelectItem(InventorySlotUI slot, InventoryItem item)
    {
        if (currentSelectedSlot != null && currentSelectedSlot != slot)
        {
            currentSelectedSlot.SetSelected(false);
        }

        currentSelectedSlot = slot;
        selectedItemData = item != null ? item.itemData : null;

        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(true);
        }

        SyncSelectionDetails();
        selectionCallback?.Invoke(item);
    }

    public void ClearSelection()
    {
        selectedItemData = null;

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
    }

    public void ClearSelectionCallback()
    {
        selectionCallback = null;
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

    private void UseSelectedItem()
    {
        if (selectedItemData == null || inventoryService == null)
        {
            return;
        }

        inventoryService.RemoveItem(selectedItemData, 1);
    }

    private void DropSelectedItem()
    {
        InventoryItem selectedItem = FindSelectedItem();
        if (selectedItem == null || inventoryService == null)
        {
            return;
        }

        inventoryService.RemoveItem(selectedItem.itemData, selectedItem.amount);
    }

    private void ConfirmSelectedItemSelection()
    {
        InventoryItem selectedItem = FindSelectedItem();
        if (selectedItem == null)
        {
            return;
        }

        selectionCallback?.Invoke(selectedItem);
    }

    private InventoryItem FindSelectedItem()
    {
        if (inventoryService == null || selectedItemData == null)
        {
            return null;
        }

        foreach (InventoryItem item in inventoryService.Items)
        {
            if (item != null && item.itemData == selectedItemData)
            {
                return item;
            }
        }

        return null;
    }

    private void SyncSelectionDetails()
    {
        InventoryItem selectedItem = FindSelectedItem();
        if (selectedItem == null || selectedItem.itemData == null)
        {
            ClearSelection();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = selectedItem.itemData.icon;
            itemIcon.enabled = selectedItem.itemData.icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = selectedItem.itemData.itemName;
        }

        if (itemDescText != null)
        {
            itemDescText.text = selectedItem.itemData.description;
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

        if (inventoryService != null)
        {
            inventoryService.OnInventoryChanged -= RefreshUI;
        }
    }
}
