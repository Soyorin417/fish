using System;
using Game.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TMP_Text amountText;
    public GameObject highlight;

    private InventoryItem currentItem;
    private Action<InventorySlotUI, InventoryItem> onSelected;

    public InventoryItem CurrentItem => currentItem;

    public void SetData(InventoryItem item, Action<InventorySlotUI, InventoryItem> onSelectedCallback)
    {
        currentItem = item;
        onSelected = onSelectedCallback;

        if (item == null || item.itemData == null)
        {
            Debug.Log("SetData: item is null");

            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }

            if (amountText != null)
            {
                amountText.text = string.Empty;
            }

            SetSelected(false);
            return;
        }

        Debug.Log("SetData item: " + item.itemData.itemName);
        Debug.Log("SetData icon missing: " + (item.itemData.icon == null));

        if (icon != null)
        {
            icon.sprite = item.itemData.icon;
            icon.enabled = item.itemData.icon != null;
        }

        if (amountText != null)
        {
            amountText.text = item.amount > 1 ? "x" + item.amount : string.Empty;
        }

        SetSelected(false);
    }

    public void OnClick()
    {
        Debug.Log("click success");

        if (currentItem == null || currentItem.itemData == null)
        {
            return;
        }

        onSelected?.Invoke(this, currentItem);
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null)
        {
            highlight.SetActive(selected);
        }
    }
}

