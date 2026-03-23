using System;
using System.Collections.Generic;
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
        public Action onInventoryChanged;
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

        public bool AddItem(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0)
            {
                return false;
            }

            Debug.Log("AddItem called: " + itemData.itemName);

            if (itemData.stackable)
            {
                InventoryItem exist = items.Find(i => i.itemData == itemData);
                if (exist != null)
                {
                    exist.amount += amount;
                    NotifyChanged();
                    return true;
                }
            }

            if (items.Count >= maxSlots)
            {
                return false;
            }

            items.Add(new InventoryItem(itemData, amount));
            NotifyChanged();
            return true;
        }

        public bool RemoveItem(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0)
            {
                return false;
            }

            InventoryItem exist = items.Find(i => i.itemData == itemData);
            if (exist == null)
            {
                return false;
            }

            exist.amount -= amount;
            if (exist.amount <= 0)
            {
                items.Remove(exist);
            }

            NotifyChanged();
            return true;
        }

        public int GetItemCount(ItemData itemData)
        {
            InventoryItem exist = items.Find(i => i.itemData == itemData);
            return exist == null ? 0 : exist.amount;
        }

        public bool HasSpace(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0)
            {
                return false;
            }

            if (itemData.stackable)
            {
                InventoryItem exist = items.Find(i => i.itemData == itemData);
                if (exist != null)
                {
                    return true;
                }
            }

            return items.Count < maxSlots;
        }

        private void NotifyChanged()
        {
            Debug.Log("NotifyChanged called");
            onInventoryChanged?.Invoke();
            OnInventoryChanged?.Invoke();

            if (InventoryJsonLoader.Instance != null)
            {
                InventoryJsonLoader.Instance.Save();
            }
        }
    }
}
