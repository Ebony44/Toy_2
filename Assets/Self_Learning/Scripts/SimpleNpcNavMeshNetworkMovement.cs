using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Learning
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class SimpleNpcNavMeshNetworkMovement : NetworkBehaviour
    {
        [SerializeField] private NavMeshAgent m_Agent;
        [SerializeField] private float m_SampleRadius = 2f;
        [SerializeField] private float m_ArriveDistance = 0.2f;

        private readonly NetworkVariable<bool> m_IsMoving = new NetworkVariable<bool>(false);

        private void Awake()
        {
            if (m_Agent == null)
            {
                m_Agent = GetComponent<NavMeshAgent>();
            }
        }

        public override void OnNetworkSpawn()
        {
            // 서버만 실제 이동 시뮬레이션
            m_Agent.enabled = IsServer;
            enabled = IsServer;
        }

        private void Update()
        {
            if (!IsServer || !m_Agent.enabled)
            {
                return;
            }

            if (m_IsMoving.Value && !m_Agent.pathPending && m_Agent.remainingDistance <= m_ArriveDistance)
            {
                m_IsMoving.Value = false;
            }
        }

        public void MoveTo(Vector3 worldPosition)
        {
            if (IsServer)
            {
                SetDestinationInternal(worldPosition);
                return;
            }

            RequestMoveRpc(worldPosition);
        }

        [Rpc(SendTo.Server)]
        private void RequestMoveRpc(Vector3 worldPosition)
        {
            SetDestinationInternal(worldPosition);
        }

        private void SetDestinationInternal(Vector3 worldPosition)
        {
            if (!m_Agent.enabled)
            {
                return;
            }

            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, m_SampleRadius, NavMesh.AllAreas))
            {
                m_Agent.SetDestination(hit.position);
                m_IsMoving.Value = true;
            }
        }

        public bool IsMoving()
        {
            return m_IsMoving.Value;
        }
    }
}
