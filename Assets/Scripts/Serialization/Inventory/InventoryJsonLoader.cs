using System.IO;
using Game.Inventory;
using Game.Inventory.Impl;
using Game.Inventory.Interface;
using UnityEngine;
using UnityEngine.Serialization;

public class InventoryJsonLoader : MonoBehaviour
{
    public static InventoryJsonLoader Instance { get; private set; }

    [FormerlySerializedAs("inventoryManagerSource")]
    [SerializeField] private MonoBehaviour inventoryServiceSource;

    private IInventoryService inventoryService;
    private InventoryManager inventoryManager;
    private bool suppressAutoSave;

    private string JsonPath =>
        Path.Combine(Application.streamingAssetsPath, "Config/Inventory.json");

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!ResolveInventoryService())
        {
            Debug.LogError("InventoryJsonLoader could not resolve an IInventoryService.");
            return;
        }

        inventoryService.OnInventoryChanged += HandleInventoryChanged;
        Load();
    }

    private void OnDestroy()
    {
        if (inventoryService != null)
        {
            inventoryService.OnInventoryChanged -= HandleInventoryChanged;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Load()
    {
        if (!ResolveInventoryService())
        {
            Debug.LogError("No inventory service available for loading.");
            return;
        }

        if (!File.Exists(JsonPath))
        {
            Debug.LogWarning("Inventory JSON not found, creating an empty inventory: " + JsonPath);
            Save();
            return;
        }

        string json = File.ReadAllText(JsonPath);
        InventoryJsonRoot root = JsonUtility.FromJson<InventoryJsonRoot>(json);

        if (root == null || root.items == null)
        {
            Debug.LogError("Inventory JSON parse failed.");
            return;
        }

        suppressAutoSave = true;

        try
        {
            inventoryService.ClearInventory();

            foreach (InventoryItemJson item in root.items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemId) || item.amount <= 0)
                {
                    Debug.LogWarning("Skipping invalid inventory JSON item during load.");
                    continue;
                }

                bool stackable = ResolveStackableForLoad(item.itemId);
                Debug.Log(
                    "Inventory Load item itemId=" + item.itemId +
                    " amount=" + item.amount +
                    " resolvedStackable=" + stackable);

                inventoryService.AddItem(item.itemId, item.amount, stackable);
            }

            NormalizeInventory("Load post-process");
        }
        finally
        {
            suppressAutoSave = false;
        }

        Debug.Log("Inventory JSON loaded. Final items: " + GetInventorySummary());
    }

    public void Save()
    {
        if (!ResolveInventoryService())
        {
            Debug.LogError("No inventory service available for saving.");
            return;
        }

        NormalizeInventory("Save pre-serialize");

        InventoryJsonRoot root = new InventoryJsonRoot();

        foreach (InventoryItem invItem in inventoryService.Items)
        {
            if (invItem == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(invItem.itemId))
            {
                Debug.LogWarning("Skipping item with empty itemId.");
                continue;
            }

            root.items.Add(new InventoryItemJson
            {
                itemId = invItem.itemId,
                amount = invItem.amount
            });
        }

        Debug.Log("Inventory Save final items: " + GetInventorySummary());

        string dir = Path.GetDirectoryName(JsonPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonUtility.ToJson(root, true);
        File.WriteAllText(JsonPath, json);

        Debug.Log("Inventory JSON saved: " + JsonPath);
    }

    private void HandleInventoryChanged()
    {
        if (suppressAutoSave)
        {
            return;
        }

        Save();
    }

    private bool ResolveInventoryService()
    {
        if (inventoryService != null)
        {
            return true;
        }

        inventoryService = inventoryServiceSource as IInventoryService;
        if (inventoryService == null && inventoryServiceSource != null)
        {
            Debug.LogError("inventoryServiceSource does not implement IInventoryService.");
        }

        if (inventoryService != null)
        {
            inventoryManager = inventoryService as InventoryManager;
            return true;
        }

        foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (behaviour is IInventoryService service)
            {
                inventoryServiceSource = behaviour;
                inventoryService = service;
                inventoryManager = service as InventoryManager;
                return true;
            }
        }

        return false;
    }

    private bool ResolveStackableForLoad(string itemId)
    {
        ItemDataRuntime itemData = ItemDatabaseRuntime.FindById(itemId);
        if (itemData != null)
        {
            return itemData.stackable;
        }

        Debug.LogWarning(
            "Inventory Load could not find item config for itemId=" + itemId +
            ". Falling back to stackable=true.");
        return true;
    }

    private void NormalizeInventory(string context)
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("Inventory " + context + " skipped manager normalization because the concrete InventoryManager was not found.");
            return;
        }

        bool changed = inventoryManager.NormalizeItems(context, false);
        Debug.Log(
            "Inventory " + context +
            " normalizeChanged=" + changed +
            " summary=" + inventoryManager.GetItemsDebugSummary());
    }

    private string GetInventorySummary()
    {
        if (inventoryManager != null)
        {
            return inventoryManager.GetItemsDebugSummary();
        }

        if (inventoryService == null || inventoryService.Items == null || inventoryService.Items.Count == 0)
        {
            return "(empty)";
        }

        return "entries=" + inventoryService.Items.Count;
    }
}
