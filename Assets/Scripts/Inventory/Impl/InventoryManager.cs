using System;
using System.Collections.Generic;
using Game.Inventory;
using UnityEngine;
using Game.Inventory.Interface;

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

        public ItemData testItemData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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

        public InventoryItem FindItem(ItemData itemData)
        {
            if (itemData == null)
            {
                return null;
            }

            return items.Find(i => i != null && i.itemData == itemData);
        }

        public bool AddItem(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0)
            {
                return false;
            }

            Debug.Log("AddItem called: " + itemData.itemName);

            if (itemData.stackable)
            {
                InventoryItem exist = FindItem(itemData);
                if (exist != null)
                {
                    exist.amount += amount;
                    NotifyChanged();
                    return true;
                }
            }

            int requiredSlots = itemData.stackable ? 1 : amount;
            if (items.Count + requiredSlots > maxSlots)
            {
                return false;
            }

            if (itemData.stackable)
            {
                items.Add(new InventoryItem(itemData, amount));
            }
            else
            {
                for (int i = 0; i < amount; i++)
                {
                    items.Add(new InventoryItem(itemData, 1));
                }
            }

            NotifyChanged();
            return true;
        }

        public bool RemoveItem(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0)
            {
                return false;
            }

            if (GetItemCount(itemData) < amount)
            {
                return false;
            }

            if (itemData.stackable)
            {
                InventoryItem exist = FindItem(itemData);
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
                    if (entry == null || entry.itemData != itemData)
                    {
                        continue;
                    }

                    items.RemoveAt(i);
                    remaining--;
                }
            }

            NotifyChanged();
            return true;
        }

        public int GetItemCount(ItemData itemData)
        {
            if (itemData == null)
            {
                return 0;
            }

            int total = 0;
            foreach (InventoryItem entry in items)
            {
                if (entry == null || entry.itemData != itemData)
                {
                    continue;
                }

                total += Mathf.Max(0, entry.amount);
            }

            return total;
        }

        public bool HasSpace(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0)
            {
                return false;
            }

            if (itemData.stackable)
            {
                InventoryItem exist = FindItem(itemData);
                if (exist != null)
                {
                    return true;
                }

                return items.Count < maxSlots;
            }

            return items.Count + amount <= maxSlots;
        }

        private void NotifyChanged()
        {
            Debug.Log("NotifyChanged called");
            OnInventoryChanged?.Invoke();
        }
    }
}

