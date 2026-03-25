using System;
using System.Collections.Generic;
using System.IO;
using Game.Inventory;
using Game.Serialization;
using TMPro;
using UnityEngine;

namespace Game.Gacha
{
    public class FishProbabilityPool : MonoBehaviour
    {
        [SerializeField] public bool loadOnAwake = true;
        [SerializeField] public TMP_Text debugText;
        [SerializeField] public string jsonFileName = "fish_probability.json";

        private readonly List<FishProbability> allEntries = new List<FishProbability>();
        private readonly List<FishProbability> rollableEntries = new List<FishProbability>();
        private readonly Dictionary<string, FishProbability> entryById = new Dictionary<string, FishProbability>(StringComparer.Ordinal);

        private bool loaded;
        private int totalWeight;
        private string jsonPath;

        public IReadOnlyList<FishProbability> RollableEntries => rollableEntries;

        private void Awake()
        {
            if (loadOnAwake)
            {
                Load();
            }
        }

        public bool EnsureLoaded()
        {
            if (loaded)
            {
                return rollableEntries.Count > 0;
            }

            Load();
            return rollableEntries.Count > 0;
        }

        public void Load()
        {
            loaded = true;
            allEntries.Clear();
            rollableEntries.Clear();
            entryById.Clear();
            totalWeight = 0;

            jsonPath = Path.Combine(Application.streamingAssetsPath, "Luban", jsonFileName);
            if (!File.Exists(jsonPath))
            {
                Warn("FishProbabilityPool json not found: " + jsonPath);
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(jsonPath);
            }
            catch (Exception ex)
            {
                Warn("FishProbabilityPool failed to read json: " + jsonPath + "\n" + ex.Message);
                return;
            }

            List<FishProbability> parsedEntries = ParseJson(json);
            if (parsedEntries == null || parsedEntries.Count == 0)
            {
                Warn("FishProbabilityPool parsed no entries from: " + jsonPath);
                return;
            }

            foreach (FishProbability entry in parsedEntries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.fishId))
                {
                    Warn("FishProbabilityPool skipped an invalid probability row in: " + jsonPath);
                    continue;
                }

                if (entryById.ContainsKey(entry.fishId))
                {
                    Warn("FishProbabilityPool duplicate fishId detected, keeping first: " + entry.fishId);
                    continue;
                }

                allEntries.Add(entry);
                entryById.Add(entry.fishId, entry);

                if (entry.fishId == "fish_025")
                {
                    Warn("FishProbabilityPool skipped ticket fish fish_025 from probability pool.");
                    continue;
                }

                if (!IsAllowedResultFishId(entry.fishId))
                {
                    Warn("FishProbabilityPool skipped non-rollable fishId: " + entry.fishId);
                    continue;
                }

                if (entry.weight <= 0)
                {
                    Warn("FishProbabilityPool skipped non-positive weight fishId=" + entry.fishId + " weight=" + entry.weight);
                    continue;
                }

                rollableEntries.Add(entry);
                totalWeight += entry.weight;
            }

            if (rollableEntries.Count == 0)
            {
                Warn("FishProbabilityPool has no rollable fish entries after filtering. Path=" + jsonPath);
                return;
            }

            DebugLog("FishProbabilityPool loaded rollableCount=" + rollableEntries.Count + " totalWeight=" + totalWeight);
        }

        public string RollFishId()
        {
            FishProbability entry = RollEntry();
            return entry != null ? entry.fishId : null;
        }

        public FishProbability RollEntry()
        {
            if (!EnsureLoaded())
            {
                Warn("FishProbabilityPool.RollEntry failed because the pool is empty. Path=" + jsonPath);
                return null;
            }

            if (totalWeight <= 0)
            {
                Warn("FishProbabilityPool.RollEntry failed because totalWeight <= 0. Path=" + jsonPath);
                return null;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int current = 0;

            foreach (FishProbability entry in rollableEntries)
            {
                current += entry.weight;
                if (roll < current)
                {
                    return entry;
                }
            }

            return rollableEntries.Count > 0 ? rollableEntries[rollableEntries.Count - 1] : null;
        }

        public FishProbability FindById(string fishId)
        {
            if (string.IsNullOrWhiteSpace(fishId))
            {
                return null;
            }

            EnsureLoaded();
            entryById.TryGetValue(fishId, out FishProbability entry);
            return entry;
        }

        public string GetFishName(string fishId)
        {
            FishProbability entry = FindById(fishId);
            if (entry != null && !string.IsNullOrWhiteSpace(entry.fishName))
            {
                return entry.fishName;
            }

            ItemDataRuntime itemData = ItemDatabaseRuntime.FindById(fishId);
            if (itemData != null && !string.IsNullOrWhiteSpace(itemData.itemName))
            {
                return itemData.itemName;
            }

            return fishId;
        }

        public int GetRarityLevel(string fishId)
        {
            FishProbability entry = FindById(fishId);
            return entry != null ? entry.rarityLevel : 1;
        }

        private List<FishProbability> ParseJson(string json)
        {
            List<FishProbability> parsed = JsonArrayHelper.FromJsonArray<FishProbability>(json);
            if (parsed != null && parsed.Count > 0)
            {
                return parsed;
            }

            RootWrapper root = JsonUtility.FromJson<RootWrapper>(json);
            if (root != null)
            {
                if (root.dataList != null && root.dataList.Count > 0)
                {
                    return root.dataList;
                }

                if (root.items != null && root.items.Count > 0)
                {
                    return root.items;
                }

                if (root.probabilities != null && root.probabilities.Count > 0)
                {
                    return root.probabilities;
                }
            }

            return new List<FishProbability>();
        }

        private static bool IsAllowedResultFishId(string fishId)
        {
            if (string.IsNullOrWhiteSpace(fishId) || fishId == "fish_025")
            {
                return false;
            }

            if (!fishId.StartsWith("fish_", StringComparison.Ordinal))
            {
                return false;
            }

            string numberPart = fishId.Substring("fish_".Length);
            if (!int.TryParse(numberPart, out int fishNumber))
            {
                return false;
            }

            return fishNumber >= 1 && fishNumber <= 24;
        }

        private void DebugLog(string message)
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

        [Serializable]
        private class RootWrapper
        {
            public List<FishProbability> dataList = new List<FishProbability>();
            public List<FishProbability> items = new List<FishProbability>();
            public List<FishProbability> probabilities = new List<FishProbability>();
        }
    }
}
