using UnityEngine;
using Game.Fishing.Data;

namespace Game.Fishing.Spots
{
    public class FishingSpot : MonoBehaviour
    {
        [SerializeField] private string spotId;

        public string SpotId => spotId;

        public FishingSpotConfig GetConfig()
        {
            if (string.IsNullOrWhiteSpace(spotId))
            {
                Debug.LogWarning("FishingSpot on " + name + " has an empty spotId.");
                return null;
            }

            return FishingSpotConfigDatabase.FindById(spotId);
        }
    }
}
