using UnityEngine;

namespace Game.Fishing.Core
{
    public class FishingSystem : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour controller;

        public MonoBehaviour Controller => controller;

        private void Update()
        {
            // 先留空，避免和旧版 FishingController 冲突
        }

        public void CancelFishing()
        {
            // 先留空，后面再接
        }
    }
}