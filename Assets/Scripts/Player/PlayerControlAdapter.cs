using UnityEngine;
using StarterAssets;

public class PlayerControlAdapter : MonoBehaviour, IPlayerControl
{
    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterInputs;

    private bool movementEnabled = true;
    private bool cameraEnabled = true;
    private bool inputEnabled = true;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterInputs = GetComponent<StarterAssetsInputs>();
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        if (!enabled && starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.sprint = false;
            starterInputs.jump = false;
        }
    }

    public void SetCameraEnabled(bool enabled)
    {
        cameraEnabled = enabled;

        if (thirdPersonController != null)
        {
            thirdPersonController.LockCameraPosition = !enabled;
        }

        if (!enabled && starterInputs != null)
        {
            starterInputs.look = Vector2.zero;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled && starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.sprint = false;
            starterInputs.jump = false;
        }
    }

    public bool IsGrounded()
    {
        return thirdPersonController != null && thirdPersonController.Grounded;
    }

    private void Update()
    {
        if (starterInputs == null) return;

        if (!inputEnabled)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.look = Vector2.zero;
            starterInputs.sprint = false;
            starterInputs.jump = false;
            return;
        }

        if (!movementEnabled)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.sprint = false;
            starterInputs.jump = false;
        }

        if (!cameraEnabled)
        {
            starterInputs.look = Vector2.zero;
        }
    }
}