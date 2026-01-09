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

    public void UpdateMovementAnimation(bool isMoving)
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(ANIM_KEY_WALK, isMoving);
        }
    }

    public void TestAction(string animKey)
    {
        playerAnimator.CrossFade(animKey, 0.1f);
    }

}
