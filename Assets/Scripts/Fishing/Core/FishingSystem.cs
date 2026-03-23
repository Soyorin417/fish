using UnityEngine;

namespace Game.Fishing.Core
{
    public class FishingSystem : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour controllerSource;

        private IFishingController controller;

        public IFishingController Controller => controller;
        public FishingState State => controller != null ? controller.State : FishingState.None;

        private void Awake()
        {
            controller = controllerSource as IFishingController;

            if (controller == null && controllerSource != null)
            {
                Debug.LogError("controllerSource does not implement IFishingController.");
            }
        }

        public void CancelFishing()
        {
            controller?.CancelFishing();
        }
    }
}
