using System.Collections.Generic;
using Game.Inventory;
using UnityEngine;

namespace Game.Fishing.Data
{
    [CreateAssetMenu(fileName = "FishDataDatabase", menuName = "Game/Fishing/Fish Database")]
    public class FishDataDatabase : ScriptableObject
    {
        [SerializeField] private List<FishData> allFish = new List<FishData>();

        public IReadOnlyList<FishData> AllFish => allFish;

        public FishData GetFishByItemData(ItemData itemData)
        {
            TryGetFishByItemData(itemData, out FishData fishData);
            return fishData;
        }

        public bool TryGetFishByItemData(ItemData itemData, out FishData fishData)
        {
            fishData = null;
            if (itemData == null)
            {
                return false;
            }

            foreach (FishData fish in allFish)
            {
                if (fish == null || fish.inventoryItem != itemData)
                {
                    continue;
                }

                fishData = fish;
                return true;
            }

            return false;
        }
    }
}
