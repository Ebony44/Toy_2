using Unity.Netcode;
using UnityEngine;

namespace Learning
{
    [RequireComponent(typeof(SimpleNpcNavMeshNetworkMovement))]
    public class NpcTargetController : NetworkBehaviour
    {
        [SerializeField] private SimpleNpcNavMeshNetworkMovement m_Movement;
        [SerializeField] private float m_RepathInterval = 0.2f;
        [SerializeField] private float m_StopDistance = 1.5f;

        private readonly NetworkVariable<ulong> m_TargetObjectId = new NetworkVariable<ulong>(0);

        private float m_RepathTimer;

        private void Awake()
        {
            if (m_Movement == null)
            {
                m_Movement = GetComponent<SimpleNpcNavMeshNetworkMovement>();
            }
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            if (!TryGetTargetTransform(out Transform targetTransform))
            {
                return;
            }

            m_RepathTimer += Time.deltaTime;
            if (m_RepathTimer < m_RepathInterval)
            {
                return;
            }

            m_RepathTimer = 0f;

            float sqrDistance = (targetTransform.position - transform.position).sqrMagnitude;
            if (sqrDistance > m_StopDistance * m_StopDistance)
            {
                m_Movement.MoveTo(targetTransform.position);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetTargetRpc(ulong targetNetworkObjectId)
        {
            SetTargetOnServer(targetNetworkObjectId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void ClearTargetRpc()
        {
            ClearTargetOnServer();
        }

        public void SetTargetOnServer(NetworkObject targetObject)
        {
            if (!IsServer || targetObject == null)
            {
                return;
            }

            SetTargetOnServer(targetObject.NetworkObjectId);
        }

        public void SetTargetOnServer(ulong targetNetworkObjectId)
        {
            if (!IsServer)
            {
                return;
            }

            if (!NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(targetNetworkObjectId))
            {
                return;
            }

            m_TargetObjectId.Value = targetNetworkObjectId;
        }

        public void ClearTargetOnServer()
        {
            if (!IsServer)
            {
                return;
            }

            m_TargetObjectId.Value = 0;
        }

        private bool TryGetTargetTransform(out Transform targetTransform)
        {
            targetTransform = null;

            if (m_TargetObjectId.Value == 0)
            {
                return false;
            }

            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(m_TargetObjectId.Value, out NetworkObject targetObject))
            {
                return false;
            }

            targetTransform = targetObject.transform;
            return true;
        }
    }
}