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
        InventoryItem FindItem(string itemId);
        void ClearInventory();
        bool AddItem(string itemId, int amount = 1, bool stackable = true);
        bool RemoveItem(string itemId, int amount = 1, bool stackable = true);
        int GetItemCount(string itemId);
        bool HasSpace(string itemId, int amount = 1, bool stackable = true);
    }
}