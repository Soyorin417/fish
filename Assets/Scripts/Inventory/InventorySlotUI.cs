using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text amountText;
    public Button button;
    public GameObject highlight;

    private InventoryItem currentItem;
    private InventoryUI inventoryUI;

    public void SetData(InventoryItem item, InventoryUI ui)
    {
        currentItem = item;
        inventoryUI = ui;

        if (item == null || item.itemData == null)
        {
            Debug.LogWarning("item 或 itemData 为空，执行 Clear()");
            Clear();
            return;
        }

        if (nameText != null)
            nameText.text = item.itemData.itemName;

        if (amountText != null)
            amountText.text = item.amount > 1 ? "x" + item.amount : "";

        if (icon != null)
        {
            icon.sprite = item.itemData.icon;
            icon.enabled = item.itemData.icon != null;
        }

        if (highlight != null)
            highlight.SetActive(false);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickSlot);
        }

        Debug.Log("SetData 成功: " + item.itemData.itemName);
    }

    private void OnClickSlot()
    {
        if (currentItem != null && inventoryUI != null)
        {
            inventoryUI.SelectItem(this, currentItem);
        }
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null)
            highlight.SetActive(selected);
    }

    public void Clear()
    {
        currentItem = null;

        if (nameText != null) nameText.text = "";
        if (amountText != null) amountText.text = "";

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }

        if (highlight != null)
            highlight.SetActive(false);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}