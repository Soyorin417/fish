using Game.Inventory;
using Game.Inventory.Impl;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InventoryJsonLoader : MonoBehaviour
{
    public static InventoryJsonLoader Instance { get; private set; }

    public List<ItemData> itemDatabase = new List<ItemData>();

    private string JsonPath =>
        Path.Combine(Application.streamingAssetsPath, "Config/Inventory.json");

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Load();
    }

    public void Load()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager.Instance 为空，无法加载背包");
            return;
        }

        if (!File.Exists(JsonPath))
        {
            Debug.LogWarning("背包JSON不存在，将创建空背包: " + JsonPath);
            Save();
            return;
        }

        string json = File.ReadAllText(JsonPath);
        InventoryJsonRoot root = JsonUtility.FromJson<InventoryJsonRoot>(json);

        if (root == null || root.items == null)
        {
            Debug.LogError("JSON解析失败");
            return;
        }

        InventoryManager.Instance.ClearInventory();

        foreach (var item in root.items)
        {
            ItemData data = FindItem(item.itemId);

            if (data == null)
            {
                Debug.LogWarning("找不到物品: " + item.itemId);
                continue;
            }

            InventoryManager.Instance.AddItem(data, item.amount);
        }

        Debug.Log("背包JSON加载完成");
    }

    public void Save()
    {
        Debug.Log("Save 被调用");
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager.Instance 为空，无法保存背包");
            return;
        }

        InventoryJsonRoot root = new InventoryJsonRoot();

        foreach (var invItem in InventoryManager.Instance.Items)
        {
            if (invItem == null || invItem.itemData == null)
                continue;

            if (string.IsNullOrEmpty(invItem.itemData.itemId))
            {
                Debug.LogWarning("存在 itemId 为空的物品，跳过保存: " + invItem.itemData.name);
                continue;
            }

            root.items.Add(new InventoryItemJson
            {
                itemId = invItem.itemData.itemId,
                amount = invItem.amount
            });
        }

        string path = Path.Combine(Application.streamingAssetsPath, "Config/Inventory.json");
        string dir = Path.GetDirectoryName(path);

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonUtility.ToJson(root, true);
        File.WriteAllText(path, json);

        Debug.Log("背包JSON保存完成: " + path);
    }

    private ItemData FindItem(string id)
    {
        foreach (var item in itemDatabase)
        {
            if (item != null && item.itemId == id)
                return item;
        }
        return null;
    }
}