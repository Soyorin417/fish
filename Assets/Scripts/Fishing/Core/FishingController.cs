using Game.Fishing.Data;
using Game.Inventory;
using Game.Inventory.Interface;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Game.Fishing.Core
{
    public class FishingController : MonoBehaviour, IFishingResultHandler
    {
        public enum FishingState
        {
            Idle,
            WaitingForBite,
            PlayingMiniGame,
            Result
        }

        [Header("交互")]
        public KeyCode fishKey = KeyCode.E;
        public Transform player;
        public float interactDistance = 3f;

        [Header("UI")]
        public GameObject fishingCanvas;
        public TMP_Text hintText;
        public TMP_Text resultText;

        [Header("MiniGame UI")]
        public Image progressFill;

        [Header("等待设置")]
        public float waitMinTime = 1.5f;
        public float waitMaxTime = 3.5f;

        [Header("小游戏设置")]
        public float miniGameDuration = 8f;
        public float successNeed = 1f;
        public float progressLosePerSecond = 0.25f;
        public float progressGainPerPress = 0.18f;

        [Header("掉落")]
        public FishingLootTable lootTable;

        [Header("背包服务")]
        [SerializeField] private MonoBehaviour inventoryServiceSource;

        private IInventoryService inventoryService;
        private FishingState state = FishingState.Idle;
        private Coroutine fishingRoutine;
        private float miniGameTimer;
        private float progressValue;

        private void Awake()
        {
            inventoryService = inventoryServiceSource as IInventoryService;

            if (inventoryService == null && inventoryServiceSource != null)
            {
                Debug.LogError("inventoryServiceSource 没有实现 IInventoryService 接口！");
            }
        }

        private void Start()
        {
            if (fishingCanvas != null)
                fishingCanvas.SetActive(false);

            SetHint("按 E 开始钓鱼");
            SetResult("");
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

                case FishingState.WaitingForBite:
                    // 这里等协程，不需要额外输入
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

        public void StartFishing()
        {
            if (state != FishingState.Idle) return;

            state = FishingState.WaitingForBite;

            if (fishingCanvas != null)
                fishingCanvas.SetActive(true);

            SetHint("等待鱼上钩...");
            SetResult("");
            SetProgress(0f);

            if (fishingRoutine != null)
                StopCoroutine(fishingRoutine);

            fishingRoutine = StartCoroutine(WaitForBiteRoutine());
        }

        private IEnumerator WaitForBiteRoutine()
        {
            float waitTime = Random.Range(waitMinTime, waitMaxTime);
            yield return new WaitForSeconds(waitTime);

            EnterMiniGame();
        }

        private void EnterMiniGame()
        {
            state = FishingState.PlayingMiniGame;
            miniGameTimer = miniGameDuration;
            progressValue = 0f;

            SetHint("连按 E 收杆！");
            SetResult("");
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
                FishData fish = RollFish();
                HandleFishResult(fish, 1);
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
                Debug.LogWarning("没有配置 FishingLootTable");
                return null;
            }

            return lootTable.Roll();
        }

        public void OnFishingSuccess(ItemData fishItem, int amount)
        {
            state = FishingState.Result;

            bool added = false;
            if (fishItem != null && inventoryService != null)
            {
                added = inventoryService.AddItem(fishItem, amount);
            }

            if (fishItem == null)
            {
                SetResult("钓鱼成功，但没有配置鱼数据");
            }
            else if (added)
            {
                SetResult("钓到了：" + fishItem.itemName + " x" + amount);
            }
            else
            {
                SetResult("钓到了：" + fishItem.itemName + "，但背包已满");
            }

            SetHint("按 E 关闭");
        }

        public void HandleFishResult(FishData fishData, int amount = 1)
        {
            if (fishData == null)
            {
                Debug.LogWarning("FishData 为空");
                return;
            }

            OnFishingSuccess(fishData.inventoryItem, amount);
        }

        public void OnFishingFail()
        {
            state = FishingState.Result;
            SetResult("鱼跑掉了");
            SetHint("按 E 关闭");
        }

        private void ExitFishing()
        {
            state = FishingState.Idle;

            if (fishingCanvas != null)
                fishingCanvas.SetActive(false);

            SetHint("按 E 开始钓鱼");
            SetResult("");
            SetProgress(0f);
        }

        private void SetHint(string text)
        {
            if (hintText != null)
                hintText.text = text;
        }

        private void SetResult(string text)
        {
            if (resultText != null)
                resultText.text = text;
        }

        private void SetProgress(float value)
        {
            if (progressFill != null)
                progressFill.fillAmount = value;
        }
    }
}

