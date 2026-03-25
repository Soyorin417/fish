using System;
using System.Collections.Generic;
using Game.Inventory;
using Game.Inventory.Interface;
using Game.Synthesis.Data;
using Game.Synthesis.Interface;
using UnityEngine;

namespace Game.Synthesis.Core
{
    public class SynthesisManager : MonoBehaviour, ISynthesisService
    {
        private const string FallbackRecipeId = "fallback_recipe";
        private const string FallbackRouteId = "fallback_route";

        [Header("Data")]
        [SerializeField] private SynthesisRecipeDatabase recipeDatabase;

        [Header("Services")]
        [SerializeField] private MonoBehaviour inventoryServiceProvider;

        [Header("Fallback")]
        [SerializeField] private string defaultResultFishId = "fish_025";

        private IInventoryService inventoryService;

        public event Action<SynthesisResult> OnSynthesisFinished;

        private void Awake()
        {
            ResolveInventoryService();
            ResolveRecipeDatabase();
        }

        public bool CanSynthesize(string fishId1, string fishId2)
        {
            if (!ResolveInventoryService() || !ResolveRecipeDatabase())
            {
                return false;
            }

            if (!TryResolveRecipe(fishId1, fishId2, out SynthesisRecipeData recipe, out _))
            {
                return false;
            }

            return recipe != null && HasEnoughMaterials(fishId1, fishId2);
        }

        public SynthesisResult TrySynthesize(string fishId1, string fishId2)
        {
            if (!ResolveInventoryService())
            {
                return Finish(SynthesisResult.CreateFailure("No inventory service found.", fishId1, fishId2));
            }

            if (!ResolveRecipeDatabase())
            {
                return Finish(SynthesisResult.CreateFailure("No synthesis recipe database available.", fishId1, fishId2));
            }

            if (!TryResolveRecipe(fishId1, fishId2, out SynthesisRecipeData recipe, out string failureReason))
            {
                return Finish(SynthesisResult.CreateFailure(failureReason, fishId1, fishId2, recipe));
            }

            if (!HasEnoughMaterials(fishId1, fishId2))
            {
                return Finish(SynthesisResult.CreateFailure("Not enough synthesis materials in inventory.", fishId1, fishId2, recipe));
            }

            Dictionary<string, int> removalPlan = BuildRemovalPlan(fishId1, fishId2);
            List<KeyValuePair<string, int>> removedEntries = new List<KeyValuePair<string, int>>();

            foreach (KeyValuePair<string, int> entry in removalPlan)
            {
                bool removed = inventoryService.RemoveItem(entry.Key, entry.Value, ResolveStackable(entry.Key));
                if (!removed)
                {
                    RollbackRemovedItems(removedEntries);
                    return Finish(SynthesisResult.CreateFailure("Failed to consume synthesis materials.", fishId1, fishId2, recipe));
                }

                removedEntries.Add(entry);
            }

            bool added = inventoryService.AddItem(recipe.resultFishId, 1, ResolveStackable(recipe.resultFishId));
            if (!added)
            {
                RollbackRemovedItems(removedEntries);
                return Finish(SynthesisResult.CreateFailure("Failed to add synthesis result to inventory.", fishId1, fishId2, recipe));
            }

            Debug.Log(
                "SynthesisManager success recipeId=" + recipe.recipeId +
                " routeId=" + recipe.routeId +
                " material1Id=" + fishId1 +
                " material2Id=" + fishId2 +
                " resultFishId=" + recipe.resultFishId);

            return Finish(SynthesisResult.CreateSuccess(fishId1, fishId2, recipe));
        }

        public string GetResultFishId(string fishId1, string fishId2)
        {
            if (!ResolveRecipeDatabase())
            {
                return null;
            }

            SynthesisRecipeData recipe = GetMatchedRecipe(fishId1, fishId2);
            return recipe != null ? recipe.resultFishId : null;
        }

        public int GetMatchedRecipeCount(string fishId1, string fishId2)
        {
            if (!ResolveRecipeDatabase())
            {
                return 0;
            }

            int recipeCount = recipeDatabase.GetRecipeCount(fishId1, fishId2);
            if (recipeCount > 0)
            {
                return recipeCount;
            }

            return CanUseFallbackRecipe(fishId1, fishId2) ? 1 : 0;
        }

        public SynthesisRecipeData GetMatchedRecipe(string fishId1, string fishId2)
        {
            if (!ResolveRecipeDatabase())
            {
                return null;
            }

            SynthesisRecipeData recipe = recipeDatabase.FindRecipe(fishId1, fishId2);
            if (recipe != null)
            {
                return recipe;
            }

            return CreateFallbackRecipe(fishId1, fishId2);
        }

        private bool TryResolveRecipe(
            string fishId1,
            string fishId2,
            out SynthesisRecipeData recipe,
            out string failureReason)
        {
            recipe = null;

            if (string.IsNullOrWhiteSpace(fishId1) || string.IsNullOrWhiteSpace(fishId2))
            {
                failureReason = "Please select two fish materials.";
                return false;
            }

            recipe = recipeDatabase.GetRandomRecipe(fishId1, fishId2);
            if (recipe == null)
            {
                recipe = CreateFallbackRecipe(fishId1, fishId2);
                if (recipe == null)
                {
                    failureReason = "No matching fusion recipe was found.";
                    return false;
                }

                Debug.LogWarning(
                    "SynthesisManager using fallback synthesis result for material1Id=" + fishId1 +
                    " material2Id=" + fishId2 +
                    " resultFishId=" + recipe.resultFishId);
            }

            if (string.IsNullOrWhiteSpace(recipe.resultFishId))
            {
                failureReason = "The matched fusion recipe has no result fish configured.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private bool ResolveInventoryService()
        {
            if (inventoryService != null)
            {
                return true;
            }

            inventoryService = inventoryServiceProvider as IInventoryService;
            if (inventoryService == null && inventoryServiceProvider != null)
            {
                Debug.LogError("inventoryServiceProvider does not implement IInventoryService.");
            }

            if (inventoryService != null)
            {
                return true;
            }

            foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>(true))
            {
                if (behaviour is IInventoryService service)
                {
                    inventoryServiceProvider = behaviour;
                    inventoryService = service;
                    return true;
                }
            }

            Debug.LogError("SynthesisManager could not resolve an IInventoryService in the scene.");
            return false;
        }

        private bool ResolveRecipeDatabase()
        {
            if (recipeDatabase == null)
            {
                recipeDatabase = ScriptableObject.CreateInstance<SynthesisRecipeDatabase>();
                recipeDatabase.name = "RuntimeSynthesisRecipeDatabase";
            }

            recipeDatabase.EnsureInitialized();
            return recipeDatabase != null;
        }

        private bool CanUseFallbackRecipe(string fishId1, string fishId2)
        {
            return !string.IsNullOrWhiteSpace(fishId1) &&
                   !string.IsNullOrWhiteSpace(fishId2) &&
                   !string.IsNullOrWhiteSpace(defaultResultFishId);
        }

        private SynthesisRecipeData CreateFallbackRecipe(string fishId1, string fishId2)
        {
            if (!CanUseFallbackRecipe(fishId1, fishId2))
            {
                return null;
            }

            return new SynthesisRecipeData
            {
                recipeId = FallbackRecipeId,
                routeId = FallbackRouteId,
                material1Id = fishId1,
                material2Id = fishId2,
                resultFishId = defaultResultFishId,
                isSymmetric = true
            };
        }

        private bool HasEnoughMaterials(string fishId1, string fishId2)
        {
            if (inventoryService == null)
            {
                return false;
            }

            Dictionary<string, int> removalPlan = BuildRemovalPlan(fishId1, fishId2);
            foreach (KeyValuePair<string, int> entry in removalPlan)
            {
                if (inventoryService.GetItemCount(entry.Key) < entry.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, int> BuildRemovalPlan(string fishId1, string fishId2)
        {
            Dictionary<string, int> removalPlan = new Dictionary<string, int>(StringComparer.Ordinal);
            AddRequirement(removalPlan, fishId1);
            AddRequirement(removalPlan, fishId2);
            return removalPlan;
        }

        private static void AddRequirement(Dictionary<string, int> removalPlan, string fishId)
        {
            if (string.IsNullOrWhiteSpace(fishId))
            {
                return;
            }

            removalPlan.TryGetValue(fishId, out int currentValue);
            removalPlan[fishId] = currentValue + 1;
        }

        private void RollbackRemovedItems(List<KeyValuePair<string, int>> removedEntries)
        {
            foreach (KeyValuePair<string, int> entry in removedEntries)
            {
                inventoryService.AddItem(entry.Key, entry.Value, ResolveStackable(entry.Key));
            }
        }

        private static bool ResolveStackable(string itemId)
        {
            ItemDataRuntime itemData = ItemDatabaseRuntime.FindById(itemId);
            if (itemData != null)
            {
                return itemData.stackable;
            }

            Debug.LogWarning(
                "SynthesisManager could not find item config for itemId=" + itemId +
                ". Falling back to stackable=true.");
            return true;
        }

        private SynthesisResult Finish(SynthesisResult result)
        {
            OnSynthesisFinished?.Invoke(result);
            return result;
        }
    }
}
