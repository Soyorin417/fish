using Game.Inventory;
using Game.Inventory.Impl;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InventoryJsonLoader : MonoBehaviour
{
    public List<ItemData> itemDatabase = new List<ItemData>();



    private void Start()
    {
        Load();
    }

    public void Load()
    {
        string path = Path.Combine(Application.streamingAssetsPath,
            "Config/Inventory.json");

        if (!File.Exists(path))
        {
            Debug.LogError("背包JSON不存在: " + path);
            return;
        }

        string json = File.ReadAllText(path);

        InventoryJsonRoot root =
            JsonUtility.FromJson<InventoryJsonRoot>(json);

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

    ItemData FindItem(string id)
    {
        foreach (var item in itemDatabase)
        {
            if (item.itemId == id)
                return item;
        }
        return null;
    }
}