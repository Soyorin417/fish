using Game.Fishing.Data;
using Game.Inventory;
using Game.Inventory.Interface;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Fishing.Core
{
    public class FishingController : MonoBehaviour, IFishingController
    {
        [Header("Interaction")]
        public KeyCode fishKey = KeyCode.E;
        public Transform player;
        public float interactDistance = 3f;

        [Header("UI")]
        public GameObject fishingCanvas;
        public TMP_Text hintText;
        public TMP_Text resultText;

        [Header("MiniGame UI")]
        public Image progressFill;

        [Header("Wait Settings")]
        public float waitMinTime = 1.5f;
        public float waitMaxTime = 3.5f;

        [Header("Mini Game Settings")]
        public float miniGameDuration = 8f;
        public float successNeed = 1f;
        public float progressLosePerSecond = 0.25f;
        public float progressGainPerPress = 0.18f;

        [Header("Loot")]
        public FishingLootTable lootTable;

        [Header("Inventory Service")]
        [SerializeField] private MonoBehaviour inventoryServiceSource;

        private IInventoryService inventoryService;
        private FishingState state = FishingState.Idle;
        private Coroutine fishingRoutine;
        private float miniGameTimer;
        private float progressValue;

        public FishingState State => state;

        public bool IsFishing =>
            state == FishingState.WaitingForBite ||
            state == FishingState.PlayingMiniGame ||
            state == FishingState.Result;

        private void Awake()
        {
            inventoryService = inventoryServiceSource as IInventoryService;

            if (inventoryService == null && inventoryServiceSource != null)
            {
                Debug.LogError("inventoryServiceSource does not implement IInventoryService.");
            }
        }

        private void Start()
        {
            ResolveInventoryService();

            if (fishingCanvas != null)
            {
                fishingCanvas.SetActive(false);
            }

            SetHint("Press E to start fishing");
            SetResult(string.Empty);
            SetProgress(0f);
        }

        private void Update()
        {
            switch (state)
            {
                case FishingState.Idle:
                    if (Input.GetKeyDown(fishKey))
                    {
                        StartFishing();
                    }
                    break;

                case FishingState.PlayingMiniGame:
                    UpdateMiniGame();
                    break;

                case FishingState.Result:
                    if (Input.GetKeyDown(fishKey))
                    {
                        ExitFishing();
                    }
                    break;
            }
        }

        public void CancelFishing()
        {
            ExitFishing();
        }

        public void StartFishing()
        {
            if (state != FishingState.Idle)
            {
                return;
            }

            state = FishingState.WaitingForBite;

            if (fishingCanvas != null)
            {
                fishingCanvas.SetActive(true);
            }

            SetHint("Waiting for a bite...");
            SetResult(string.Empty);
            SetProgress(0f);

            if (fishingRoutine != null)
            {
                StopCoroutine(fishingRoutine);
            }

            fishingRoutine = StartCoroutine(WaitForBiteRoutine());
        }

        private IEnumerator WaitForBiteRoutine()
        {
            float waitTime = Random.Range(waitMinTime, waitMaxTime);
            yield return new WaitForSeconds(waitTime);

            if (state == FishingState.WaitingForBite)
            {
                EnterMiniGame();
            }
        }

        private void EnterMiniGame()
        {
            state = FishingState.PlayingMiniGame;
            miniGameTimer = miniGameDuration;
            progressValue = 0f;

            SetHint("Tap E to reel in");
            SetResult(string.Empty);
            SetProgress(progressValue);
        }

        private void UpdateMiniGame()
        {
            miniGameTimer -= Time.deltaTime;
            progressValue -= progressLosePerSecond * Time.deltaTime;
            progressValue = Mathf.Clamp01(progressValue);

            if (Input.GetKeyDown(fishKey))
            {
                progressValue += progressGainPerPress;
                progressValue = Mathf.Clamp01(progressValue);
            }

            SetProgress(progressValue);

            if (progressValue >= successNeed)
            {
                FinishWithFish(RollFish(), 1);
                return;
            }

            if (miniGameTimer <= 0f)
            {
                OnFishingFail();
            }
        }

        private FishData RollFish()
        {
            if (lootTable == null)
            {
                Debug.LogWarning("FishingLootTable is not assigned.");
                return null;
            }

            return lootTable.Roll();
        }

        public void OnFishingFail()
        {
            state = FishingState.Result;
            SetResult("The fish got away.");
            SetHint("Press E to close");
        }

        private void FinishWithFish(FishData fishData, int amount)
        {
            state = FishingState.Result;
            SetResult(BuildCatchMessage(fishData, amount));
            SetHint("Press E to close");
        }

        private void ExitFishing()
        {
            state = FishingState.Idle;

            if (fishingRoutine != null)
            {
                StopCoroutine(fishingRoutine);
                fishingRoutine = null;
            }

            if (fishingCanvas != null)
            {
                fishingCanvas.SetActive(false);
            }

            SetHint("Press E to start fishing");
            SetResult(string.Empty);
            SetProgress(0f);
        }

        private void ResolveInventoryService()
        {
            if (inventoryService != null)
            {
                return;
            }

            inventoryService = inventoryServiceSource as IInventoryService;
            if (inventoryService == null && inventoryServiceSource != null)
            {
                Debug.LogError("inventoryServiceSource does not implement IInventoryService.");
            }

            if (inventoryService != null)
            {
                return;
            }

            foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>(true))
            {
                if (behaviour is IInventoryService service)
                {
                    inventoryServiceSource = behaviour;
                    inventoryService = service;
                    return;
                }
            }
        }

        private string BuildCatchMessage(FishData fishData, int amount)
        {
            if (fishData == null)
            {
                return "Caught something, but no FishData is configured.";
            }

            ItemData fishItem = fishData.inventoryItem;
            if (fishItem == null)
            {
                return "Caught " + fishData.fishName + ", but no inventory item is configured.";
            }

            ResolveInventoryService();
            if (inventoryService == null)
            {
                return "Caught " + fishItem.itemName + ", but no inventory service is available.";
            }

            bool added = inventoryService.AddItem(fishItem, amount);
            if (added)
            {
                return "Caught " + fishItem.itemName + " x" + amount;
            }

            return "Caught " + fishItem.itemName + ", but the inventory is full.";
        }

        private void SetHint(string text)
        {
            if (hintText != null)
            {
                hintText.text = text;
            }
        }

        private void SetResult(string text)
        {
            if (resultText != null)
            {
                resultText.text = text;
            }
        }

        private void SetProgress(float value)
        {
            if (progressFill != null)
            {
                progressFill.fillAmount = value;
            }
        }
    }
}
