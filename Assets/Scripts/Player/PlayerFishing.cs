using System.Collections;
using UnityEngine;

public class PlayerFishing : MonoBehaviour
{
    public KeyCode fishKey = KeyCode.E;
    public KeyCode controlKey = KeyCode.Space;

    [Header("UI")]
    public FishingUI fishingUI;

    [Header("Wait Settings")]
    public float waitMinTime = 1.5f;
    public float waitMaxTime = 3.5f;

    [Header("Mini Game")]
    public float miniGameDuration = 999f;

    [Tooltip("目标判定区移动速度")]
    public float fishZoneSpeed = 0.02f;

    [Tooltip("玩家条按住空格向右移动速度")]
    public float playerMoveRightSpeed = 0.1f;

    [Tooltip("玩家条不按时向左回落速度")]
    public float playerFallLeftSpeed = 0.05f;

    [Tooltip("判定区宽度（0~1）")]
    [Range(0.05f, 0.5f)]
    public float fishZoneWidth = 0.4f;

    [Tooltip("玩家条宽度（0~1）")]
    [Range(0.01f, 0.2f)]
    public float playerBarWidth = 0.20f;

    [Tooltip("在判定区内时，进度增长速度")]
    public float progressGainPerSecond = 1f;

    [Tooltip("离开判定区时，进度下降速度")]
    public float progressLosePerSecond = 0.08f;

    [Tooltip("小游戏开始时的初始进度")]
    [Range(0f, 1f)]
    public float startProgress = 0.80f;

    [Tooltip("小游戏开始后的保护时间，期间不会掉进度")]
    public float startGraceTime = 3f;

    private float currentGraceTimer = 0f;

    private FishingSpot currentSpot;
    private IPlayerControl playerControl;
    private Coroutine fishingRoutine;

    private bool isFishing;
    private bool inMiniGame;

    private float fishZoneCenter = 0.2f;
    private int fishZoneDir = 1;

    private float playerBarCenter = 0f;

    private float catchProgress = 0f;

    private void Awake()
    {
        playerControl = GetComponent<IPlayerControl>();
    }

    private void Start()
    {
        if (fishingUI == null)
        {
            fishingUI = FindObjectOfType<FishingUI>(true);
        }

        Debug.Log("Start -> fishingUI = " + fishingUI);

        if (fishingUI != null)
        {
            fishingUI.HideAll();
        }
        else
        {
            Debug.LogError("没有找到 FishingUI。请确认场景里 FishingCanvas 上挂了 FishingUI 脚本。");
        }
    }

    private void Update()
    {
        Debug.Log("Update中 -> inMiniGame = " + inMiniGame + ", isFishing = " + isFishing);

        if (inMiniGame)
        {
            Debug.Log("准备执行 UpdateMiniGame");
            UpdateMiniGame();
            return;
        }

        if (currentSpot == null) return;

        if (!isFishing && Input.GetKeyDown(fishKey))
        {
            Debug.Log("按下E，开始钓鱼");
            StartFishing();
        }
    }

    private void StartFishing()
    {
        isFishing = true;
        inMiniGame = false;

        if (playerControl != null)
        {
            playerControl.SetMovementEnabled(false);
            playerControl.SetCameraEnabled(false);
            playerControl.SetInputEnabled(false);
        }

        if (fishingUI != null)
        {
            fishingUI.ShowHint("等待鱼儿上钩...");
            fishingUI.HideMiniGame();
        }

        fishingRoutine = StartCoroutine(FishingFlow());
    }

    private IEnumerator FishingFlow()
    {
        float waitTime = Random.Range(waitMinTime, waitMaxTime);
        yield return new WaitForSeconds(waitTime);

        StartMiniGame();

        float timer = miniGameDuration;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            Debug.Log("FishingFlow检查 -> catchProgress = " + catchProgress + ", timer = " + timer);

            if (catchProgress >= 1f)
            {
                Debug.Log("触发 CatchFish");
                CatchFish();
                yield break;
            }

            if (catchProgress <= 0f)
            {
                Debug.Log("触发 FailFishing");
                FailFishing();
                yield break;
            }

            yield return null;
        }

        Debug.Log("时间到了，触发 FailFishing");
        FailFishing();
    }
    private void StartMiniGame()
    {
        Debug.Log("=== StartMiniGame ===");

        inMiniGame = true;

        fishZoneCenter = 0.2f;
        fishZoneDir = 1;
        playerBarCenter = fishZoneCenter;
        catchProgress = startProgress;
        currentGraceTimer = startGraceTime;

        if (fishingUI != null)
        {
            fishingUI.ShowHint("按住空格控制鱼线，保持在判定区内");
            fishingUI.ShowMiniGame();
            fishingUI.SetFishZonePosition(fishZoneCenter);
            fishingUI.SetPlayerBarPosition(playerBarCenter);
            fishingUI.SetProgress(catchProgress);
        }
    }

    private void UpdateMiniGame()
    {
        Debug.Log("UpdateMiniGame 正在运行");
        float dt = Time.deltaTime;

        // 1. 鱼判定区左右移动
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

        // 2. 玩家条：按住空格往右，不按往左掉
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

        // 3. 判断是否重叠
        float zoneLeft = fishZoneCenter - halfZone;
        float zoneRight = fishZoneCenter + halfZone;

        float playerLeft = playerBarCenter - halfPlayer;
        float playerRight = playerBarCenter + halfPlayer;

        bool inZone = playerRight >= zoneLeft && playerLeft <= zoneRight;

        // 4. 单进度条逻辑
        if (currentGraceTimer > 0f)
        {
            currentGraceTimer -= dt;

            // 保护期内：在区间里可以涨，不在区间里不扣
            if (inZone)
            {
                catchProgress += progressGainPerSecond * dt;
            }
        }
        else
        {
            if (inZone)
            {
                catchProgress += progressGainPerSecond * dt;
            }
            else
            {
                catchProgress -= progressLosePerSecond * dt;
            }
        }

        catchProgress = Mathf.Clamp01(catchProgress);

        catchProgress = Mathf.Clamp01(catchProgress);

        // 5. UI刷新
        if (fishingUI != null)
        {
            fishingUI.SetFishZonePosition(fishZoneCenter);
            fishingUI.SetPlayerBarPosition(playerBarCenter);
            fishingUI.SetProgress(catchProgress);
        }
    }

    private void CatchFish()
    {
        inMiniGame = false;
        StopCurrentRoutine();

        if (fishingUI != null)
        {
            fishingUI.ShowHint("钓到了！");
            fishingUI.HideMiniGame();
        }

        StartCoroutine(FinishAfterDelay());
    }

    private void FailFishing()
    {
        inMiniGame = false;
        StopCurrentRoutine();

        if (fishingUI != null)
        {
            fishingUI.ShowHint("鱼跑了...");
            fishingUI.HideMiniGame();
        }

        StartCoroutine(FinishAfterDelay());
    }

    private IEnumerator FinishAfterDelay()
    {
        yield return new WaitForSeconds(0.8f);
        StopFishing();
    }

    private void StopFishing()
    {
        isFishing = false;
        inMiniGame = false;
        StopCurrentRoutine();

        if (playerControl != null)
        {
            playerControl.SetInputEnabled(true);
            playerControl.SetMovementEnabled(true);
            playerControl.SetCameraEnabled(true);
        }

        if (fishingUI != null)
        {
            if (currentSpot != null)
            {
                fishingUI.ShowHint("按 E 开始钓鱼");
                fishingUI.HideMiniGame();
            }
            else
            {
                fishingUI.HideAll();
            }
        }
    }

    private void StopCurrentRoutine()
    {
        if (fishingRoutine != null)
        {
            StopCoroutine(fishingRoutine);
            fishingRoutine = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("进入触发器: " + other.name);

        FishingSpot spot = other.GetComponent<FishingSpot>();
        if (spot == null)
        {
            spot = other.GetComponentInParent<FishingSpot>();
        }

        if (spot != null)
        {
            Debug.Log("进入钓鱼点成功: " + spot.name);

            currentSpot = spot;

            if (fishingUI == null)
            {
                Debug.LogError("fishingUI 为空，无法显示 UI");
                return;
            }

            if (isFishing)
            {
                Debug.LogWarning("当前正在钓鱼中，不显示靠近提示");
                return;
            }

            fishingUI.ShowHint("按 E 开始钓鱼");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("退出触发器: " + other.name);

        FishingSpot spot = other.GetComponent<FishingSpot>();
        if (spot == null)
        {
            spot = other.GetComponentInParent<FishingSpot>();
        }

        if (spot != null && spot == currentSpot)
        {
            Debug.Log("钓鱼过程中离开触发器，isFishing = " + isFishing + ", inMiniGame = " + inMiniGame);

            currentSpot = null;

            if (isFishing)
            {
                StopFishing();
            }
            else if (fishingUI != null)
            {
                fishingUI.HideAll();
            }
        }
    }
}