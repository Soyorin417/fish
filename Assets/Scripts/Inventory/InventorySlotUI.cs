using System;
using Game.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image icon;
    public TMP_Text amountText;
    public GameObject highlight;

    private InventoryItem currentItem;
    private Action<InventorySlotUI, InventoryItem> onSelected;
    private Button cachedButton;

    public InventoryItem CurrentItem => currentItem;

    private void Awake()
    {
        cachedButton = GetComponent<Button>();
        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(OnClick);
            cachedButton.onClick.AddListener(OnClick);
        }
        else
        {
            Debug.LogWarning("InventorySlotUI has no Button component on " + name + ". Falling back to IPointerClickHandler only.");
        }
    }

    private void OnDestroy()
    {
        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(OnClick);
        }
    }

    public void SetData(InventoryItem item, Action<InventorySlotUI, InventoryItem> onSelectedCallback)
    {
        currentItem = item;
        onSelected = onSelectedCallback;

        Debug.Log(
            "InventorySlotUI.SetData slot=" + name +
            " itemId=" + (item != null ? item.itemId : "(null)") +
            " amount=" + (item != null ? item.amount : 0) +
            " callbackAssigned=" + (onSelectedCallback != null));

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
            Debug.LogWarning("InventorySlotUI.SetData item config missing: " + item.itemId);

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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cachedButton != null)
        {
            Debug.Log(
                "InventorySlotUI.OnPointerClick ignored because Button.onClick will handle slot=" + name +
                " itemId=" + (currentItem != null ? currentItem.itemId : "(null)"));
            return;
        }

        Debug.Log(
            "InventorySlotUI.OnPointerClick slot=" + name +
            " itemId=" + (currentItem != null ? currentItem.itemId : "(null)"));
        OnClick();
    }

    public void OnClick()
    {
        Debug.Log(
            "InventorySlotUI.OnClick slot=" + name +
            " itemId=" + (currentItem != null ? currentItem.itemId : "(null)") +
            " hasSelectionHandler=" + (onSelected != null));

        if (currentItem == null || string.IsNullOrWhiteSpace(currentItem.itemId))
        {
            Debug.LogWarning("InventorySlotUI.OnClick ignored because currentItem is empty on slot=" + name);
            return;
        }

        if (onSelected == null)
        {
            Debug.LogWarning("InventorySlotUI.OnClick has no onSelected callback for slot=" + name);
            return;
        }

        onSelected.Invoke(this, currentItem);
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null)
        {
            highlight.SetActive(selected);
        }
    }
}
