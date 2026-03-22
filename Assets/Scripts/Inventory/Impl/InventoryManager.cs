using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Fishing.Data;
using Game.Inventory.Interface;

namespace Game.Inventory.Impl
{
    public class InventoryManager : MonoBehaviour, IInventoryService, IFishingResultHandler
    {
        public static InventoryManager Instance { get; private set; }

        [Header("背包数据")]
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();



        [Header("最大格子数")]
        [SerializeField] private int maxSlots = 20;

        public IReadOnlyList<InventoryItem> Items => items;
        public System.Action onInventoryChanged;
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

        private void Start()
        {
            // 测试数据
            if (Items.Count == 0)
            {
                Debug.Log("添加测试物品");

                AddItem(testItemData, 20); // 拖一个 ItemData 进来
            }
        }

        public void ClearInventory()
        {
            items.Clear();
            onInventoryChanged?.Invoke();
        }


        public bool AddItem(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0) return false;

            if (itemData.stackable)
            {
                var exist = items.Find(i => i.itemData == itemData);
                if (exist != null)
                {
                    exist.amount += amount;
                    NotifyChanged();
                    return true;
                }
            }

            if (items.Count >= maxSlots)
                return false;

            items.Add(new InventoryItem(itemData, amount));
            NotifyChanged();
            return true;
        }

        public bool RemoveItem(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0) return false;

            var exist = items.Find(i => i.itemData == itemData);
            if (exist == null) return false;

            exist.amount -= amount;
            if (exist.amount <= 0)
                items.Remove(exist);

            NotifyChanged();
            return true;
        }

        public int GetItemCount(ItemData itemData)
        {
            var exist = items.Find(i => i.itemData == itemData);
            return exist == null ? 0 : exist.amount;
        }

        public bool HasSpace(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0) return false;

            if (itemData.stackable)
            {
                var exist = items.Find(i => i.itemData == itemData);
                if (exist != null) return true;
            }

            return items.Count < maxSlots;
        }

        public void HandleFishResult(FishData fishData, int amount = 1)
        {
            if (fishData == null || fishData.inventoryItem == null)
            {
                Debug.LogWarning("FishData 或 inventoryItem 未配置，无法加入背包");
                return;
            }

            bool success = AddItem(fishData.inventoryItem, amount);
            Debug.Log(success
                ? $"获得物品：{fishData.inventoryItem.itemName} x{amount}"
                : $"背包已满，无法获得：{fishData.inventoryItem.itemName}");
        }



        private void NotifyChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}