using System;
using System.Collections.Generic;
using System.Text;
using Game.Inventory.Interface;
using UnityEngine;

namespace Game.Inventory.Impl
{
    public class InventoryManager : MonoBehaviour, IInventoryService
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Inventory Data")]
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

        [Header("Max Slots")]
        [SerializeField] private int maxSlots = 20;

        public IReadOnlyList<InventoryItem> Items => items;
        public int MaxSlots => maxSlots;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            NormalizeItems("Awake", false);
        }

        public void ClearInventory()
        {
            items.Clear();
            NotifyChanged();
        }

        public IReadOnlyList<InventoryItem> GetItems()
        {
            return items;
        }

        public InventoryItem FindItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            return items.Find(i =>
                i != null &&
                !string.IsNullOrWhiteSpace(i.itemId) &&
                i.itemId == itemId);
        }

        public bool AddItem(string itemId, int amount = 1, bool stackable = true)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            NormalizeItems("AddItem pre-check", false);

            bool resolvedStackable = ResolveStackable(itemId, stackable, "AddItem");
            int beforeCount = GetItemCountInternal(itemId);

            Debug.Log(
                "Inventory AddItem request itemId=" + itemId +
                " amount=" + amount +
                " requestedStackable=" + stackable +
                " resolvedStackable=" + resolvedStackable +
                " beforeCount=" + beforeCount +
                " entriesBefore=" + items.Count);

            if (resolvedStackable)
            {
                InventoryItem exist = FindItem(itemId);
                if (exist != null)
                {
                    int oldAmount = exist.amount;
                    exist.amount += amount;

                    Debug.Log(
                        "Inventory AddItem stacked existing itemId=" + itemId +
                        " oldAmount=" + oldAmount +
                        " addAmount=" + amount +
                        " newAmount=" + exist.amount);

                    NotifyChanged();
                    return true;
                }
            }

            int requiredSlots = resolvedStackable ? 1 : amount;
            if (items.Count + requiredSlots > maxSlots)
            {
                Debug.LogWarning(
                    "Inventory full, cannot add itemId=" + itemId +
                    " requiredSlots=" + requiredSlots +
                    " currentEntries=" + items.Count +
                    " maxSlots=" + maxSlots);
                return false;
            }

            if (resolvedStackable)
            {
                items.Add(new InventoryItem(itemId, amount));
            }
            else
            {
                for (int i = 0; i < amount; i++)
                {
                    items.Add(new InventoryItem(itemId, 1));
                }
            }

            Debug.Log(
                "Inventory AddItem created new entry itemId=" + itemId +
                " resolvedStackable=" + resolvedStackable +
                " afterCount=" + GetItemCountInternal(itemId) +
                " entriesAfter=" + items.Count);

            NotifyChanged();
            return true;
        }

        public bool RemoveItem(string itemId, int amount = 1, bool stackable = true)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            NormalizeItems("RemoveItem pre-check", false);

            bool resolvedStackable = ResolveStackable(itemId, stackable, "RemoveItem");
            if (GetItemCountInternal(itemId) < amount)
            {
                return false;
            }

            if (resolvedStackable)
            {
                InventoryItem exist = FindItem(itemId);
                if (exist == null)
                {
                    return false;
                }

                exist.amount -= amount;
                if (exist.amount <= 0)
                {
                    items.Remove(exist);
                }
            }
            else
            {
                int remaining = amount;
                for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    InventoryItem entry = items[i];
                    if (entry == null || entry.itemId != itemId)
                    {
                        continue;
                    }

                    int entryAmount = Mathf.Max(1, entry.amount);
                    int removable = Mathf.Min(entryAmount, remaining);
                    entry.amount -= removable;
                    remaining -= removable;

                    if (entry.amount <= 0)
                    {
                        items.RemoveAt(i);
                    }
                }
            }

            NormalizeItems("RemoveItem post-check", false);
            NotifyChanged();
            return true;
        }

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            return GetItemCountInternal(itemId);
        }

        public bool HasSpace(string itemId, int amount = 1, bool stackable = true)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            NormalizeItems("HasSpace pre-check", false);

            bool resolvedStackable = ResolveStackable(itemId, stackable, "HasSpace");
            if (resolvedStackable)
            {
                InventoryItem exist = FindItem(itemId);
                if (exist != null)
                {
                    return true;
                }

                return items.Count < maxSlots;
            }

            return items.Count + amount <= maxSlots;
        }

        public bool NormalizeItems(string context = "Manual", bool notifyChange = false)
        {
            if (items == null)
            {
                items = new List<InventoryItem>();
                return false;
            }

            List<InventoryItem> normalized = new List<InventoryItem>();
            Dictionary<string, int> stackableEntryIndex = new Dictionary<string, int>();
            bool changed = false;

            foreach (InventoryItem entry in items)
            {
                if (entry == null)
                {
                    changed = true;
                    Debug.LogWarning("NormalizeItems[" + context + "] removed null inventory entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.itemId))
                {
                    changed = true;
                    Debug.LogWarning("NormalizeItems[" + context + "] removed entry with empty itemId.");
                    continue;
                }

                if (entry.amount <= 0)
                {
                    changed = true;
                    Debug.LogWarning(
                        "NormalizeItems[" + context + "] removed non-positive entry itemId=" +
                        entry.itemId +
                        " amount=" + entry.amount);
                    continue;
                }

                if (!TryGetConfiguredItemData(entry.itemId, out ItemDataRuntime itemData))
                {
                    Debug.LogWarning(
                        "NormalizeItems[" + context + "] config not found for itemId=" +
                        entry.itemId +
                        ". Keeping raw entry amount=" + entry.amount + ".");
                    normalized.Add(new InventoryItem(entry.itemId, entry.amount));
                    continue;
                }

                if (itemData.stackable)
                {
                    if (stackableEntryIndex.TryGetValue(entry.itemId, out int existingIndex))
                    {
                        int oldAmount = normalized[existingIndex].amount;
                        normalized[existingIndex].amount += entry.amount;
                        changed = true;

                        Debug.Log(
                            "NormalizeItems[" + context + "] merged stackable itemId=" +
                            entry.itemId +
                            " mergeAmount=" + entry.amount +
                            " oldAmount=" + oldAmount +
                            " newAmount=" + normalized[existingIndex].amount);
                        continue;
                    }

                    stackableEntryIndex[entry.itemId] = normalized.Count;
                    normalized.Add(new InventoryItem(entry.itemId, entry.amount));
                    continue;
                }

                if (entry.amount > 1)
                {
                    changed = true;
                    Debug.Log(
                        "NormalizeItems[" + context + "] split non-stackable itemId=" +
                        entry.itemId +
                        " amount=" + entry.amount +
                        " into independent entries.");

                    for (int i = 0; i < entry.amount; i++)
                    {
                        normalized.Add(new InventoryItem(entry.itemId, 1));
                    }

                    continue;
                }

                normalized.Add(new InventoryItem(entry.itemId, entry.amount));
            }

            if (!changed)
            {
                return false;
            }

            items = normalized;
            Debug.Log("NormalizeItems[" + context + "] result: " + GetItemsDebugSummary());

            if (notifyChange)
            {
                NotifyChanged();
            }

            return true;
        }

        public string GetItemsDebugSummary()
        {
            if (items == null || items.Count == 0)
            {
                return "(empty)";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                InventoryItem item = items[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                if (item == null)
                {
                    builder.Append("[null]");
                    continue;
                }

                builder.Append(item.itemId);
                builder.Append(" x");
                builder.Append(item.amount);
            }

            return builder.ToString();
        }

        private int GetItemCountInternal(string itemId)
        {
            int total = 0;
            foreach (InventoryItem entry in items)
            {
                if (entry == null || entry.itemId != itemId)
                {
                    continue;
                }

                total += Mathf.Max(0, entry.amount);
            }

            return total;
        }

        private bool ResolveStackable(string itemId, bool fallbackStackable, string context)
        {
            if (TryGetConfiguredItemData(itemId, out ItemDataRuntime itemData))
            {
                return itemData.stackable;
            }

            Debug.LogWarning(
                "Inventory " + context +
                " could not find item config for itemId=" + itemId +
                ". Falling back to stackable=" + fallbackStackable + ".");
            return fallbackStackable;
        }

        private static bool TryGetConfiguredItemData(string itemId, out ItemDataRuntime itemData)
        {
            itemData = null;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            itemData = ItemDatabaseRuntime.FindById(itemId);
            return itemData != null;
        }

        private void NotifyChanged()
        {
            Debug.Log("Inventory changed: " + GetItemsDebugSummary());
            OnInventoryChanged?.Invoke();
        }
    }
}
