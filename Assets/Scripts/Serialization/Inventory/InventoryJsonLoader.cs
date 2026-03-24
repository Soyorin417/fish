using System.Collections.Generic;
using System.IO;
using Game.Inventory;
using Game.Inventory.Interface;
using UnityEngine;
using UnityEngine.Serialization;

public class InventoryJsonLoader : MonoBehaviour
{
    public static InventoryJsonLoader Instance { get; private set; }

    [FormerlySerializedAs("inventoryManagerSource")]
    [SerializeField] private MonoBehaviour inventoryServiceSource;

    public List<ItemData> itemDatabase = new List<ItemData>();

    private IInventoryService inventoryService;
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
                ItemData data = FindItem(item.itemId);

                if (data == null)
                {
                    Debug.LogWarning("Item not found in database: " + item.itemId);
                    continue;
                }

                inventoryService.AddItem(data, item.amount);
            }
        }
        finally
        {
            suppressAutoSave = false;
        }

        Debug.Log("Inventory JSON loaded.");
    }

    public void Save()
    {
        if (!ResolveInventoryService())
        {
            Debug.LogError("No inventory service available for saving.");
            return;
        }

        InventoryJsonRoot root = new InventoryJsonRoot();

        foreach (InventoryItem invItem in inventoryService.Items)
        {
            if (invItem == null || invItem.itemData == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(invItem.itemData.itemId))
            {
                Debug.LogWarning("Skipping item with empty itemId: " + invItem.itemData.name);
                continue;
            }

            root.items.Add(new InventoryItemJson
            {
                itemId = invItem.itemData.itemId,
                amount = invItem.amount
            });
        }

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

    private ItemData FindItem(string id)
    {
        foreach (ItemData item in itemDatabase)
        {
            if (item != null && item.itemId == id)
            {
                return item;
            }
        }

        return null;
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
            return true;
        }

        foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (behaviour is IInventoryService service)
            {
                inventoryServiceSource = behaviour;
                inventoryService = service;
                return true;
            }
        }

        return false;
    }
}
