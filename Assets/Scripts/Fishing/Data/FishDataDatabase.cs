using System;
using System.Collections.Generic;
using System.IO;
using Game.Inventory;
using UnityEngine;
using Game.Fishing.Spots;

namespace Game.Fishing.Data
{
    [CreateAssetMenu(fileName = "FishDataDatabase", menuName = "Game/Fishing/Fish Database")]
    public class FishDataDatabase : ScriptableObject
    {
        [SerializeField] private List<FishData> allFish = new List<FishData>();
        private readonly Dictionary<string, FishData> fishById = new Dictionary<string, FishData>();
        private readonly Dictionary<string, FishData> fishByItemId = new Dictionary<string, FishData>();

        private static FishDataDatabase defaultDatabase;
        private static readonly Dictionary<string, FishData> staticFishById = new Dictionary<string, FishData>();
        private static bool staticCacheInitialized;
        private bool instanceCacheInitialized;

        public IReadOnlyList<FishData> AllFish => allFish;

        private void OnEnable()
        {
            RebuildInstanceCache();
            RegisterAsDefaultDatabase();
        }

        private void OnValidate()
        {
            RebuildInstanceCache();
            RegisterAsDefaultDatabase();
        }

        public static FishData FindById(string fishId)
        {
            if (string.IsNullOrWhiteSpace(fishId))
            {
                return null;
            }

            EnsureStaticCache();
            staticFishById.TryGetValue(fishId, out FishData fishData);

            if (fishData == null)
            {
                Debug.LogWarning("FishDataDatabase could not find FishData for fishId=" + fishId);
            }

            return fishData;
        }

        public FishData GetFishByItemData(ItemDataRuntime itemData)
        {
            TryGetFishByItemData(itemData, out FishData fishData);
            return fishData;
        }

        public bool TryGetFishByItemData(ItemDataRuntime itemData, out FishData fishData)
        {
            fishData = null;
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.itemId))
            {
                return false;
            }

            EnsureInstanceCache();
            return fishByItemId.TryGetValue(itemData.itemId, out fishData);
        }

        private void EnsureInstanceCache()
        {
            if (instanceCacheInitialized)
            {
                return;
            }

            RebuildInstanceCache();
        }

        private void RebuildInstanceCache()
        {
            instanceCacheInitialized = true;
            fishById.Clear();
            fishByItemId.Clear();

            if (allFish == null)
            {
                return;
            }

            foreach (FishData fish in allFish)
            {
                if (fish == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(fish.fishId))
                {
                    if (fishById.ContainsKey(fish.fishId))
                    {
                        Debug.LogWarning("FishDataDatabase duplicate fishId: " + fish.fishId);
                    }
                    else
                    {
                        fishById.Add(fish.fishId, fish);
                    }
                }

                if (fish.inventoryItem == null || string.IsNullOrWhiteSpace(fish.inventoryItem.itemId))
                {
                    continue;
                }

                if (fishByItemId.ContainsKey(fish.inventoryItem.itemId))
                {
                    Debug.LogWarning("FishDataDatabase duplicate inventory itemId: " + fish.inventoryItem.itemId);
                    continue;
                }

                fishByItemId.Add(fish.inventoryItem.itemId, fish);
            }
        }

        private void RegisterAsDefaultDatabase()
        {
            if (defaultDatabase == null || defaultDatabase == this)
            {
                defaultDatabase = this;
                SyncStaticCacheFromInstance();
            }
        }

        private void SyncStaticCacheFromInstance()
        {
            staticCacheInitialized = true;
            staticFishById.Clear();

            foreach (KeyValuePair<string, FishData> pair in fishById)
            {
                staticFishById[pair.Key] = pair.Value;
            }
        }

        private static void EnsureStaticCache()
        {
            if (staticCacheInitialized)
            {
                return;
            }

            staticCacheInitialized = true;
            staticFishById.Clear();

            FishDataDatabase database = GetDefaultDatabase();
            if (database != null)
            {
                database.EnsureInstanceCache();

                foreach (KeyValuePair<string, FishData> pair in database.fishById)
                {
                    staticFishById[pair.Key] = pair.Value;
                }

                Debug.Log("FishDataDatabase static cache loaded count: " + staticFishById.Count);
                return;
            }

            FishData[] loadedFish = Resources.FindObjectsOfTypeAll<FishData>();
            foreach (FishData fish in loadedFish)
            {
                if (fish == null || string.IsNullOrWhiteSpace(fish.fishId))
                {
                    continue;
                }

                if (staticFishById.ContainsKey(fish.fishId))
                {
                    Debug.LogWarning("FishDataDatabase duplicate loaded fishId: " + fish.fishId);
                    continue;
                }

                staticFishById.Add(fish.fishId, fish);
            }

            if (staticFishById.Count == 0)
            {
                Debug.LogError("FishDataDatabase static cache is empty. Assign or load a FishDataDatabase before fishing by spot config.");
                return;
            }

            Debug.LogWarning("FishDataDatabase static cache loaded from currently loaded FishData assets. Count=" + staticFishById.Count);
        }

        private static FishDataDatabase GetDefaultDatabase()
        {
            if (defaultDatabase != null)
            {
                return defaultDatabase;
            }

            FishDataDatabase[] databases = Resources.FindObjectsOfTypeAll<FishDataDatabase>();
            if (databases != null && databases.Length > 0)
            {
                defaultDatabase = databases[0];
            }

            return defaultDatabase;
        }
    }

    [Serializable]
    public class FishingSpotConfigRoot
    {
        public List<FishingSpotConfig> spots = new List<FishingSpotConfig>();
    }

    [Serializable]
    public class FishingSpotConfig
    {
        public string spotId;
        public string spotName;
        public int recommendedRodLevel;
        public List<FishingSpotEntry> entries = new List<FishingSpotEntry>();
    }

    [Serializable]
    public class FishingSpotEntry
    {
        public string fishId;
        public int weight;
    }

    public static class FishingSpotConfigDatabase
    {
        private static readonly Dictionary<string, FishingSpotConfig> cache = new Dictionary<string, FishingSpotConfig>();
        private static bool initialized;

        private static string ConfigPath =>
            Path.Combine(Application.streamingAssetsPath, "Config/fishing_spots.json");

        public static FishingSpotConfig FindById(string spotId)
        {
            if (string.IsNullOrWhiteSpace(spotId))
            {
                Debug.LogWarning("FishingSpotConfigDatabase.FindById received an empty spotId.");
                return null;
            }

            EnsureInit();
            cache.TryGetValue(spotId, out FishingSpotConfig config);

            if (config == null)
            {
                Debug.LogWarning("FishingSpotConfigDatabase could not find config for spotId=" + spotId);
            }

            return config;
        }

        public static FishData RollFish(FishingSpot spot)
        {
            if (spot == null)
            {
                Debug.LogWarning("FishingSpotConfigDatabase.RollFish received a null FishingSpot.");
                return null;
            }

            FishingSpotConfig config = spot.GetConfig();
            if (config == null)
            {
                Debug.LogWarning("FishingSpotConfigDatabase.RollFish could not resolve config for spotId=" + spot.SpotId);
                return null;
            }

            if (config.entries == null || config.entries.Count == 0)
            {
                Debug.LogWarning("FishingSpotConfigDatabase.RollFish found no entries for spotId=" + config.spotId);
                return null;
            }

            int totalWeight = 0;
            foreach (FishingSpotEntry entry in config.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.fishId) || entry.weight <= 0)
                {
                    continue;
                }

                totalWeight += entry.weight;
            }

            if (totalWeight <= 0)
            {
                Debug.LogWarning("FishingSpotConfigDatabase.RollFish found no valid weights for spotId=" + config.spotId);
                return null;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int current = 0;

            foreach (FishingSpotEntry entry in config.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.fishId) || entry.weight <= 0)
                {
                    continue;
                }

                current += entry.weight;
                if (roll >= current)
                {
                    continue;
                }

                FishData fishData = FishDataDatabase.FindById(entry.fishId);
                if (fishData == null)
                {
                    Debug.LogError(
                        "FishingSpotConfigDatabase.RollFish selected fishId=" + entry.fishId +
                        " for spotId=" + config.spotId +
                        " but FishDataDatabase could not resolve it.");
                }

                return fishData;
            }

            Debug.LogWarning("FishingSpotConfigDatabase.RollFish failed to resolve a fish for spotId=" + config.spotId);
            return null;
        }

        public static void Reload()
        {
            initialized = false;
            cache.Clear();
            EnsureInit();
        }

        private static void EnsureInit()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            cache.Clear();

            if (!File.Exists(ConfigPath))
            {
                Debug.LogError("Fishing spot config json not found: " + ConfigPath);
                return;
            }

            string json = File.ReadAllText(ConfigPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("Fishing spot config json is empty: " + ConfigPath);
                return;
            }

            FishingSpotConfigRoot root = JsonUtility.FromJson<FishingSpotConfigRoot>(json);
            if (root == null || root.spots == null)
            {
                Debug.LogError("Fishing spot config json parse failed: " + ConfigPath);
                return;
            }

            foreach (FishingSpotConfig config in root.spots)
            {
                if (config == null || string.IsNullOrWhiteSpace(config.spotId))
                {
                    Debug.LogWarning("FishingSpotConfigDatabase skipped an invalid spot config entry.");
                    continue;
                }

                if (cache.ContainsKey(config.spotId))
                {
                    Debug.LogWarning("FishingSpotConfigDatabase duplicate spotId: " + config.spotId);
                    continue;
                }

                cache.Add(config.spotId, config);
            }

            Debug.Log("FishingSpotConfigDatabase loaded spot count: " + cache.Count);
        }
    }
}
