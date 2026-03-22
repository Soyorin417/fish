using UnityEngine;
using StarterAssets;
using Game.Player;

public class PlayerControlAdapter : MonoBehaviour, IPlayerControl
{
    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterInputs;
    private Animator animator;

    private bool moveEnabled = true;
    private bool lookEnabled = true;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterInputs = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();
    }

    public void SetMoveEnabled(bool enabled)
    {
        moveEnabled = enabled;

        if (!enabled && starterInputs != null)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.sprint = false;
            starterInputs.jump = false;
        }
    }

    public void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;

        if (thirdPersonController != null)
        {
            thirdPersonController.LockCameraPosition = !enabled;
        }

        if (!enabled && starterInputs != null)
        {
            starterInputs.look = Vector2.zero;
        }
    }

    public void PlayFishingAnimation(bool isFishing)
    {
        if (animator != null)
        {
            animator.SetBool("Fishing", isFishing);
        }
    }

    private void Update()
    {
        if (starterInputs == null) return;

        if (!moveEnabled)
        {
            starterInputs.move = Vector2.zero;
            starterInputs.sprint = false;
            starterInputs.jump = false;
        }

        if (!lookEnabled)
        {
            starterInputs.look = Vector2.zero;
        }
    }
}