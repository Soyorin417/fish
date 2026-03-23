using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI;
    public PlayerControlAdapter playerControl;

    private bool isOpen = false;

    void Start()
    {
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
            Debug.Log("Start 时关闭背包：" + inventoryUI.name);
        }

        isOpen = false;
        ApplyState();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
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
        if (isOpen) return;

        isOpen = true;
        ApplyState();
    }

    public void CloseInventory()
    {
        if (!isOpen) return;

        isOpen = false;
        ApplyState();
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

        Debug.Log("背包状态: " + (isOpen ? "打开" : "关闭"));
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}