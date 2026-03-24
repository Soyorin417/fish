using UnityEngine;
using Game.Inventory;

namespace Game.Fishing.Data
{
    [CreateAssetMenu(fileName = "FishData", menuName = "Game/Fishing/Fish Data")]
    public class FishData : ScriptableObject
    {
        public string fishId;
        public string fishName;
        public Sprite icon;

        public ItemDataRuntime inventoryItem;

        public int rarity = 1;

        [Header("合成配置")]
        public bool canSynthesize = true;
        public string synthGroupId;
        public int synthTier = 1;

        [Header("掉落权重")]
        public float weight = 1f;

        [Header("体型范围")]
        public float sizeMin = 0.8f;
        public float sizeMax = 1.2f;
    }
}