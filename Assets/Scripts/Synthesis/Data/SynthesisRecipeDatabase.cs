using System.Collections.Generic;
using Game.Fishing.Data;
using UnityEngine;

namespace Game.Synthesis.Data
{
    [CreateAssetMenu(fileName = "SynthesisRecipeDatabase", menuName = "Game/Synthesis/Recipe Database")]
    public class SynthesisRecipeDatabase : ScriptableObject
    {
        [SerializeField] private List<SynthesisRecipeData> recipes = new List<SynthesisRecipeData>();

        public IReadOnlyList<SynthesisRecipeData> Recipes => recipes;

        public SynthesisRecipeData FindRecipe(FishData a, FishData b)
        {
            if (a == null || b == null)
            {
                return null;
            }

            foreach (SynthesisRecipeData recipe in recipes)
            {
                if (IsMatch(recipe, a, b))
                {
                    return recipe;
                }
            }

            return null;
        }

        public bool HasRecipe(FishData a, FishData b)
        {
            return FindRecipe(a, b) != null;
        }

        private static bool IsMatch(SynthesisRecipeData recipe, FishData a, FishData b)
        {
            if (recipe == null || recipe.inputFishA == null || recipe.inputFishB == null || recipe.outputItem == null)
            {
                return false;
            }

            bool exactMatch = recipe.inputFishA == a && recipe.inputFishB == b;
            if (exactMatch)
            {
                return true;
            }

            if (!recipe.orderInsensitive)
            {
                return false;
            }

            return recipe.inputFishA == b && recipe.inputFishB == a;
        }
    }
}
