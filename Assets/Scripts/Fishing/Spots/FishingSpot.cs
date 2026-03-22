using UnityEngine;
using Game.Fishing.Data;

namespace Game.Fishing.Spots
{
    public class FishingSpot : MonoBehaviour
    {
        [SerializeField] private FishingLootTable lootTable;
        [SerializeField] private float interactDistance = 3f;

        public FishingLootTable LootTable => lootTable;
        public float InteractDistance => interactDistance;
    }
}