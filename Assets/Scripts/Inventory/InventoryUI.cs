using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Inventory.Impl;

public class InventoryUI : MonoBehaviour
{
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

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private InventoryItem selectedItem;
    private InventorySlotUI currentSelectedSlot;

    private void Start()
    {

        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedItem);

        if (dropButton != null)
            dropButton.onClick.AddListener(DropSelectedItem);

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged += RefreshUI;
        }

        RefreshUI();
        ClearSelection();
    }

    public void RefreshUI()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager.Instance 为空");
            return;
        }

        if (slotContainer == null)
        {
            Debug.LogError("slotContainer 没绑定");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("slotPrefab 没绑定");
            return;
        }

        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        slotUIs.Clear();

        var realItems = InventoryManager.Instance.Items;

        for (int i = 0; i < realItems.Count; i++)
        {
            var item = realItems[i];

            GameObject slotObj = Instantiate(slotPrefab, slotContainer);

            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            if (slotUI == null)
            {
                Debug.LogError("slotPrefab 上没有 InventorySlotUI 组件");
                continue;
            }

            slotUI.SetData(item, this);
            slotUIs.Add(slotUI);
        }
    }

    public void SelectItem(InventorySlotUI slot, InventoryItem item)
    {
        if (currentSelectedSlot != null)
            currentSelectedSlot.SetSelected(false);

        currentSelectedSlot = slot;
        selectedItem = item;

        if (currentSelectedSlot != null)
            currentSelectedSlot.SetSelected(true);

        if (item == null || item.itemData == null)
        {
            ClearSelection();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = item.itemData.icon;
            itemIcon.enabled = item.itemData.icon != null;
        }

        if (itemNameText != null)
            itemNameText.text = item.itemData.itemName;

        if (itemDescText != null)
            itemDescText.text = item.itemData.description;

        if (itemAmountText != null)
            itemAmountText.text = "数量 x" + item.amount;
    }

    public void ClearSelection()
    {
        selectedItem = null;

        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(false);
            currentSelectedSlot = null;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (itemNameText != null) itemNameText.text = "";
        if (itemDescText != null) itemDescText.text = "";
        if (itemAmountText != null) itemAmountText.text = "";
    }

    private void UseSelectedItem()
    {
        if (selectedItem == null || selectedItem.itemData == null) return;

        Debug.Log("使用物品：" + selectedItem.itemData.itemName);

        selectedItem.amount--;

        if (selectedItem.amount <= 0)
        {
            InventoryManager.Instance.RemoveItem(selectedItem.itemData, 1);
            ClearSelection();
        }
        else
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = selectedItem.itemData.icon;
                itemIcon.enabled = selectedItem.itemData.icon != null;
            }

            if (itemNameText != null)
                itemNameText.text = selectedItem.itemData.itemName;

            if (itemDescText != null)
                itemDescText.text = selectedItem.itemData.description;

            if (itemAmountText != null)
                itemAmountText.text = "数量 x" + selectedItem.amount;
        }

        RefreshUI();
    }

    private void DropSelectedItem()
    {
        if (selectedItem == null || selectedItem.itemData == null) return;

        Debug.Log("丢弃物品：" + selectedItem.itemData.itemName);

        InventoryManager.Instance.RemoveItem(selectedItem.itemData, selectedItem.amount);
        ClearSelection();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged -= RefreshUI;
        }
    }
}