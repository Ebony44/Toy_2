using Unity.Netcode.Components;
using UnityEngine;

public class PlayerVisualController : MonoBehaviour
{
    [SerializeField] private Animator playerAnimator;

    // [SerializeField] private NetworkTransform playerCharacter;
    [SerializeField] private Transform playerCharacter;

    public void UpdateMovementAnimation(bool isMoving)
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isMoving", isMoving);
        }
    }

    public void TestAction(string animKey)
    {
        playerAnimator.CrossFade(animKey, 0.1f);
    }

}
