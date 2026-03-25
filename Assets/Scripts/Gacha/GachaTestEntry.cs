using Game.Inventory;
using Game.Inventory.Interface;
using TMPro;
using UnityEngine;

namespace Game.Gacha
{
    public class GachaTestEntry : MonoBehaviour
    {
        [SerializeField] public GachaRollController rollController;
        [SerializeField] public bool testMode = true;
        [SerializeField] public string ticketFishId = "fish_025";
        [SerializeField] private MonoBehaviour inventoryServiceProvider;
        [SerializeField] private TMP_Text debugText;

        private IInventoryService inventoryService;

        private void Awake()
        {
            ResolveRollController();
            ResolveInventoryService();

            if (rollController != null)
            {
                rollController.onRollCompleted -= HandleRollCompleted;
                rollController.onRollCompleted += HandleRollCompleted;
            }
        }

        private void OnDestroy()
        {
            if (rollController != null)
            {
                rollController.onRollCompleted -= HandleRollCompleted;
            }
        }

        public void TryUseTicket()
        {
            ResolveRollController();
            if (rollController == null)
            {
                Warn("GachaTestEntry.TryUseTicket failed because rollController is not assigned.");
                return;
            }

            if (testMode)
            {
                LogLine("Consume ticket " + ticketFishId + " x1");
                rollController.PlayRandomRoll();
                return;
            }

            if (!ResolveInventoryService())
            {
                Warn("GachaTestEntry.TryUseTicket failed because no inventory service is available.");
                return;
            }

            if (inventoryService.GetItemCount(ticketFishId) <= 0)
            {
                Warn("GachaTestEntry.TryUseTicket found no ticket item: " + ticketFishId);
                return;
            }

            bool removed = inventoryService.RemoveItem(ticketFishId, 1, ResolveStackable(ticketFishId));
            if (!removed)
            {
                Warn("GachaTestEntry.TryUseTicket failed to consume ticket: " + ticketFishId);
                return;
            }

            LogLine("Consume ticket " + ticketFishId + " x1");
            rollController.PlayRandomRoll();
        }

        private void HandleRollCompleted(string resultFishId)
        {
            if (string.IsNullOrWhiteSpace(resultFishId))
            {
                Warn("GachaTestEntry.HandleRollCompleted received an empty resultFishId.");
                return;
            }

            LogLine("Roll result = " + resultFishId);

            if (testMode)
            {
                return;
            }

            if (!ResolveInventoryService())
            {
                Warn("GachaTestEntry could not add result because no inventory service is available.");
                return;
            }

            bool added = inventoryService.AddItem(resultFishId, 1, ResolveStackable(resultFishId));
            if (!added)
            {
                Warn("GachaTestEntry failed to add roll result to inventory: " + resultFishId);
            }
        }

        private void ResolveRollController()
        {
            if (rollController == null)
            {
                rollController = FindObjectOfType<GachaRollController>(true);
            }
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
                Debug.LogWarning("GachaTestEntry inventoryServiceProvider does not implement IInventoryService.");
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

            return false;
        }

        private void LogLine(string message)
        {
            Debug.Log(message);
            if (debugText != null)
            {
                debugText.text = message;
            }
        }

        private void Warn(string message)
        {
            Debug.LogWarning(message);
            if (debugText != null)
            {
                debugText.text = message;
            }
        }

        private static bool ResolveStackable(string itemId)
        {
            ItemDataRuntime itemData = ItemDatabaseRuntime.FindById(itemId);
            return itemData == null || itemData.stackable;
        }
    }
}
