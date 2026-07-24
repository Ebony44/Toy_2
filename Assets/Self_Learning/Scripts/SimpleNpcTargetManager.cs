using Unity.Netcode;
using UnityEngine;

namespace Learning
{
    [RequireComponent(typeof(SimpleNpcSpawnManager))]
    public class SimpleNpcTargetManager : NetworkBehaviour
    {
        [SerializeField] private SimpleNpcSpawnManager m_SpawnManager;

        // NPCAttackController
        private void Awake()
        {
            if (m_SpawnManager == null)
            {
                m_SpawnManager = GetComponent<SimpleNpcSpawnManager>();
            }
        }

        [ContextMenu("Assign Closest Player To All NPCs")]
        public void AssignClosestPlayerToAllNpcs()
        {
            if (!IsServer || m_SpawnManager == null)
            {
                return;
            }

            for (int i = 0; i < m_SpawnManager.SpawnedNpcs.Count; i++)
            {
                NetworkObject npc = m_SpawnManager.SpawnedNpcs[i];
                if (npc == null)
                {
                    continue;
                }

                NpcTargetController targetController = npc.GetComponent<NpcTargetController>();
                if (targetController == null)
                {
                    Debug.LogWarning($"NpcTargetController not found on NPC {npc.name}");
                    continue;
                }

                NetworkObject closestPlayer = FindClosestPlayer(npc.transform.position);
                if (closestPlayer != null)
                {
                    targetController.SetTargetOnServer(closestPlayer);
                }
                else
                {
                    targetController.ClearTargetOnServer();
                }
            }
        }

        private NetworkObject FindClosestPlayer(Vector3 from)
        {
            if (NetworkManager == null || NetworkManager.ConnectedClientsList == null)
            {
                return null;
            }

            float bestSqrDistance = float.MaxValue;
            NetworkObject best = null;

            for (int i = 0; i < NetworkManager.ConnectedClientsList.Count; i++)
            {
                NetworkObject playerObject = NetworkManager.ConnectedClientsList[i].PlayerObject;
                if (playerObject == null)
                {
                    Debug.Log($"PlayerObject is null for client {NetworkManager.ConnectedClientsList[i].ClientId}");
                    continue;
                }

                float sqrDistance = (playerObject.transform.position - from).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = playerObject;
                }
            }

            return best;
        }
    }
}