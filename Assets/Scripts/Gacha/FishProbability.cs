using System;

namespace Game.Gacha
{
    [Serializable]
    public class FishProbability
    {
        public string fishId;
        public string fishName;
        public int rarityLevel;
        public int weight;
        public float probability;
    }
}
