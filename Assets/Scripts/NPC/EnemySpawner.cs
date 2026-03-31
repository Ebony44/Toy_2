using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private NPCMovement_2 enemyPrefab; // NetworkObject 컴포넌트가 있어야 함
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Transform initTargetTrans;

    private const int INIT_POOL_SIZE = 10;
    private const int MAX_POOL_SIZE = 50;

    private IObjectPool<NPCMovement_2> enemyPool;
    private Dictionary<ulong, NPCMovement_2> activeEnemies = new Dictionary<ulong, NPCMovement_2>();

    private float timer;
    public bool bIsGameStarted = false;

    private void Awake()
    {
        // ObjectPool 초기화
        enemyPool = new ObjectPool<NPCMovement_2>(
            createFunc: CreateEnemy,
            actionOnGet: OnGetEnemy,
            actionOnRelease: OnReleaseEnemy,
            actionOnDestroy: OnDestroyEnemy,
            collectionCheck: false,
            defaultCapacity: INIT_POOL_SIZE,
            maxSize: MAX_POOL_SIZE
        );
    }

    public override void OnNetworkSpawn()
    {
        // 서버에서만 스폰 로직 실행
        if (!NetworkManager.Singleton.IsServer) return;

        timer = spawnInterval;
    }

    public override void OnNetworkDespawn()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // 모든 활성 적 제거
        ClearAllEnemies();
    }

    private void Update()
    {
        // if (!IsServer) return;
        if (!NetworkManager.Singleton.IsServer) return; // 서버에서만 실행
        if (bIsGameStarted == false) return;

        Debug.Log($"[EnemySpawner] Timer: {timer}");

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnEnemy();
            timer = spawnInterval;
        }
    }

    #region Object Pool Callbacks

    private NPCMovement_2 CreateEnemy()
    {
        NPCMovement_2 enemy = Instantiate(enemyPrefab);
        enemy.gameObject.SetActive(false);
        return enemy;
    }

    private void OnGetEnemy(NPCMovement_2 enemy)
    {
        enemy.gameObject.SetActive(true);
    }

    private void OnReleaseEnemy(NPCMovement_2 enemy)
    {
        enemy.gameObject.SetActive(false);

        // NetworkObject Despawn (네트워크에서 제거)
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn(false); // destroy=false, 풀로 돌아갈 것이므로
        }
    }

    private void OnDestroyEnemy(NPCMovement_2 enemy)
    {
        if (enemy != null)
        {
            Destroy(enemy.gameObject);
        }
    }

    #endregion

    #region Spawn Logic

    private void SpawnEnemy()
    {
        // 랜덤 스폰 포인트 선택
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // 풀에서 적 가져오기
        NPCMovement_2 enemy = enemyPool.Get();
        enemy.transform.position = spawnPoint.position;
        enemy.transform.rotation = spawnPoint.rotation;

        // 네트워크 스폰 (모든 클라이언트에 동기화)
        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
        networkObject.Spawn();

        // 활성 적 딕셔너리에 추가
        activeEnemies.Add(networkObject.NetworkObjectId, enemy);

        // 초기 타겟 설정 (필요시)
        if (initTargetTrans != null)
        {
            enemy.FollowTransform(initTargetTrans);
        }

        Debug.Log($"Enemy spawned at {spawnPoint.position}, NetworkObjectId: {networkObject.NetworkObjectId}");
    }

    // 외부에서 호출할 수 있는 스폰 메서드
    public void TriggerSpawn(Vector3 position)
    {
        if (!IsServer) return;

        NPCMovement_2 enemy = enemyPool.Get();
        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.identity;

        NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
        networkObject.Spawn();

        activeEnemies.Add(networkObject.NetworkObjectId, enemy);

        if (initTargetTrans != null)
        {
            enemy.FollowTransform(initTargetTrans);
        }
    }

    #endregion

    #region Enemy Lifecycle Management

    /// <summary>
    /// 적이 죽었을 때 호출 (예: EnemyHealth에서 호출)
    /// </summary>
    public void ReturnEnemyToPool(ulong networkObjectId)
    {
        if (!IsServer) return;

        if (activeEnemies.TryGetValue(networkObjectId, out NPCMovement_2 enemy))
        {
            activeEnemies.Remove(networkObjectId);
            enemyPool.Release(enemy); // 풀로 반환
            Debug.Log($"Enemy {networkObjectId} returned to pool");
        }
    }

    /// <summary>
    /// NetworkObjectId로 적 반환 (외부 접근용)
    /// </summary>
    public void ReturnEnemyToPool(NetworkObject networkObject)
    {
        if (networkObject != null)
        {
            ReturnEnemyToPool(networkObject.NetworkObjectId);
        }
    }

    private void ClearAllEnemies()
    {
        foreach (var kvp in activeEnemies)
        {
            enemyPool.Release(kvp.Value);
        }
        activeEnemies.Clear();
    }

    #endregion
}