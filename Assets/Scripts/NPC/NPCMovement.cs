using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public enum MovementState
{
    Idle = 0,
    PathFollowing = 1,
    Charging = 2,
    Knockback = 3,
}

public class NPCMovement : NetworkBehaviour
{

    [SerializeField] Transform testTarget;

    [SerializeField] NavMeshAgent m_Agent;

    [SerializeField] Rigidbody m_Rigidbody;

    private MovementState m_MovementState;

    // MovementStatus m_PreviousState;

    /// <summary>
    /// This field caches a NavMesh Path so that we don't have to allocate a new one each time.
    /// </summary>
    NavMeshPath m_NavMeshPath;

    /// <summary>
    /// The remaining path points to follow to reach the target position.
    /// </summary>
    List<Vector3> m_Path;


    /// <summary>
    /// A moving transform target, the path will readjust when the target moves. If this is non-null, it takes precedence over m_PositionTarget.
    /// </summary>
    Transform m_TransformTarget;


    /// <summary>
    /// The target position of this path.
    /// </summary>
    Vector3 m_PositionTarget;

    /// <summary>
    /// The tolerance to decide whether the path needs to be recalculated when the position of a target transform changed.
    /// </summary>
    const float k_RepathToleranceSqr = 9f;

    /// <summary>
    /// The target position value which was used to calculate the current path.
    /// This gets stored to make sure the path gets recalculated if the target moves beyond the repath tolerance..?
    /// </summary>
    Vector3 m_CurrentPathOriginalTarget;


    // follow certain transform(m_TransformTarget) or... position(m_PositionTarget)
    // for instance, moving platform vs static point
    // if, transform(moving target) is missing, use position(static target)
    // or if moving target is present, use that...?
    // so this NPC will follow original position (m_CurrentPathOriginalTarget)
    Vector3 TargetPosition => m_TransformTarget != null ? m_TransformTarget.position : m_PositionTarget;

    void Awake()
    {
        // disable this NetworkBehavior until it is spawned
        enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Only enable server component on servers
            enabled = true;

            // On the server enable navMeshAgent and initialize
            m_Agent.enabled = true;
            //m_NavigationSystem = GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag).GetComponent<NavigationSystem>();
            //m_NavPath = new DynamicNavPath(m_NavMeshAgent, m_NavigationSystem);



        }
    }


    public void TestFollow()
    {
        FollowTransform(testTarget);
    }

    public void FollowTransform(Transform followTransform)
    {
        // this one was in update method...
        m_MovementState = MovementState.PathFollowing;
        // m_NavPath.FollowTransform(followTransform);
        // m_Agent.SetDestination(followTransform.position);
        m_TransformTarget = followTransform;
    }

    private void FixedUpdate()
    {
        PerformMovement();
    }

    private void PerformMovement()
    {
        if (m_MovementState == MovementState.Idle)
        {
            return;
        }
        Vector3 movementVector;

        if(m_MovementState == MovementState.PathFollowing)
        {
            movementVector = MoveAlongPath(m_Agent.speed * Time.fixedDeltaTime);
            m_Agent.Move(movementVector);
            transform.rotation = Quaternion.LookRotation(movementVector);

            m_Rigidbody.position = transform.position;
            m_Rigidbody.rotation = transform.rotation;

            // m_Rigidbody.MovePosition(m_Rigidbody.position + movementVector);
        }

    }



    public Vector3 MoveAlongPath(float distance)
    {
        if (m_TransformTarget != null)
        {
            // m_TransformTarget.position : m_PositionTarget;
            OnTargetPositionChanged(TargetPosition);
        }

        if (m_Path.Count == 0)
        {
            return Vector3.zero;
        }

        var currentPredictedPosition = m_Agent.transform.position;
        var remainingDistance = distance;

        while (remainingDistance > 0)
        {
            var toNextPathPoint = m_Path[0] - currentPredictedPosition;

            // If end point is closer then distance to move
            if (toNextPathPoint.sqrMagnitude < remainingDistance * remainingDistance)
            {
                currentPredictedPosition = m_Path[0];
                m_Path.RemoveAt(0);
                remainingDistance -= toNextPathPoint.magnitude;
            }

            // Move towards point
            currentPredictedPosition += toNextPathPoint.normalized * remainingDistance;

            // There is definitely no remaining distance to cover here.
            break;
        }

        return currentPredictedPosition - m_Agent.transform.position;
    }

    void OnTargetPositionChanged(Vector3 newTarget)
    {
        if (m_Path.Count == 0)
        {
            RecalculatePath(newTarget);
        }

        if ((newTarget - m_CurrentPathOriginalTarget).sqrMagnitude > k_RepathToleranceSqr)
        {
            RecalculatePath(newTarget);
        }
    }

    public void RecalculatePath(Vector3 target)
    {
        m_CurrentPathOriginalTarget = TargetPosition;
        // m_PositionTarget = target;

        m_Agent.CalculatePath(target, m_NavMeshPath);
        m_Path.Clear();
        var corners = m_NavMeshPath.corners;

        for (int i = 1; i < corners.Length; i++) // Skip the first corner because it is the starting point.
        {
            m_Path.Add(corners[i]);
        }

        // m_Path = new List<Vector3>(corners);

        // m_NavMeshAgent.SetDestination(target);
        // m_MovementState = MovementState.PathFollowing;
    }





}

///// <summary>
///// Describes how the character's movement should be animated: as standing idle, running normally,
///// magically slowed, sped up, etc. (Not all statuses are currently used by game content,
///// but they are set up to be displayed correctly for future use.)
///// </summary>
//[Serializable]
//public enum MovementStatus
//{
//    Idle,         // not trying to move
//    Normal,       // character is moving (normally)
//    Uncontrolled, // character is being moved by e.g. a knockback -- they are not in control!
//    Slowed,       // character's movement is magically hindered
//    Hasted,       // character's movement is magically enhanced
//    Walking,      // character should appear to be "walking" rather than normal running (e.g. for cut-scenes)
//}