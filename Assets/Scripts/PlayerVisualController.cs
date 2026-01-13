using Unity.Netcode.Components;
using UnityEngine;

public class PlayerVisualController : MonoBehaviour
{
    [SerializeField] private Animator playerAnimator;

    // [SerializeField] private NetworkAnimator networkAnimator;

    // [SerializeField] private NetworkTransform playerCharacter;
    [SerializeField] private Transform playerCharacter;

    // private const string ANIM_KEY_IDLE = "Idle";
    private const string ANIM_KEY_WALK = "IsWalking";
    private const string ANIM_KEY_DASH = "IsDashing";

    public void UpdateMovementAnimation(bool isMoving)
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(ANIM_KEY_WALK, isMoving);
        }
    }
    public void UpdateIdleAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.Play("Idle");
        }
    }
    public void PlayAnimationWithName(string animName)
    {
        if (playerAnimator != null)
        {
            playerAnimator.Play(animName);
        }
    }
    public void SetBoolAnimationWithName(string animName, bool value)
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(animName, value);
        }
    }

    public void TestAction(string animKey)
    {
        playerAnimator.CrossFade(animKey, 0.1f);
    }

}
