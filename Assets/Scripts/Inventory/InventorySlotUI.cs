using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Inventory.Impl;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;              // 绑定 ItemIcon
    public TMP_Text amountText;     // 绑定 Amount
    public GameObject highlight;    // 绑定 Highlight

    private InventoryItem currentItem;
    private InventoryUI inventoryUI;

    public void SetData(InventoryItem item, InventoryUI ui)
    {
        currentItem = item;
        inventoryUI = ui;

        if (item == null || item.itemData == null)
        {
            Debug.Log("SetData: item 为空");

            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }

            if (amountText != null)
                amountText.text = "";

            SetSelected(false);
            return;
        }

        Debug.Log("SetData物品: " + item.itemData.itemName);
        Debug.Log("SetData图标是否为空: " + (item.itemData.icon == null));

        if (icon != null)
        {
            icon.sprite = item.itemData.icon;
            icon.enabled = item.itemData.icon != null;
        }

        if (amountText != null)
            amountText.text = item.amount > 1 ? "x" + item.amount : "";

        SetSelected(false);
    }

    public void OnClick()
    {
        Debug.Log("click success");

        if (currentItem == null || currentItem.itemData == null)
            return;

        if (inventoryUI != null)
        {
            inventoryUI.SelectItem(this, currentItem);
        }
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null)
        {
            highlight.SetActive(selected);
        }
    }
}