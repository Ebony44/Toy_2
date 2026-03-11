using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

// 로컬에서 관리할 투사체 인스턴스 클래스
public class ProjectileInstance
{
    public ProjectileInfo Info;
    public GameObject VisualObject;
}

public class ProjectileManager : NetworkBehaviour
{
    // 로컬 시뮬레이션용 리스트 (서버와 클라이언트 모두 각자 가짐)
    // NetworkList가 아닙니다!
    private Dictionary<int, ProjectileInstance> activeProjectiles = new Dictionary<int, ProjectileInstance>();

    private List<int> cachedIdsToRemove = new List<int>(16);

    [SerializeField] private GameObject projectilePrefab;
    private IObjectPool<GameObject> projectilePool;
    private const int INIT_POOL_SIZE = 32;

    // 서버 사이드 ID 발급용
    private int _nextId = 0;

    private void Awake()
    {
        projectilePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(projectilePrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: INIT_POOL_SIZE,
            maxSize: 100
        );
    }

    public override void OnNetworkDespawn()
    {
        // 종료 시 정리
        ClearAllProjectiles();
    }

    #region Fire Logic (Server -> Client)

    // 1. [Server] 발사 요청 시작
    public void FireProjectile(Vector2 spawnPosition, Vector2 velocity, ushort projectileTypeId)
    {
        if (!IsServer) return;

        var newProjectileInfo = new ProjectileInfo
        {
            ProjectileTypeId = projectileTypeId,
            projectileId = _nextId++,
            SpawnPosition = spawnPosition,
            Velocity = velocity,
            currentLifeTime = 0f,
            lifeTime = 2f,
        };

        // 2. [Server & Client] 모든 클라이언트에게 생성 지시 (Dead Reckoning 시작)
        // RPC를 통해 "초기 데이터"만 전송합니다. 이후 위치 갱신은 각자 합니다.
        SpawnProjectileRpc(newProjectileInfo);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnProjectileRpc(ProjectileInfo info)
    {
        // 풀에서 비주얼 오브젝트 가져오기
        GameObject visual = projectilePool.Get();
        visual.transform.position = info.SpawnPosition;

        // visual.transform.rotation = Quaternion.LookRotation(info.Velocity.normalized);
        // check only Z axis for isometric view
        visual.transform.rotation = Quaternion.LookRotation(new Vector3(info.Velocity.x, 0, info.Velocity.y).normalized);

        // 로컬 딕셔너리에 등록 (이제부터 각자 알아서 움직임)
        var instance = new ProjectileInstance
        {
            Info = info,
            VisualObject = visual
        };

        activeProjectiles.Add(info.projectileId, instance);
    }

    #endregion

    #region Simulation Loop (Dead Reckoning)

    private void Update()
    {
        // 서버와 클라이언트 모두, 각자 가지고 있는 리스트를 기반으로 투사체를 이동시킵니다.
        // 이것이 Dead Reckoning의 핵심입니다.

        // 딕셔너리 순회 중 삭제를 위해 키 리스트 별도 관리 필요 가능성 있음 (여기서는 단순화)
        // List<int> idsToRemove = new List<int>();

        foreach (var kvp in activeProjectiles)
        {
            int id = kvp.Key;
            ProjectileInstance instance = kvp.Value;

            // 1. 시간 흐름 계산
            instance.Info.currentLifeTime += Time.deltaTime;

            // 2. 이동 처리 (수학적으로 계산하여 이동)
            // P_new = P_old + V * t
            // convert y value to z value for isometric view
            // Vector3 displacement = (Vector3)instance.Info.Velocity * Time.deltaTime;
            Vector3 displacement = new Vector3(instance.Info.Velocity.x, 0, instance.Info.Velocity.y) * Time.deltaTime;

            instance.VisualObject.transform.position += displacement;

            // 내부 데이터(Info)의 위치도 갱신 (Collision 체크 등을 위해)
            instance.Info.SpawnPosition = instance.VisualObject.transform.position;

            // -------------------------------------------------------------------
            // [Server Only] 충돌 및 수명 처리
            // -------------------------------------------------------------------
            if (IsServer)
            {
                // 수명 체크
                if (instance.Info.lifeTime > 0 && instance.Info.currentLifeTime >= instance.Info.lifeTime)
                {
                    // idsToRemove.Add(id); // 삭제 예정 목록에 추가
                    cachedIdsToRemove.Add(id);
                    continue;
                }

                // 충돌 체크 (Raycast)
                float moveDistance = instance.Info.Velocity.magnitude * Time.deltaTime;
                if (Physics.Raycast(instance.VisualObject.transform.position - displacement, instance.Info.Velocity.normalized, out RaycastHit hit, moveDistance))
                {
                    // 충돌 발생
                    HandleHit(id, hit);
                    // idsToRemove.Add(id); // 삭제 예정 목록에 추가
                    cachedIdsToRemove.Add(id);
                    Debug.Log($"[Server] Projectile {id} hit something and will be removed." +
                        $" how far it moved: {moveDistance}, hit point: {hit.point}, hit collider: {hit.collider.name}");
                }
            }
        }

        // [Server Only] 삭제 처리 실행
        if (IsServer && cachedIdsToRemove.Count > 0)
        {
            foreach (int id in cachedIdsToRemove)
            {
                // 모든 클라이언트에게 삭제 지시
                DestroyProjectileRpc(id);
            }
        }
    }

    #endregion

    #region Event Handling & Cleanup

    private void HandleHit(int id, RaycastHit hit)
    {
        Debug.Log($"[Server] Projectile {id} hit {hit.collider.name}");
        // 데미지 처리 로직...

        var current = activeProjectiles[id];
        // TODO: particle, sound effect, etc. (should using RPC to trigger on clients?)
        // HandleHitFromClientRpc(current.Info.SpawnPosition, hit.point, current.Info.ProjectileTypeId);

        // remove projectile after hit
        DestroyProjectileRpc(id);

    }

    // 3. [Server & Client] 제거 지시
    [Rpc(SendTo.ClientsAndHost)]
    private void DestroyProjectileRpc(int projectileId)
    {
        if (activeProjectiles.TryGetValue(projectileId, out ProjectileInstance instance))
        {
            // 풀에 반납
            projectilePool.Release(instance.VisualObject);
            // 리스트에서 제거
            activeProjectiles.Remove(projectileId);
        }
    }

    private void ClearAllProjectiles()
    {
        foreach (var kvp in activeProjectiles)
        {
            projectilePool.Release(kvp.Value.VisualObject);
        }
        activeProjectiles.Clear();
    }

    #endregion

    #region Client Request

    public void TestFireProjectile()
    {
        if (!IsServer) return; // 테스트는 편의상 서버에서만 호출
        FireProjectile(Vector2.zero, new Vector2(1, 1).normalized * 5f, 0);
    }

    [Rpc(SendTo.Server)]
    public void FireRequestFromClientRpc(Vector2 spawnPosition, Vector2 velocity, ushort projectileTypeId)
    {
        if (IsServer)
        {
            FireProjectile(spawnPosition, velocity, projectileTypeId);
        }
    }
    #endregion
}