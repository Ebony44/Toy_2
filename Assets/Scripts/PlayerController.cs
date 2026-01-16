using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    private bool isDashing = false;

    private Vector2 moveInput;

    // [NonSerialized] public Vector3 movementInput; //Initial input coming from the Protagonist script
    // [NonSerialized] public Vector3 movementVector; //Final movement vector, manipulated by the StateMachine actions


    [SerializeField] private NetworkTransform characterTransform;

    private InputSystem_Actions controls; // Name depends on what you named your asset

    public bool bIsMovementEnabled = false;

    #region player visual variables

    [SerializeField] private PlayerVisualController playerVisualController;
    // [SerializeField] private Animator playerAnimator;

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

            //// if moveInput is not zero
            //if (moveInput == Vector2.zero)
            //{

            //    return;
            //}

            // Debug.Log("Player is moving");
            //// Add movement logic here
            //Vector2 move = moveInput * moveSpeed * Time.deltaTime;
            //characterTransform.transform.Translate(new Vector3(move.x, 0, move.y));

            Vector2 moveDir = moveInput.normalized; // 방향만 추출, 길이 1로 고정
            Vector2 move = moveDir * moveSpeed * Time.deltaTime * moveInput.magnitude; // 입력 세기 반영(아날로그 스틱 등)
            characterTransform.transform.Translate(new Vector3(move.x, 0, move.y),Space.World);

            // rotation code
            if (moveInput.sqrMagnitude > 0.01f)
            {
                // 2D 입력을 3D 방향으로 변환
                Vector3 lookDir = new Vector3(moveDir.x, 0, moveDir.y);
                // Quaternion.LookRotation은 forward 방향을 기준으로 회전 생성
                Quaternion targetRotation = Quaternion.LookRotation(lookDir, Vector3.up);

                // characterTransform.transform.rotation = targetRotation;

                // interpolate the rotation
                characterTransform.transform.rotation = Quaternion.Lerp(
                characterTransform.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
                );


            }

            // rotation code end

            #region player visual update
            // Update player visual based on movement
            if (playerVisualController != null)
            {
                bool isMoving = moveInput.magnitude > 0.1f;
                playerVisualController.UpdateMovementAnimation(isMoving);
            }
            
            #endregion player visual update end

        }
    }

    // Input System의 "Move" 액션에 연결
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        Debug.Log("OnMove called, move input is " + moveInput
            + " is owner: " + IsOwner);
    }

    //public void OnDash(InputAction.CallbackContext context)
    //{
    //    if(context.phase == InputActionPhase.Performed)
    //    {
    //        Debug.Log("Dash performed");
    //    }

    //}

    public void OnDash(InputAction.CallbackContext context)
    {
        // 이미 대시 중이거나 이동 불가능한 상태라면 무시
        if (isDashing || !IsOwner || !bIsMovementEnabled) return;

        // 버튼 입력 확인
        // if (context.ReadValueAsButton())
        if (context.phase == InputActionPhase.Performed)
        {
            StartCoroutine(PerformDash());
        }


        // Dash logic here?
        // if moveinput is not zero
        // moveinput with modifier?

        // if moveinput is zero
        // follow mouse direction?

    }

    private System.Collections.IEnumerator PerformDash()
    {
        isDashing = true;
        bool wasMovementEnabled = bIsMovementEnabled;
        bIsMovementEnabled = false; // 대시 중 일반 이동 차단

        Vector3 dashDirection = transform.forward; // 기본값: 현재 정면

        // 마우스 위치로 방향 계산
        if (Camera.main != null && Mouse.current != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane groundPlane = new Plane(Vector3.up, transform.position); // 플레이어 높이 기준 평면

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0; // 수직 이동 방지

                if (direction.sqrMagnitude > 0.01f)
                {
                    dashDirection = direction.normalized;
                }
            }
        }

        // 대시 방향으로 회전 (선택 사항)
        characterTransform.transform.rotation = Quaternion.LookRotation(dashDirection, Vector3.up);

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration)
        {
            characterTransform.transform.Translate(dashDirection * dashSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        bIsMovementEnabled = wasMovementEnabled;
        isDashing = false;
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
