using System;
using System.Collections.Generic;
using Game.Inventory;

namespace Game.Inventory.Interface
{
    public interface IInventoryService
    {
        IReadOnlyList<InventoryItem> Items { get; }
        int MaxSlots { get; }

        event Action OnInventoryChanged;

        IReadOnlyList<InventoryItem> GetItems();
        InventoryItem FindItem(ItemData itemData);
        void ClearInventory();
        bool AddItem(ItemData itemData, int amount = 1);
        bool RemoveItem(ItemData itemData, int amount = 1);
        int GetItemCount(ItemData itemData);
        bool HasSpace(ItemData itemData, int amount = 1);
    }
}

