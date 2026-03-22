using Game.Inventory.Impl;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private InventoryItem selectedItem;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedItem);

        if (dropButton != null)
            dropButton.onClick.AddListener(DropSelectedItem);

        RefreshUI();
        ClearSelection();
        CloseInventory();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (panelRoot == null) return;

        bool show = !panelRoot.activeSelf;
        panelRoot.SetActive(show);

        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void OpenInventory()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void CloseInventory()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
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

    public void SelectItem(InventoryItem item)
    {
        selectedItem = item;

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
            InventoryManager.Instance.RemoveItem(selectedItem.itemData, selectedItem.amount);
            ClearSelection();
        }
        else
        {
            SelectItem(selectedItem);
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
}