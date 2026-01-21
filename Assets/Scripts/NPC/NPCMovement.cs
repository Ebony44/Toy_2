using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class NPCMovement : NetworkManager
{
    [SerializeField] NavMeshAgent m_NavMeshAgent;

    [SerializeField] Rigidbody m_Rigidbody;
}
