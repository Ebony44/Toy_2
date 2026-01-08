using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Vector2 moveInput;

    // [NonSerialized] public Vector3 movementInput; //Initial input coming from the Protagonist script
    // [NonSerialized] public Vector3 movementVector; //Final movement vector, manipulated by the StateMachine actions


    [SerializeField] private NetworkTransform characterTransform;

    private InputSystem_Actions controls; // Name depends on what you named your asset

    public bool bIsMovementEnabled = false;

    #region player visual variables

    [SerializeField] private Animator playerAnimator;

    #endregion


    private void Awake()
    {
        controls = new InputSystem_Actions();
        
        
    }
    private void Start()
    {
        // GameManager.Instance.OnNetWorkPostSpawned += OnNetworkPostSpawnedHandler;
    }

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

        // GameManager.Instance.OnNetWorkPostSpawned -= OnNetworkPostSpawnedHandler;

    }

    protected override void OnNetworkPostSpawn()
    {
        // base.OnNetworkPostSpawn();
        Debug.Log("PlayerController OnNetworkPostSpawn called");
        TakeOutOwnershipRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void TakeOutOwnershipRpc()
    {
        Debug.Log("TakeOutOwnershipRpc called");
        if (IsServer)
        {
            Debug.Log("Taking out ownership from player");
            NetworkObject networkObject = GetComponent<NetworkObject>();
            networkObject.RemoveOwnership();
        }
    }
    private void OnNetworkPostSpawnedHandler(object sender, System.EventArgs e)
    {
        Debug.Log("PlayerController received OnNetworkPostSpawned event");
        TakeOutOwnershipRpc();
    }

    private void Update()
    {
        if(!IsOwner)
        {
            return;
        }
        Move();

    }

    public override void OnGainedOwnership()
    {
        // base.OnGainedOwnership();
        Debug.Log("PlayerController OnGainedOwnership called");
    }

    public override void OnLostOwnership()
    {
        // base.OnLostOwnership();
        Debug.Log("PlayerController OnLostOwnership called");
    }

    public void Move()
    {
        if (bIsMovementEnabled)
        {
            Debug.Log("Player is moving");
            //// Add movement logic here
            //Vector2 move = moveInput * moveSpeed * Time.deltaTime;
            //characterTransform.transform.Translate(new Vector3(move.x, 0, move.y));

            Vector2 moveDir = moveInput.normalized; // 방향만 추출, 길이 1로 고정
            Vector2 move = moveDir * moveSpeed * Time.deltaTime * moveInput.magnitude; // 입력 세기 반영(아날로그 스틱 등)
            characterTransform.transform.Translate(new Vector3(move.x, 0, move.y));

        }
    }

    // Input System의 "Move" 액션에 연결
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        Debug.Log("OnMove called, move input is " + moveInput
            + " is owner: " + IsOwner);
    }

    public void GiveOwnershipTo(ulong clientId)
    {
        if (IsServer)
        {
            bIsMovementEnabled = true;
            NetworkObject networkObject = GetComponent<NetworkObject>();
            networkObject.ChangeOwnership(clientId);
        }
    }

}
