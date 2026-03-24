using System;
using System.Collections.Generic;
using Game.Fishing.Data;
using Game.Inventory;
using Game.Inventory.Interface;
using Game.Synthesis.Data;
using Game.Synthesis.Interface;
using UnityEngine;

namespace Game.Synthesis.Core
{
    public class SynthesisManager : MonoBehaviour, ISynthesisService
    {
        [Header("Data")]
        [SerializeField] private SynthesisRecipeDatabase recipeDatabase;
        [SerializeField] private FishDataDatabase fishDatabase;

        [Header("Services")]
        [SerializeField] private MonoBehaviour inventoryServiceProvider;

        private IInventoryService inventoryService;

        public event Action<SynthesisResult> OnSynthesisFinished;

        /// <summary>
        /// 初始化时解析并缓存背包服务，供后续合成流程使用。
        /// </summary>
        private void Awake()
        {
            ResolveInventoryService();
        }

        /// <summary>
        /// 判断当前选中的两个物品是否满足合成条件。
        /// </summary>
        public bool CanSynthesize(InventoryItem itemA, InventoryItem itemB)
        {
            if (!ResolveInventoryService())
            {
                return false;
            }

            if (!TryResolveRecipe(itemA, itemB, out SynthesisRecipeData recipe, out _))
            {
                return false;
            }

            return HasEnoughMaterials(itemA, itemB) && recipe != null;
        }

        /// <summary>
        /// 获取两个输入物品所匹配到的合成配方。
        /// </summary>
        public SynthesisRecipeData GetMatchedRecipe(InventoryItem itemA, InventoryItem itemB)
        {
            return TryResolveRecipe(itemA, itemB, out SynthesisRecipeData recipe, out _) ? recipe : null;
        }

        /// <summary>
        /// 执行合成流程：校验配方、扣除材料、发放产物，并返回合成结果。
        /// </summary>
        public SynthesisResult TrySynthesize(InventoryItem itemA, InventoryItem itemB)
        {
            if (!ResolveInventoryService())
            {
                return Finish(SynthesisResult.CreateFailure("No inventory service found.", itemA, itemB));
            }

            if (recipeDatabase == null)
            {
                return Finish(SynthesisResult.CreateFailure("No synthesis recipe database assigned.", itemA, itemB));
            }

            if (fishDatabase == null)
            {
                return Finish(SynthesisResult.CreateFailure("No fish database assigned.", itemA, itemB));
            }

            if (!TryResolveRecipe(itemA, itemB, out SynthesisRecipeData recipe, out string resolveMessage))
            {
                return Finish(SynthesisResult.CreateFailure(resolveMessage, itemA, itemB));
            }

            if (!HasEnoughMaterials(itemA, itemB))
            {
                return Finish(SynthesisResult.CreateFailure("Not enough materials in inventory.", itemA, itemB, recipe));
            }

            Dictionary<string, int> removalPlan = BuildRemovalPlan(itemA, itemB);
            List<KeyValuePair<string, int>> removedEntries = new List<KeyValuePair<string, int>>();

            foreach (KeyValuePair<string, int> entry in removalPlan)
            {
                bool removed = inventoryService.RemoveItem(entry.Key, entry.Value, true);
                if (!removed)
                {
                    RollbackRemovedItems(removedEntries);
                    return Finish(SynthesisResult.CreateFailure("Failed to consume synthesis materials.", itemA, itemB, recipe));
                }

                removedEntries.Add(entry);
            }

            if (recipe.outputItem == null || string.IsNullOrWhiteSpace(recipe.outputItem.itemId))
            {
                RollbackRemovedItems(removedEntries);
                return Finish(SynthesisResult.CreateFailure("Recipe output item is invalid.", itemA, itemB, recipe));
            }

            bool added = inventoryService.AddItem(
                recipe.outputItem.itemId,
                1,
                recipe.outputItem.stackable
            );

            if (!added)
            {
                RollbackRemovedItems(removedEntries);
                return Finish(SynthesisResult.CreateFailure("Failed to add synthesis result to inventory.", itemA, itemB, recipe));
            }

            string successMessage = string.IsNullOrWhiteSpace(recipe.recipeName)
                ? "Synthesis succeeded."
                : "Synthesis succeeded: " + recipe.recipeName;

            return Finish(SynthesisResult.CreateSuccess(successMessage, itemA, itemB, recipe, 1));
        }

        /// <summary>
        /// 根据两个输入物品解析对应的鱼和配方，并输出失败原因。
        /// </summary>
        private bool TryResolveRecipe(InventoryItem itemA, InventoryItem itemB, out SynthesisRecipeData recipe, out string message)
        {
            recipe = null;

            ItemDataRuntime itemDataA = GetItemDataRuntime(itemA);
            ItemDataRuntime itemDataB = GetItemDataRuntime(itemB);

            if (itemDataA == null || itemDataB == null)
            {
                message = "Please select two valid fish items.";
                return false;
            }

            if (recipeDatabase == null)
            {
                message = "No synthesis recipe database assigned.";
                return false;
            }

            if (fishDatabase == null)
            {
                message = "No fish database assigned.";
                return false;
            }

            if (!fishDatabase.TryGetFishByItemData(itemDataA, out FishData fishA))
            {
                message = "Input A is not a recognized fish item.";
                return false;
            }

            if (!fishDatabase.TryGetFishByItemData(itemDataB, out FishData fishB))
            {
                message = "Input B is not a recognized fish item.";
                return false;
            }

            if (!fishA.canSynthesize || !fishB.canSynthesize)
            {
                message = "The selected fish cannot be synthesized.";
                return false;
            }

            recipe = recipeDatabase.FindRecipe(fishA, fishB);
            if (recipe == null)
            {
                message = "No synthesis recipe matches the selected pair.";
                return false;
            }

            if (recipe.outputItem == null)
            {
                message = "Recipe output item is missing.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// 解析并获取场景中的背包服务实例。
        /// </summary>
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

        /// <summary>
        /// 检查当前背包中的材料数量是否满足本次合成需求。
        /// </summary>
        private bool HasEnoughMaterials(InventoryItem itemA, InventoryItem itemB)
        {
            if (itemA == null || itemB == null || inventoryService == null)
            {
                return false;
            }

            Dictionary<string, int> removalPlan = BuildRemovalPlan(itemA, itemB);
            foreach (KeyValuePair<string, int> entry in removalPlan)
            {
                if (inventoryService.GetItemCount(entry.Key) < entry.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 根据两个输入物品生成本次合成所需的扣除材料清单。
        /// </summary>
        private static Dictionary<string, int> BuildRemovalPlan(InventoryItem itemA, InventoryItem itemB)
        {
            Dictionary<string, int> removalPlan = new Dictionary<string, int>();
            AddRequirement(removalPlan, itemA != null ? itemA.itemId : null);
            AddRequirement(removalPlan, itemB != null ? itemB.itemId : null);
            return removalPlan;
        }

        /// <summary>
        /// 将单个物品需求加入扣除清单，并自动累加数量。
        /// </summary>
        private static void AddRequirement(Dictionary<string, int> removalPlan, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            removalPlan.TryGetValue(itemId, out int currentValue);
            removalPlan[itemId] = currentValue + 1;
        }

        /// <summary>
        /// 当合成中途失败时，把已扣除的材料重新返还到背包中。
        /// </summary>
        private void RollbackRemovedItems(List<KeyValuePair<string, int>> removedEntries)
        {
            foreach (KeyValuePair<string, int> entry in removedEntries)
            {
                inventoryService.AddItem(entry.Key, entry.Value, true);
            }
        }

        /// <summary>
        /// 根据背包物品获取其对应的运行时物品数据。
        /// </summary>
        private static ItemDataRuntime GetItemDataRuntime(InventoryItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
            {
                return null;
            }

            return ItemDatabaseRuntime.FindById(item.itemId);
        }

        /// <summary>
        /// 统一分发合成完成事件，并返回最终结果。
        /// </summary>
        private SynthesisResult Finish(SynthesisResult result)
        {
            OnSynthesisFinished?.Invoke(result);
            return result;
        }
    }
}