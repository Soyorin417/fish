using Game.Player;
using StarterAssets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI;

    [FormerlySerializedAs("playerControl")]
    [SerializeField] private MonoBehaviour playerControlSource;

    private IPlayerControl playerControl;
    private StarterAssetsInputs starterAssetsInputs;
    private bool warnedInvalidPlayerControlSource;

    // 只表示背包面板自己是否打开
    private bool inventoryOpen;

    // 表示当前有多少个UI在申请“UI模式”
    private int uiModeRequestCount;

    private void Start()
    {
        ResolvePlayerControl();
        ResolveStarterAssetsInputs();

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
            Debug.Log("[InventoryToggle] Start close inventory: " + inventoryUI.name);
        }

        inventoryOpen = false;
        uiModeRequestCount = 0;
        ApplyState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleInventory();
        }
    }

    /// <summary>
    /// 切换背包面板显示状态。
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    /// <summary>
    /// 打开背包面板。
    /// </summary>
    public void OpenInventory()
    {
        if (inventoryOpen)
        {
            return;
        }

        inventoryOpen = true;
        ApplyState();
    }

    /// <summary>
    /// 关闭背包面板。
    /// </summary>
    public void CloseInventory()
    {
        if (!inventoryOpen)
        {
            return;
        }

        inventoryOpen = false;
        ApplyState();
    }

    /// <summary>
    /// 供其他UI申请进入UI模式（显示鼠标、禁用玩家控制）。
    /// </summary>
    public void PushUIMode()
    {
        uiModeRequestCount++;
        ApplyState();
    }

    /// <summary>
    /// 供其他UI退出UI模式。
    /// </summary>
    public void PopUIMode()
    {
        if (uiModeRequestCount == 0)
        {
            Debug.LogWarning("[InventoryToggle] PopUIMode called when uiModeRequestCount is already 0.");
        }

        uiModeRequestCount = Mathf.Max(0, uiModeRequestCount - 1);
        ApplyState();
    }

    /// <summary>
    /// 当前是否处于UI模式。
    /// </summary>
    public bool IsInUIMode()
    {
        return inventoryOpen || uiModeRequestCount > 0;
    }

    /// <summary>
    /// 当前背包是否打开。
    /// </summary>
    public bool IsOpen()
    {
        return inventoryOpen;
    }

    /// <summary>
    /// 根据当前状态刷新背包、鼠标和玩家控制。
    /// </summary>
    private void ApplyState()
    {
        ResolvePlayerControl();
        ResolveStarterAssetsInputs();

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(inventoryOpen);
        }

        bool inUIMode = IsInUIMode();

        Cursor.lockState = inUIMode ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = inUIMode;

        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.cursorLocked = !inUIMode;
            starterAssetsInputs.cursorInputForLook = !inUIMode;
        }

        if (!inUIMode && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (playerControl != null)
        {
            playerControl.SetMoveEnabled(!inUIMode);
            playerControl.SetLookEnabled(!inUIMode);
            playerControl.SetJumpEnabled(!inUIMode);
        }

        Debug.Log($"[InventoryToggle] ApplyState inventoryOpen={inventoryOpen}, uiModeRequestCount={uiModeRequestCount}, inUIMode={inUIMode}");
    }

    /// <summary>
    /// 解析玩家控制组件。
    /// </summary>
    private bool ResolvePlayerControl()
    {
        if (playerControl != null)
        {
            return true;
        }

        if (playerControlSource is IPlayerControl sourceControl)
        {
            playerControl = sourceControl;
            return true;
        }

        if (playerControlSource != null)
        {
            MonoBehaviour[] siblingBehaviours = playerControlSource.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in siblingBehaviours)
            {
                if (behaviour is IPlayerControl siblingControl)
                {
                    playerControlSource = behaviour;
                    playerControl = siblingControl;
                    return true;
                }
            }

            if (!warnedInvalidPlayerControlSource)
            {
                Debug.LogWarning(
                    "InventoryToggle playerControlSource does not implement IPlayerControl directly. " +
                    "Tried to recover from the same GameObject: " + playerControlSource.name);
                warnedInvalidPlayerControlSource = true;
            }
        }

        foreach (MonoBehaviour behaviour in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (behaviour is IPlayerControl control)
            {
                playerControlSource = behaviour;
                playerControl = control;
                return true;
            }
        }

        if (!warnedInvalidPlayerControlSource)
        {
            Debug.LogError("InventoryToggle could not resolve any IPlayerControl in the scene.");
            warnedInvalidPlayerControlSource = true;
        }

        return false;
    }

    public void RefreshState()
    {
        if (!isActiveAndEnabled)
        {
            ApplySafetyState();
            return;
        }

        ApplyState();
    }

    private void OnDisable()
    {
        inventoryOpen = false;
        uiModeRequestCount = 0;
        ApplySafetyState();
    }

    private void ApplySafetyState()
    {
        ResolvePlayerControl();
        ResolveStarterAssetsInputs();

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (starterAssetsInputs != null)
        {
            starterAssetsInputs.cursorLocked = true;
            starterAssetsInputs.cursorInputForLook = true;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (playerControl != null)
        {
            playerControl.SetMoveEnabled(true);
            playerControl.SetLookEnabled(true);
            playerControl.SetJumpEnabled(true);
        }

        Debug.Log("[InventoryToggle] ApplySafetyState inventoryOpen=false, uiModeRequestCount=0, inUIMode=false");
    }

    private void ResolveStarterAssetsInputs()
    {
        if (starterAssetsInputs != null)
        {
            return;
        }

        if (playerControlSource != null)
        {
            starterAssetsInputs = playerControlSource.GetComponent<StarterAssetsInputs>();
        }

        if (starterAssetsInputs == null)
        {
            starterAssetsInputs = FindObjectOfType<StarterAssetsInputs>(true);
        }
    }
}
