using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Vector2 moveInput;

    [SerializeField] private NetworkTransform characterTransform;

    private InputSystem_Actions controls; // Name depends on what you named your asset


    private void OnEnable()
    {
        controls.Enable();
        // Subscribe to the move action
        controls.Player.Move.performed += ctx => OnMove(ctx);
        controls.Player.Move.canceled += ctx => OnMove(ctx);
    }
    private void OnDisable()
    {
        controls.Player.Move.performed -= ctx => OnMove(ctx);
        controls.Player.Move.canceled -= ctx => OnMove(ctx);
        controls.Disable();

    }

    private void Update()
    {
        if(!IsOwner)
        {
            return;
        }
        // Move();

    }

    public void Move()
    {
        if (IsOwner)
        {
            Debug.Log("Player is moving");
            // Add movement logic here
            Vector2 move = moveInput * moveSpeed * Time.deltaTime;
            characterTransform.transform.Translate(new Vector3(move.x, 0, move.y));
        }
    }

    // Input System의 "Move" 액션에 연결
    public void OnMove(InputAction.CallbackContext context)
    {
        Debug.Log("OnMove called");
        moveInput = context.ReadValue<Vector2>();
    }

    public void GiveOwnershipTo(ulong clientId)
    {
        if (IsServer)
        {
            NetworkObject networkObject = GetComponent<NetworkObject>();
            networkObject.ChangeOwnership(clientId);
        }
    }

}
