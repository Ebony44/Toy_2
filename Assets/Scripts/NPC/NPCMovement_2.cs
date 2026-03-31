using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Navigation;

// 1. 상태 정의: 캐릭터가 현재 무엇을 하고 있는지 정의합니다.
//public enum MovementState
//{
//    Idle = 0,           // 정지
//    PathFollowing = 1,  // 길 찾기 이동 중
//    Charging = 2,       // 돌진 (스킬 등)
//    Knockback = 3,      // 넉백 (밀려남)
//}

public class NPCMovement_2 : NetworkBehaviour
{
    // 의존성 컴포넌트들
    [SerializeField] NavMeshAgent m_NavMeshAgent;
    [SerializeField] Rigidbody m_Rigidbody;
    // [SerializeField] ServerCharacter m_CharLogic; // 속도(Speed) 정보 등을 가져오기 위함

    private NavigationSystem m_NavigationSystem;
    private DynamicNavPath m_NavPath; // 우리가 직접 만든(혹은 분석한) 길찾기 도우미

    // 내부 상태 변수
    private MovementState m_MovementState;

    // 특수 이동(돌진/넉백)을 위한 변수들
    private float m_ForcedSpeed;
    private float m_SpecialModeDurationRemaining;
    private Vector3 m_KnockbackVector;

    // temp variables
    [SerializeField] private float baseMoveSpeed = 3f;

    // 타겟 도달 감지용
    [SerializeField] private float targetReachedDistance = 0.5f; // 도달 판정 거리
    private Transform m_CurrentTarget;
    private EnemySpawner m_Spawner; // 스포너 참조



    // 초기화: Start() 대신 OnNetworkSpawn()을 사용합니다.
    public override void OnNetworkSpawn()
    {
        // if (IsServer) // 이 스크립트는 서버에서만 돌아갑니다!
        if(NetworkManager.Singleton.IsServer)
        {
            enabled = true;
            m_NavMeshAgent.enabled = true;

            // 네비게이션 시스템 찾기
            // m_NavigationSystem = GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag).GetComponent<NavigationSystem>();
            m_NavigationSystem = GetComponent<NavigationSystem>(); // 같은 게임 오브젝트에 있다고 가정

            // DynamicNavPath 초기화 (이전 질문에서 본 그 클래스)
            m_NavPath = new DynamicNavPath(m_NavMeshAgent, m_NavigationSystem);
        }
        else
        {
            enabled = false; // 클라이언트는 이 스크립트를 끕니다 (위치는 NetworkTransform으로 동기화받음)
        }
    }

    public override void OnNetworkDespawn()
    {
        if (m_NavPath != null)
            m_NavPath.Dispose();
    }

    private void FixedUpdate()
    {

        PerformMovement();
        CheckTargetReached();
    }

    /// <summary>
    /// 타겟에 도달했는지 체크 (테스트용)
    /// </summary>
    private void CheckTargetReached()
    {
        if (!IsServer || m_CurrentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, m_CurrentTarget.position);

        if (distanceToTarget <= targetReachedDistance)
        {
            Debug.Log($"[NPCMovement_2] Enemy reached target! Distance: {distanceToTarget}. Returning to pool.");
            ReturnToPool();
        }
    }

    /// <summary>
    /// 풀로 반환 (테스트용)
    /// </summary>
    private void ReturnToPool()
    {
        if (m_Spawner != null)
        {
            m_CurrentTarget = null;
            m_MovementState = MovementState.Idle;
            m_NavPath.Clear();

            m_Spawner.ReturnEnemyToPool(GetComponent<NetworkObject>());
        }
        else
        {
            Debug.LogWarning("[NPCMovement_2] Spawner reference not found!");
        }
    }


    private void PerformMovement()
    {
        if (m_MovementState == MovementState.Idle)
            return;

        Vector3 movementVector;
        var desiredMovementAmount = baseMoveSpeed * Time.fixedDeltaTime;
        movementVector = m_NavPath.MoveAlongPath(desiredMovementAmount);
    }

    public void FollowTransform(Transform followTransform)
    {
        m_NavPath?.FollowTransform(followTransform);
    
    }


    public void StartForwardCharge(float speed, float duration)
    {
        m_NavPath.Clear();
        m_MovementState = MovementState.Charging;
        m_ForcedSpeed = speed;
        m_SpecialModeDurationRemaining = duration;
    }

    public void StartKnockback(Vector3 knocker, float speed, float duration)
    {
        m_NavPath.Clear();
        m_MovementState = MovementState.Knockback;
        m_KnockbackVector = transform.position - knocker;
        m_ForcedSpeed = speed;
        m_SpecialModeDurationRemaining = duration;
    }


}