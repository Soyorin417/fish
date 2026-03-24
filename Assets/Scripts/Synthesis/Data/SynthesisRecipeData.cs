using Game.Fishing.Data;
using Game.Inventory;
using UnityEngine;

namespace Game.Synthesis.Data
{
    [CreateAssetMenu(fileName = "SynthesisRecipe", menuName = "Game/Synthesis/Recipe")]
    public class SynthesisRecipeData : ScriptableObject
    {
        public string recipeId;
        public string recipeName;
        public FishData inputFishA;
        public FishData inputFishB;
        public ItemData outputItem;
        public bool orderInsensitive = true;

        [TextArea]
        public string description;
    }
}
