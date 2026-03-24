using Game.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI;

    [FormerlySerializedAs("playerControl")]
    [SerializeField] private MonoBehaviour playerControlSource;

    private IPlayerControl playerControl;
    private bool isOpen;

    private void Start()
    {
        ResolvePlayerControl();

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
            Debug.Log("Start close inventory: " + inventoryUI.name);
        }

        isOpen = false;
        ApplyState();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;
        ApplyState();
    }

    public void OpenInventory()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        ApplyState();
    }

    public void CloseInventory()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        ApplyState();
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    private void ApplyState()
    {
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(isOpen);
        }

        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;

        if (!isOpen && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (playerControl != null)
        {
            playerControl.SetMoveEnabled(!isOpen);
            playerControl.SetLookEnabled(!isOpen);
            playerControl.SetJumpEnabled(!isOpen);
        }

        Debug.Log("Inventory state: " + (isOpen ? "open" : "closed"));
    }

    private bool ResolvePlayerControl()
    {
        if (playerControl != null)
        {
            return true;
        }

        playerControl = playerControlSource as IPlayerControl;
        if (playerControl == null && playerControlSource != null)
        {
            Debug.LogError("playerControlSource does not implement IPlayerControl.");
        }

        if (playerControl != null)
        {
            return true;
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

        return false;
    }
}
