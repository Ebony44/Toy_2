using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Learning
{
    [RequireComponent(typeof(SimpleNpcTargetManager))]
    public class SimpleNpcSpawnManager : NetworkBehaviour
    {
        [SerializeField] private NetworkObject m_NpcPrefab;
        [SerializeField] private Transform[] m_SpawnPoints;
        [SerializeField] private bool m_SpawnOnServerStart = true;
        [SerializeField] private int m_SpawnCountOnStart = 1;
        [SerializeField] private SimpleNpcTargetManager m_TargetManager;

        private readonly List<NetworkObject> m_SpawnedNpcs = new List<NetworkObject>();

        public IReadOnlyList<NetworkObject> SpawnedNpcs => m_SpawnedNpcs;

        private void Awake()
        {
            if (m_TargetManager == null)
            {
                m_TargetManager = GetComponent<SimpleNpcTargetManager>();
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer || !m_SpawnOnServerStart)
            {
                return;
            }

            for (int i = 0; i < m_SpawnCountOnStart; i++)
            {
                Vector3 spawnPosition = GetSpawnPosition(i);
                SpawnNpc(spawnPosition);
            }

            // 초기 일괄 타겟 할당
            if (m_TargetManager != null)
            {
                m_TargetManager.AssignClosestPlayerToAllNpcs();
            }
        }

        public NetworkObject SpawnNpc(Vector3 position)
        {
            if (!IsServer || m_NpcPrefab == null)
            {
                return null;
            }

            NetworkObject npc = Instantiate(m_NpcPrefab, position, Quaternion.identity);
            npc.Spawn(true);
            m_SpawnedNpcs.Add(npc);

            // 새로 스폰된 NPC 포함해서 즉시 타겟 재할당
            if (m_TargetManager != null)
            {
                m_TargetManager.AssignClosestPlayerToAllNpcs();
            }

            return npc;
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (m_SpawnPoints != null && m_SpawnPoints.Length > 0)
            {
                int clamped = Mathf.Clamp(index, 0, m_SpawnPoints.Length - 1);
                return m_SpawnPoints[clamped].position;
            }

            return transform.position;
        }
    }
}