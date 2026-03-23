using UnityEngine;
using StarterAssets;
using Game.Player;
using UnityEngine.InputSystem;

public class PlayerControlAdapter : MonoBehaviour, IPlayerControl
{
    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterInputs;
    private Animator animator;
    private PlayerInput playerInput;

    private bool moveEnabled = true;
    private bool lookEnabled = true;
    private bool jumpEnabled = true;

    private InputAction jumpAction;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterInputs = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null && playerInput.actions != null)
        {
            jumpAction = playerInput.actions["Jump"];
        }
    }

    public void SetMoveEnabled(bool enabled)
    {
        moveEnabled = enabled;

        if (starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.sprint = false;
        }
    }

    public void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;

        if (starterInputs != null)
        {
            starterInputs.look = Vector2.zero;
        }
    }

    public void SetJumpEnabled(bool enabled)
    {
        jumpEnabled = enabled;

        if (starterInputs != null)
        {
            starterInputs.jump = false;
        }

        if (jumpAction != null)
        {
            if (enabled)
            {
                if (!jumpAction.enabled)
                    jumpAction.Enable();
            }
            else
            {
                if (jumpAction.enabled)
                    jumpAction.Disable();
            }
        }
    }

    public void PlayFishingAnimation(bool isFishing)
    {
        if (animator != null)
        {
            animator.SetBool("Fishing", isFishing);
        }
    }

    private void LateUpdate()
    {
        if (starterInputs == null) return;

        if (!moveEnabled)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.sprint = false;
        }

        if (!lookEnabled)
        {
            starterInputs.look = Vector2.zero;
        }

        if (!jumpEnabled)
        {
            starterInputs.jump = false;
        }
    }
}