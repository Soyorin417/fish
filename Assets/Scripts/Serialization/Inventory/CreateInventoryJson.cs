using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateInventoryJson : MonoBehaviour
{
    private void Start()
    {
        InventoryJsonRoot root = new InventoryJsonRoot();

        InventoryItemJson item1 = new InventoryItemJson();
        item1.itemId = "fish_carp";
        item1.amount = 3;
        root.items.Add(item1);

        InventoryItemJson item2 = new InventoryItemJson();
        item2.itemId = "fish_salmon";
        item2.amount = 1;
        root.items.Add(item2);

        InventoryItemJson item3 = new InventoryItemJson();
        item3.itemId = "bait_basic";
        item3.amount = 10;
        root.items.Add(item3);

        var basePath = Path.Combine(Application.streamingAssetsPath, "Config");

        DirectoryInfo dir = new DirectoryInfo(basePath);
        if (!dir.Exists)
            dir.Create();

        var configPath = Path.Combine(basePath, "Inventory.json");

        FileInfo fileInfo = new FileInfo(configPath);
        if (fileInfo.Exists)
            fileInfo.Delete();

        StreamWriter writer = fileInfo.CreateText();
        writer.Write(JsonUtility.ToJson(root, true));
        writer.Flush();
        writer.Dispose();
        writer.Close();

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

        Debug.Log("背包JSON生成完成: " + configPath);
    }
}