using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Serialization;

namespace Game.Inventory
{
    public static class ItemDatabaseRuntime
    {
        [System.Serializable]
        private class ItemConfig
        {
            public string id;
            public string name;
            public string icon;
            public string description;
            public bool stackable;
            public int maxStack;
        }

        private static readonly Dictionary<string, ItemDataRuntime> cache = new Dictionary<string, ItemDataRuntime>();
        private static bool initialized;

        public static ItemDataRuntime FindById(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            EnsureInit();
            cache.TryGetValue(itemId, out ItemDataRuntime itemData);
            return itemData;
        }

        private static void EnsureInit()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            cache.Clear();

            string path = Path.Combine(Application.streamingAssetsPath, "Luban/item.json");
            if (!File.Exists(path))
            {
                Debug.LogError("Item config json not found: " + path);
                return;
            }

            string json = File.ReadAllText(path);
            List<ItemConfig> configs = JsonArrayHelper.FromJsonArray<ItemConfig>(json);

            if (configs == null || configs.Count == 0)
            {
                Debug.LogWarning("item.json Îª¿Õ»ò½âÎöÊ§°Ü: " + path);
                return;
            }

            foreach (ItemConfig cfg in configs)
            {
                if (cfg == null || string.IsNullOrWhiteSpace(cfg.id))
                {
                    continue;
                }

                if (cache.ContainsKey(cfg.id))
                {
                    Debug.LogWarning("Duplicate item id in item.json: " + cfg.id);
                    continue;
                }

                cache[cfg.id] = new ItemDataRuntime
                {
                    id = cfg.id,
                    name = cfg.name,
                    icon = cfg.icon,
                    description = cfg.description,
                    stackable = cfg.stackable,
                    maxStack = cfg.maxStack
                };
            }

            Debug.Log("ItemDatabaseRuntime loaded item count: " + cache.Count);
        }
    }
}