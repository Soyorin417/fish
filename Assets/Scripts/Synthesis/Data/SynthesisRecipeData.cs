using System;

namespace Game.Synthesis.Data
{
    [Serializable]
    public class SynthesisRecipeData
    {
        public string recipeId;
        public string routeId;
        public string material1Id;
        public string material2Id;
        public string resultFishId;
        public bool isSymmetric = true;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(recipeId) &&
                   !string.IsNullOrWhiteSpace(material1Id) &&
                   !string.IsNullOrWhiteSpace(material2Id) &&
                   !string.IsNullOrWhiteSpace(resultFishId);
        }
    }
}
