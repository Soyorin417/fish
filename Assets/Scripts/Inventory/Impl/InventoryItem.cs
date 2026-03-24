using System;

namespace Game.Inventory
{
    [Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int amount;

        public InventoryItem()
        {
        }

        public InventoryItem(string itemId, int amount)
        {
            this.itemId = itemId;
            this.amount = amount;
        }
    }
}