using System;
using System.Collections.Generic;
using System.IO;
using Game.Serialization;
using UnityEngine;

namespace Game.Synthesis.Data
{
    [CreateAssetMenu(fileName = "SynthesisRecipeDatabase", menuName = "Game/Synthesis/Recipe Database")]
    public class SynthesisRecipeDatabase : ScriptableObject
    {
        private static readonly StringComparer KeyComparer = StringComparer.Ordinal;

        [SerializeField] private List<SynthesisRecipeData> recipes = new List<SynthesisRecipeData>();

        private readonly Dictionary<string, List<SynthesisRecipeData>> symmetricRecipesByKey =
            new Dictionary<string, List<SynthesisRecipeData>>(KeyComparer);

        private readonly Dictionary<string, List<SynthesisRecipeData>> directionalRecipesByKey =
            new Dictionary<string, List<SynthesisRecipeData>>(KeyComparer);

        private readonly HashSet<string> warnedDuplicatePools = new HashSet<string>(KeyComparer);

        private bool initialized;

        public IReadOnlyList<SynthesisRecipeData> Recipes => recipes;

        public void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            LoadFromLubanConfig();
        }

        public SynthesisRecipeData FindRecipe(string fishId1, string fishId2)
        {
            if (string.IsNullOrWhiteSpace(fishId1) || string.IsNullOrWhiteSpace(fishId2))
            {
                return null;
            }

            EnsureInitialized();

            string exactKey = BuildOrderedKey(fishId1, fishId2);
            if (directionalRecipesByKey.TryGetValue(exactKey, out List<SynthesisRecipeData> directionalRecipes) &&
                directionalRecipes.Count > 0)
            {
                return directionalRecipes[0];
            }

            string symmetricKey = BuildLookupKey(fishId1, fishId2);
            if (symmetricRecipesByKey.TryGetValue(symmetricKey, out List<SynthesisRecipeData> symmetricRecipes) &&
                symmetricRecipes.Count > 0)
            {
                return symmetricRecipes[0];
            }

            return null;
        }

        public bool HasRecipe(string fishId1, string fishId2)
        {
            return FindRecipe(fishId1, fishId2) != null;
        }

        public int GetRecipeCount(string fishId1, string fishId2)
        {
            return FindRecipes(fishId1, fishId2).Count;
        }

        public string GetResultFishId(string fishId1, string fishId2)
        {
            SynthesisRecipeData recipe = FindRecipe(fishId1, fishId2);
            return recipe != null ? recipe.resultFishId : null;
        }

        public IReadOnlyList<SynthesisRecipeData> FindRecipes(string fishId1, string fishId2)
        {
            if (string.IsNullOrWhiteSpace(fishId1) || string.IsNullOrWhiteSpace(fishId2))
            {
                return Array.Empty<SynthesisRecipeData>();
            }

            EnsureInitialized();

            string exactKey = BuildOrderedKey(fishId1, fishId2);
            if (directionalRecipesByKey.TryGetValue(exactKey, out List<SynthesisRecipeData> directionalRecipes) &&
                directionalRecipes.Count > 0)
            {
                return directionalRecipes;
            }

            string symmetricKey = BuildLookupKey(fishId1, fishId2);
            if (symmetricRecipesByKey.TryGetValue(symmetricKey, out List<SynthesisRecipeData> symmetricRecipes) &&
                symmetricRecipes.Count > 0)
            {
                return symmetricRecipes;
            }

            return Array.Empty<SynthesisRecipeData>();
        }

        public SynthesisRecipeData GetRandomRecipe(string fishId1, string fishId2)
        {
            IReadOnlyList<SynthesisRecipeData> recipesForPair = FindRecipes(fishId1, fishId2);
            if (recipesForPair.Count == 0)
            {
                return null;
            }

            int index = UnityEngine.Random.Range(0, recipesForPair.Count);
            return recipesForPair[index];
        }

        public static string BuildLookupKey(string fishId1, string fishId2)
        {
            string a = NormalizeKeyPart(fishId1);
            string b = NormalizeKeyPart(fishId2);

            return string.CompareOrdinal(a, b) <= 0
                ? a + "|" + b
                : b + "|" + a;
        }

        private static string BuildOrderedKey(string fishId1, string fishId2)
        {
            return NormalizeKeyPart(fishId1) + "|" + NormalizeKeyPart(fishId2);
        }

        private static string NormalizeKeyPart(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private void LoadFromLubanConfig()
        {
            recipes.Clear();
            symmetricRecipesByKey.Clear();
            directionalRecipesByKey.Clear();

            string path = Path.Combine(Application.streamingAssetsPath, "Luban/fish_fusion_recipe.json");
            if (!File.Exists(path))
            {
                Debug.LogError("Synthesis recipe config json not found: " + path);
                return;
            }

            string json = File.ReadAllText(path);
            List<LubanRecipeRow> rows = JsonArrayHelper.FromJsonArray<LubanRecipeRow>(json);
            if (rows == null || rows.Count == 0)
            {
                Debug.LogWarning("fish_fusion_recipe.json is empty or failed to parse: " + path);
                return;
            }

            foreach (LubanRecipeRow row in rows)
            {
                if (row == null)
                {
                    continue;
                }

                SynthesisRecipeData recipe = new SynthesisRecipeData
                {
                    recipeId = row.recipeId,
                    routeId = row.routeId,
                    material1Id = row.material1Id,
                    material2Id = row.material2Id,
                    resultFishId = row.resultFishId,
                    isSymmetric = row.isSymmetric
                };

                if (!recipe.IsValid())
                {
                    Debug.LogWarning("Skipped invalid fusion recipe row while loading Luban config.");
                    continue;
                }

                recipes.Add(recipe);
                RegisterRecipe(recipe);
            }

            Debug.Log(
                "SynthesisRecipeDatabase loaded recipes from Luban. " +
                "rawCount=" + recipes.Count +
                " symmetricKeys=" + symmetricRecipesByKey.Count +
                " directionalKeys=" + directionalRecipesByKey.Count);
        }

        private void RegisterRecipe(SynthesisRecipeData recipe)
        {
            if (recipe.isSymmetric)
            {
                string key = BuildLookupKey(recipe.material1Id, recipe.material2Id);
                AddRecipeToPool(symmetricRecipesByKey, key, recipe);
                return;
            }

            string orderedKey = BuildOrderedKey(recipe.material1Id, recipe.material2Id);
            AddRecipeToPool(directionalRecipesByKey, orderedKey, recipe);
        }

        private void AddRecipeToPool(
            Dictionary<string, List<SynthesisRecipeData>> pool,
            string key,
            SynthesisRecipeData recipe)
        {
            if (!pool.TryGetValue(key, out List<SynthesisRecipeData> recipesForKey))
            {
                recipesForKey = new List<SynthesisRecipeData>();
                pool.Add(key, recipesForKey);
            }

            recipesForKey.Add(recipe);
            if (recipesForKey.Count > 1 && warnedDuplicatePools.Add(key))
            {
                Debug.LogWarning(
                    "Multiple fish fusion results detected for materials=" + key +
                    ". Runtime will choose one result randomly from " + recipesForKey.Count +
                    " configured recipes. First routeId=" + recipesForKey[0].routeId + ".");
            }
        }

        [Serializable]
        private class LubanRecipeRow
        {
            public string recipeId;
            public string routeId;
            public string material1Id;
            public string material2Id;
            public string resultFishId;
            public bool isSymmetric = true;
        }
    }
}
