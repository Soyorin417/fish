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

        if (item == null || string.IsNullOrWhiteSpace(item.itemId))
        {
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

        ItemDataRuntime itemData = ItemDatabaseRuntime.FindById(item.itemId);

        if (itemData == null)
        {
            Debug.LogWarning("SetData item config missing: " + item.itemId);

            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }

            if (amountText != null)
            {
                amountText.text = item.amount > 1 ? "x" + item.amount : string.Empty;
            }

            SetSelected(false);
            return;
        }

        if (icon != null)
        {
            Sprite sp = itemData.LoadIcon();
            icon.sprite = sp;
            icon.enabled = sp != null;
        }

        if (amountText != null)
        {
            amountText.text = item.amount > 1 ? "x" + item.amount : string.Empty;
        }

        SetSelected(false);
    }

    public void OnClick()
    {
        if (currentItem == null || string.IsNullOrWhiteSpace(currentItem.itemId))
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
