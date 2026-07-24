using Unity.Netcode;
using UnityEngine;

namespace Learning
{
    public enum EAttackShape
    {
        ForwardArc,
        Linear,
        Projectile,
        AreaOfEffect
    }

    [RequireComponent(typeof(NpcTargetController))]
    public class NPCAttackController : NetworkBehaviour
    {
        [SerializeField] private NpcTargetController m_TargetController;
        [SerializeField] private EAttackShape m_AttackShape = EAttackShape.ForwardArc;
        [SerializeField] private float m_AttackRange = 1.5f;
        [SerializeField] private float m_AttackInterval = 1f;
        [SerializeField] private int m_AttackDamage = 10;
        [SerializeField] private float m_AttackShapeDuration = 0.25f;
        [SerializeField] private Color m_AttackShapeColor = Color.red;

        public NetworkVariable<bool> bIsAttackable = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private float m_AttackTimer;

        private void Awake()
        {
            if (m_TargetController == null)
            {
                m_TargetController = GetComponent<NpcTargetController>();
            }
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            if (m_TargetController == null)
            {
                bIsAttackable.Value = false;
                return;
            }

            if (!m_TargetController.TryGetTargetTransform(out Transform targetTransform))
            {
                bIsAttackable.Value = false;
                m_AttackTimer = 0f;
                return;
            }

            bIsAttackable.Value = IsAttackable(targetTransform);
            if (!bIsAttackable.Value)
            {
                m_AttackTimer = 0f;
                return;
            }

            m_AttackTimer += Time.deltaTime;
            if (m_AttackTimer < m_AttackInterval)
            {
                return;
            }

            m_AttackTimer = 0f;
            Attack(targetTransform);
        }

        private bool IsAttackable(Transform targetTransform)
        {
            float sqrDistance = (targetTransform.position - transform.position).sqrMagnitude;
            return sqrDistance <= m_AttackRange * m_AttackRange;
        }

        private void Attack(Transform targetTransform)
        {
            VisualizeAttackShape();

            Debug.Log($"{name} attacked {targetTransform.name} for {m_AttackDamage} damage.");

            // targetTransform.GetComponent<PlayerHealth>()?.TakeDamage(m_AttackDamage);
        }

        private void VisualizeAttackShape()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;

            switch (m_AttackShape)
            {
                case EAttackShape.ForwardArc:
                    DrawAttackArc(origin, 180f);
                    break;

                case EAttackShape.Linear:
                    DrawLinearAttack(origin);
                    break;

                case EAttackShape.Projectile:
                    Debug.DrawLine(
                        origin,
                        origin + transform.forward * m_AttackRange,
                        m_AttackShapeColor,
                        m_AttackShapeDuration);
                    break;

                case EAttackShape.AreaOfEffect:
                    DrawAttackArc(origin, 360f);
                    break;
            }
        }

        private void DrawAttackArc(Vector3 origin, float angle)
        {
            const int segmentCount = 16;

            Vector3 previousPoint = origin +
                Quaternion.AngleAxis(-angle * 0.5f, Vector3.up) *
                transform.forward *
                m_AttackRange;

            if (angle < 360f)
            {
                Debug.DrawLine(origin, previousPoint, m_AttackShapeColor, m_AttackShapeDuration);
            }

            for (int i = 1; i <= segmentCount; i++)
            {
                float currentAngle = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, (float)i / segmentCount);
                Vector3 flatForward = transform.forward;
                flatForward.y = 0f;
                flatForward.Normalize();

                Vector3 point =
                    origin +
                    Quaternion.AngleAxis(currentAngle, Vector3.up) *
                    flatForward *
                    m_AttackRange;

                Debug.DrawLine(previousPoint, point, m_AttackShapeColor, m_AttackShapeDuration);
                previousPoint = point;
            }

            if (angle < 360f)
            {
                Debug.DrawLine(origin, previousPoint, m_AttackShapeColor, m_AttackShapeDuration);
            }
        }

        private void DrawLinearAttack(Vector3 origin)
        {
            float halfWidth = 0.25f;
            Vector3 leftOffset = transform.right * halfWidth;
            Vector3 end = origin + transform.forward * m_AttackRange;

            Debug.DrawLine(origin - leftOffset, origin + leftOffset, m_AttackShapeColor, m_AttackShapeDuration);
            Debug.DrawLine(origin - leftOffset, end - leftOffset, m_AttackShapeColor, m_AttackShapeDuration);
            Debug.DrawLine(origin + leftOffset, end + leftOffset, m_AttackShapeColor, m_AttackShapeDuration);
            Debug.DrawLine(end - leftOffset, end + leftOffset, m_AttackShapeColor, m_AttackShapeDuration);
        }
    }
}