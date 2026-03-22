using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryUI;

    void Start()
    {
        if (inventoryUI != null)
            inventoryUI.SetActive(false);
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
        if (inventoryUI == null) return;


        bool show = !inventoryUI.activeSelf;

        inventoryUI.SetActive(show);


        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }
}