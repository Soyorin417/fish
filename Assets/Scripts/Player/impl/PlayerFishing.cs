using Game.Fishing.Core;
using Game.Fishing.Data;
using Game.Fishing.Spots;
using Game.Inventory;
using Game.Inventory.Interface;
using Game.Player;
using System.Collections;
using UnityEngine;

public class PlayerFishing : MonoBehaviour, IFishingController
{
    public KeyCode fishKey = KeyCode.E;
    public KeyCode controlKey = KeyCode.Space;

    [Header("Legacy Fish Table")]
    public FishData[] fishTable;

    [Header("Loot")]
    public FishingLootTable defaultLootTable;

    [Header("UI")]
    public FishingUI fishingUI;
    [SerializeField] private InventoryToggle inventoryToggle;

    [Header("Services")]
    [SerializeField] private MonoBehaviour inventoryServiceSource;

    [Header("Wait Settings")]
    public float waitMinTime = 1.5f;
    public float waitMaxTime = 3.5f;

    [Header("Mini Game")]
    public float miniGameDuration = 999f;

    [Tooltip("Target zone movement speed")]
    public float fishZoneSpeed = 0.02f;

    [Tooltip("Player bar move speed while holding Space")]
    public float playerMoveRightSpeed = 0.1f;

    [Tooltip("Player bar fall speed when Space is released")]
    public float playerFallLeftSpeed = 0.05f;

    [Tooltip("Target zone width (0 to 1)")]
    [Range(0.05f, 0.5f)]
    public float fishZoneWidth = 0.4f;

    [Tooltip("Player bar width (0 to 1)")]
    [Range(0.01f, 0.2f)]
    public float playerBarWidth = 0.20f;

    [Tooltip("Progress gain speed while inside the target zone")]
    public float progressGainPerSecond = 1f;

    [Tooltip("Progress loss speed while outside the target zone")]
    public float progressLosePerSecond = 0.08f;

    [Tooltip("Initial catch progress when the mini game starts")]
    [Range(0f, 1f)]
    public float startProgress = 0.80f;

    [Tooltip("Grace time before progress can start dropping")]
    public float startGraceTime = 3f;

    private float currentGraceTimer;
    private FishingSpot currentSpot;
    private IPlayerControl playerControl;
    private IInventoryService inventoryService;
    private Coroutine fishingRoutine;
    private Coroutine finishRoutine;
    private FishingState state = FishingState.None;

    private float fishZoneCenter = 0.2f;
    private int fishZoneDir = 1;
    private float playerBarCenter;
    private float catchProgress;
    private bool fishingUIModeRequested;
    private bool warnedMissingInventoryToggle;

    public FishingState State => state;

    public bool IsFishing =>
        state == FishingState.WaitingForBite ||
        state == FishingState.PlayingMiniGame ||
        state == FishingState.Success ||
        state == FishingState.Failed;

    private void Awake()
    {
        playerControl = GetComponent<IPlayerControl>();
        inventoryService = inventoryServiceSource as IInventoryService;

        if (inventoryService == null && inventoryServiceSource != null)
        {
            Debug.LogError("inventoryServiceSource does not implement IInventoryService.");
        }
    }

    private void Start()
    {
        ResolveFishingUI();
        ResolveInventoryService();
        ResolveInventoryToggle();

        fishingUI?.HideAll();
        state = currentSpot != null ? FishingState.Ready : FishingState.None;
    }

    private void Update()
    {
        if (state == FishingState.PlayingMiniGame)
        {
            UpdateMiniGame();
            return;
        }

        if (currentSpot == null)
        {
            return;
        }

        if (state == FishingState.Ready && Input.GetKeyDown(fishKey))
        {
            StartFishing();
        }
    }

    public void CancelFishing()
    {
        StopRoutine(ref fishingRoutine);
        StopRoutine(ref finishRoutine);
        ApplyPlayerFishingLock(false);

        if (currentSpot != null)
        {
            state = FishingState.Ready;
            ShowReadyHint();
            return;
        }

        state = FishingState.None;
        fishingUI?.HideAll();
    }

    private void OnDisable()
    {
        StopRoutine(ref fishingRoutine);
        StopRoutine(ref finishRoutine);
        ApplyPlayerFishingLock(false);
        fishingUI?.HideAll();
        state = currentSpot != null ? FishingState.Ready : FishingState.None;
    }

    private void StartFishing()
    {
        if (currentSpot == null || state != FishingState.Ready)
        {
            return;
        }

        StopRoutine(ref finishRoutine);
        ApplyPlayerFishingLock(true);
        state = FishingState.WaitingForBite;
        ShowWaitingForBite();
        fishingRoutine = StartCoroutine(FishingFlow());
    }

    private IEnumerator FishingFlow()
    {
        float waitTime = Random.Range(waitMinTime, waitMaxTime);
        yield return new WaitForSeconds(waitTime);

        if (state != FishingState.WaitingForBite || currentSpot == null)
        {
            yield break;
        }

        StartMiniGame();

        float timer = miniGameDuration;
        while (state == FishingState.PlayingMiniGame && timer > 0f)
        {
            timer -= Time.deltaTime;

            if (catchProgress >= 1f)
            {
                CatchFish();
                yield break;
            }

            if (catchProgress <= 0f)
            {
                FailFishing();
                yield break;
            }

            yield return null;
        }

        if (state == FishingState.PlayingMiniGame)
        {
            FailFishing();
        }
    }

    private void StartMiniGame()
    {
        state = FishingState.PlayingMiniGame;
        fishZoneCenter = 0.2f;
        fishZoneDir = 1;
        playerBarCenter = fishZoneCenter;
        catchProgress = startProgress;
        currentGraceTimer = startGraceTime;

        if (fishingUI != null)
        {
            fishingUI.ShowHint("Hold Space to keep the line inside the target zone");
            fishingUI.ShowMiniGame();
            SyncMiniGameUi();
        }
    }

    private void UpdateMiniGame()
    {
        float dt = Time.deltaTime;

        fishZoneCenter += fishZoneDir * fishZoneSpeed * dt;

        float halfZone = fishZoneWidth * 0.5f;
        if (fishZoneCenter >= 1f - halfZone)
        {
            fishZoneCenter = 1f - halfZone;
            fishZoneDir = -1;
        }
        else if (fishZoneCenter <= halfZone)
        {
            fishZoneCenter = halfZone;
            fishZoneDir = 1;
        }

        if (Input.GetKey(controlKey))
        {
            playerBarCenter += playerMoveRightSpeed * dt;
        }
        else
        {
            playerBarCenter -= playerFallLeftSpeed * dt;
        }

        float halfPlayer = playerBarWidth * 0.5f;
        playerBarCenter = Mathf.Clamp(playerBarCenter, halfPlayer, 1f - halfPlayer);

        float zoneLeft = fishZoneCenter - halfZone;
        float zoneRight = fishZoneCenter + halfZone;
        float playerLeft = playerBarCenter - halfPlayer;
        float playerRight = playerBarCenter + halfPlayer;
        bool inZone = playerRight >= zoneLeft && playerLeft <= zoneRight;

        if (currentGraceTimer > 0f)
        {
            currentGraceTimer -= dt;
            if (inZone)
            {
                catchProgress += progressGainPerSecond * dt;
            }
        }
        else if (inZone)
        {
            catchProgress += progressGainPerSecond * dt;
        }
        else
        {
            catchProgress -= progressLosePerSecond * dt;
        }

        catchProgress = Mathf.Clamp01(catchProgress);
        SyncMiniGameUi();
    }

    private void CatchFish()
    {
        state = FishingState.Success;
        StopRoutine(ref fishingRoutine);

        FishData fish = RollFish();
        string resultMessage = BuildCatchMessage(fish);

        if (fishingUI != null)
        {
            fishingUI.ShowHint(resultMessage);
            fishingUI.HideMiniGame();
        }

        finishRoutine = StartCoroutine(FinishAfterDelay());
    }

    private void FailFishing()
    {
        state = FishingState.Failed;
        StopRoutine(ref fishingRoutine);

        if (fishingUI != null)
        {
            fishingUI.ShowHint("The fish got away...");
            fishingUI.HideMiniGame();
        }

        finishRoutine = StartCoroutine(FinishAfterDelay());
    }

    private IEnumerator FinishAfterDelay()
    {
        yield return new WaitForSeconds(0.8f);
        finishRoutine = null;
        FinishFishingSession();
    }

    private void FinishFishingSession()
    {
        ApplyPlayerFishingLock(false);

        if (currentSpot != null)
        {
            state = FishingState.Ready;
            ShowReadyHint();
            return;
        }

        state = FishingState.None;
        fishingUI?.HideAll();
    }

    private FishData RollFish()
    {
        if (currentSpot != null)
        {
            FishData fishFromSpotConfig = FishingSpotConfigDatabase.RollFish(currentSpot);
            if (fishFromSpotConfig != null)
            {
                return fishFromSpotConfig;
            }

            Debug.LogWarning(
                "PlayerFishing failed to roll fish from spot config for spotId=" + currentSpot.SpotId +
                ". Falling back to legacy loot table or fishTable.");
        }

        FishingLootTable lootTable = defaultLootTable;
        if (lootTable != null)
        {
            return lootTable.Roll();
        }

        return RollFishFromLegacyTable();
    }

    private FishData RollFishFromLegacyTable()
    {
        if (fishTable == null || fishTable.Length == 0)
        {
            return null;
        }

        float total = 0f;
        FishData fallbackFish = null;

        foreach (FishData fish in fishTable)
        {
            if (fish == null)
            {
                continue;
            }

            total += fish.weight;
            if (fallbackFish == null)
            {
                fallbackFish = fish;
            }
        }

        if (fallbackFish == null)
        {
            return null;
        }

        if (total <= 0f)
        {
            return fallbackFish;
        }

        float rand = Random.Range(0f, total);
        float cur = 0f;

        foreach (FishData fish in fishTable)
        {
            if (fish == null)
            {
                continue;
            }

            cur += fish.weight;
            if (rand <= cur)
            {
                return fish;
            }
        }

        return fallbackFish;
    }

    private string BuildCatchMessage(FishData fish)
    {
        if (fish == null)
        {
            return "Caught something, but no FishData is configured.";
        }

        TryAddFishToInventory(fish, 1, out string message);
        return message;
    }

    private bool TryAddFishToInventory(FishData fish, int amount, out string message)
    {
        if (fish == null)
        {
            message = "Caught something, but no FishData is configured.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(fish.fishName))
        {
            fish.fishName = fish.name;
        }

        string itemId = null;
        bool stackable = true;

        if (fish.inventoryItem != null)
        {
            itemId = fish.inventoryItem.itemId;
            stackable = fish.inventoryItem.stackable;
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = fish.fishId;
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            message = "Caught " + fish.fishName + ", but no inventory item id is configured.";
            return false;
        }

        ResolveInventoryService();
        if (inventoryService == null)
        {
            message = "Caught " + fish.fishName + ", but no inventory service is available.";
            return false;
        }

        ItemDataRuntime itemData = ItemDatabaseRuntime.FindById(itemId);
        if (itemData != null)
        {
            stackable = itemData.stackable;
        }
        else
        {
            Debug.LogWarning(
                "PlayerFishing could not find item config for itemId=" + itemId +
                ". Falling back to fish inventoryItem.stackable=" + stackable + ".");
        }

        int beforeCount = inventoryService.GetItemCount(itemId);

        bool success = inventoryService.AddItem(itemId, amount, stackable);
        int afterCount = inventoryService.GetItemCount(itemId);

        Debug.Log(
            "PlayerFishing TryAddFishToInventory itemId=" + itemId +
            " amount=" + amount +
            " stackable=" + stackable +
            " beforeCount=" + beforeCount +
            " afterCount=" + afterCount +
            " success=" + success);

        message = success
            ? "Caught " + fish.fishName
            : "Caught " + fish.fishName + ", but the inventory is full.";
        return success;
    }

    private void ResolveFishingUI()
    {
        if (fishingUI != null)
        {
            return;
        }

        FishingUIBinder binder = FindObjectOfType<FishingUIBinder>(true);
        if (binder != null)
        {
            fishingUI = binder.fishingUI;
        }

        if (fishingUI == null)
        {
            fishingUI = FindObjectOfType<FishingUI>(true);
        }

        if (fishingUI == null)
        {
            Debug.LogError("FishingUI was not found. Check that a FishingUI component exists in the scene.");
        }
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

    private void ApplyPlayerFishingLock(bool locked)
    {
        if (playerControl != null)
        {
            playerControl.PlayFishingAnimation(locked);
        }

        if (locked == fishingUIModeRequested)
        {
            return;
        }

        ResolveInventoryToggle();
        if (inventoryToggle != null)
        {
            if (locked)
            {
                inventoryToggle.PushUIMode();
            }
            else if (inventoryToggle.isActiveAndEnabled)
            {
                inventoryToggle.PopUIMode();
            }

            fishingUIModeRequested = locked;
            return;
        }

        if (!warnedMissingInventoryToggle)
        {
            Debug.LogError("PlayerFishing could not find InventoryToggle. UI/player state can not be synchronized correctly.");
            warnedMissingInventoryToggle = true;
        }
        fishingUIModeRequested = locked;
    }

    private void ShowWaitingForBite()
    {
        if (fishingUI == null)
        {
            return;
        }

        fishingUI.ShowHint("Waiting for a bite...");
        fishingUI.HideMiniGame();
    }

    private void ShowReadyHint()
    {
        if (fishingUI == null)
        {
            return;
        }

        fishingUI.ShowHint("Press E to start fishing");
        fishingUI.HideMiniGame();
    }

    private void SyncMiniGameUi()
    {
        if (fishingUI == null)
        {
            return;
        }

        fishingUI.SetFishZonePosition(fishZoneCenter);
        fishingUI.SetPlayerBarPosition(playerBarCenter);
        fishingUI.SetProgress(catchProgress);
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        FishingSpot spot = GetSpotFromCollider(other);
        if (spot == null)
        {
            return;
        }

        currentSpot = spot;

        if (!IsFishing)
        {
            state = FishingState.Ready;
            ShowReadyHint();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        FishingSpot spot = GetSpotFromCollider(other);
        if (spot == null || spot != currentSpot)
        {
            return;
        }

        currentSpot = null;

        if (IsFishing)
        {
            CancelFishing();
        }
        else
        {
            state = FishingState.None;
            fishingUI?.HideAll();
        }
    }

    private static FishingSpot GetSpotFromCollider(Collider other)
    {
        FishingSpot spot = other.GetComponent<FishingSpot>();
        if (spot == null)
        {
            spot = other.GetComponentInParent<FishingSpot>();
        }

        return spot;
    }

    private void ResolveInventoryToggle()
    {
        if (inventoryToggle == null)
        {
            inventoryToggle = FindObjectOfType<InventoryToggle>(true);
        }
    }
}
