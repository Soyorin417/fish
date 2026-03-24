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

        private void Awake()
        {
            ResolveInventoryService();
        }

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

        public SynthesisRecipeData GetMatchedRecipe(InventoryItem itemA, InventoryItem itemB)
        {
            return TryResolveRecipe(itemA, itemB, out SynthesisRecipeData recipe, out _) ? recipe : null;
        }

        public SynthesisResult TrySynthesize(InventoryItem itemA, InventoryItem itemB)
        {
            if (!ResolveInventoryService())
            {
                return Finish(SynthesisResult.CreateFailure("未找到背包服务，无法执行合成。", itemA, itemB));
            }

            if (recipeDatabase == null)
            {
                return Finish(SynthesisResult.CreateFailure("未配置合成配方库。", itemA, itemB));
            }

            if (fishDatabase == null)
            {
                return Finish(SynthesisResult.CreateFailure("未配置鱼类数据库。", itemA, itemB));
            }

            if (!TryResolveRecipe(itemA, itemB, out SynthesisRecipeData recipe, out string resolveMessage))
            {
                return Finish(SynthesisResult.CreateFailure(resolveMessage, itemA, itemB));
            }

            if (!HasEnoughMaterials(itemA, itemB))
            {
                return Finish(SynthesisResult.CreateFailure("背包中的材料数量不足，无法完成合成。", itemA, itemB, recipe));
            }

            Dictionary<ItemData, int> removalPlan = BuildRemovalPlan(itemA, itemB);
            List<KeyValuePair<ItemData, int>> removedEntries = new List<KeyValuePair<ItemData, int>>();

            foreach (KeyValuePair<ItemData, int> entry in removalPlan)
            {
                if (!inventoryService.RemoveItem(entry.Key, entry.Value))
                {
                    RollbackRemovedItems(removedEntries);
                    return Finish(SynthesisResult.CreateFailure("扣除合成材料失败，已取消本次合成。", itemA, itemB, recipe));
                }

                removedEntries.Add(entry);
            }

            if (!inventoryService.AddItem(recipe.outputItem, 1))
            {
                RollbackRemovedItems(removedEntries);
                return Finish(SynthesisResult.CreateFailure("背包空间不足或结果物品无效，合成失败。", itemA, itemB, recipe));
            }

            string successMessage = string.IsNullOrWhiteSpace(recipe.recipeName)
                ? "合成成功。"
                : "合成成功：" + recipe.recipeName;

            return Finish(SynthesisResult.CreateSuccess(successMessage, itemA, itemB, recipe, 1));
        }

        private bool TryResolveRecipe(InventoryItem itemA, InventoryItem itemB, out SynthesisRecipeData recipe, out string message)
        {
            recipe = null;

            if (itemA == null || itemA.itemData == null || itemB == null || itemB.itemData == null)
            {
                message = "请先放入两条可用于合成的鱼。";
                return false;
            }

            if (recipeDatabase == null)
            {
                message = "未配置合成配方库。";
                return false;
            }

            if (fishDatabase == null)
            {
                message = "未配置鱼类数据库。";
                return false;
            }

            if (!fishDatabase.TryGetFishByItemData(itemA.itemData, out FishData fishA))
            {
                message = "材料 A 不是可识别的鱼类物品。";
                return false;
            }

            if (!fishDatabase.TryGetFishByItemData(itemB.itemData, out FishData fishB))
            {
                message = "材料 B 不是可识别的鱼类物品。";
                return false;
            }

            if (!fishA.canSynthesize || !fishB.canSynthesize)
            {
                message = "所选鱼类当前不支持合成。";
                return false;
            }

            recipe = recipeDatabase.FindRecipe(fishA, fishB);
            if (recipe == null)
            {
                message = "当前组合没有固定合成结果。";
                return false;
            }

            if (recipe.outputItem == null)
            {
                message = "配方缺少输出物品配置。";
                return false;
            }

            message = string.Empty;
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

        private bool HasEnoughMaterials(InventoryItem itemA, InventoryItem itemB)
        {
            if (itemA == null || itemB == null || itemA.itemData == null || itemB.itemData == null || inventoryService == null)
            {
                return false;
            }

            Dictionary<ItemData, int> removalPlan = BuildRemovalPlan(itemA, itemB);
            foreach (KeyValuePair<ItemData, int> entry in removalPlan)
            {
                if (inventoryService.GetItemCount(entry.Key) < entry.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<ItemData, int> BuildRemovalPlan(InventoryItem itemA, InventoryItem itemB)
        {
            Dictionary<ItemData, int> removalPlan = new Dictionary<ItemData, int>();
            AddRequirement(removalPlan, itemA != null ? itemA.itemData : null);
            AddRequirement(removalPlan, itemB != null ? itemB.itemData : null);
            return removalPlan;
        }

        private static void AddRequirement(Dictionary<ItemData, int> removalPlan, ItemData itemData)
        {
            if (itemData == null)
            {
                return;
            }

            removalPlan.TryGetValue(itemData, out int currentValue);
            removalPlan[itemData] = currentValue + 1;
        }

        private void RollbackRemovedItems(List<KeyValuePair<ItemData, int>> removedEntries)
        {
            foreach (KeyValuePair<ItemData, int> entry in removedEntries)
            {
                inventoryService.AddItem(entry.Key, entry.Value);
            }
        }

        private SynthesisResult Finish(SynthesisResult result)
        {
            OnSynthesisFinished?.Invoke(result);
            return result;
        }
    }
}
