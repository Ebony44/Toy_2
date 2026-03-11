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

    // 초기화: Start() 대신 OnNetworkSpawn()을 사용합니다.
    public override void OnNetworkSpawn()
    {
        if (IsServer) // 이 스크립트는 서버에서만 돌아갑니다!
        {
            enabled = true;
            m_NavMeshAgent.enabled = true;

            // 네비게이션 시스템 찾기
            m_NavigationSystem = GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag).GetComponent<NavigationSystem>();

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
}