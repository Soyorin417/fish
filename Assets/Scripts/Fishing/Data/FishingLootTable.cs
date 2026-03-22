using System.Collections.Generic;
using UnityEngine;

namespace Game.Fishing.Data
{
    [CreateAssetMenu(fileName = "FishingLootTable", menuName = "Game/Fishing/Loot Table")]
    public class FishingLootTable : ScriptableObject
    {
        [System.Serializable]
        public class LootEntry
        {
            public FishData fishData;
            public int weight = 1;
        }

        public List<LootEntry> entries = new List<LootEntry>();

        public FishData Roll()
        {
            if (entries == null || entries.Count == 0) return null;

            int totalWeight = 0;
            foreach (var entry in entries)
            {
                if (entry != null && entry.fishData != null && entry.weight > 0)
                    totalWeight += entry.weight;
            }

            if (totalWeight <= 0) return null;

            int roll = Random.Range(0, totalWeight);
            int current = 0;

            foreach (var entry in entries)
            {
                if (entry == null || entry.fishData == null || entry.weight <= 0)
                    continue;

                current += entry.weight;
                if (roll < current)
                    return entry.fishData;
            }

            return null;
        }
    }
}