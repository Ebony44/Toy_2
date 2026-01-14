using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectileManager_Obsolete : NetworkBehaviour
{
    private NetworkList<ProjectileInfo> projectileInfos;

    private Dictionary<int, GameObject> visualProjectiles = new Dictionary<int, GameObject>(INIT_POOL_SIZE);

    [SerializeField] private GameObject projectilePrefab;

    private IObjectPool<GameObject> projectilePool;

    private const int INIT_POOL_SIZE = 32;


    #region for interpolation (if needed)
    private Dictionary<int, Vector3> previousPositions = new Dictionary<int, Vector3>();
    private float lastSyncTime = 0f;
    #endregion

    // client request or host invoke...
    // adding projectileInfos
    // NetworkList changed event will call
    // -> SpawnVisualProjectile
    // then objectpool's Get() to spawn visual projectile


    private void Awake()
    {
        projectileInfos = new NetworkList<ProjectileInfo>(default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

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

    public override void OnDestroy()
    {
        base.OnDestroy();
        projectileInfos.Dispose();
    }
    

    public override void OnNetworkSpawn()
    {
        projectileInfos.OnListChanged += OnProjectileInfosChanged;

        if(!IsServer)
        {
            // mid or late joined clients need to spawn existing projectiles ?
            // no need, game does NOT support mid-game joining
            // if they disconnected, they rejoined after battle ended

        }

    }

    public override void OnNetworkDespawn()
    {
        projectileInfos.OnListChanged -= OnProjectileInfosChanged;
    }


    #region Server side updates
    private int _nextId = 0;

    public void FireProjectile(ProjectileInfo paramInfo)
    {
        if (!IsServer) return;
        var newProjectileInfo = paramInfo;
        newProjectileInfo.projectileId = _nextId++;
        projectileInfos.Add(newProjectileInfo);
    }

    public void FireProjectile(Vector2 spawnPosition, Vector2 velocity, ushort projectileTypeId)
    {
        //if (!IsServer) return;
        //var newProjectileInfo = new ProjectileInfo
        //{
        //    ProjectileTypeId = projectileTypeId,
        //    projectileId = _nextId++,
        //    SpawnPosition = spawnPosition,
        //    Velocity = velocity,
        //    currentLifeTime = 0f,
        //    lifeTime = 2f,
        //};
        //projectileInfos.Add(newProjectileInfo);

        // refactor with above method
        var newProjectileInfo = new ProjectileInfo
        {
            ProjectileTypeId = projectileTypeId,
            SpawnPosition = spawnPosition,
            Velocity = velocity,
            currentLifeTime = 0f,
            lifeTime = 2f,
        };
        FireProjectile(newProjectileInfo);


    }
    public void OnProjectileHit(int projectileId)
    {
        // Remove projectile from the list
        // method name should be changed?

        if (!IsServer) return;

        Debug.Log($"Projectile {projectileId} hit something or expired.");

        // Find the index of the projectile with the given projectileId
        int indexToRemove = -1;
        for (int i = 0; i < projectileInfos.Count; i++)
        {
            if (projectileInfos[i].projectileId == projectileId)
            {
                indexToRemove = i;
                break;
            }
        }
        if (indexToRemove != -1)
        {
            projectileInfos.RemoveAt(indexToRemove);
        }
        else
        {
            Debug.LogError($"Projectile with ID {projectileId} not found.");
        }
    }

    private void FixedUpdate()
    {
        if(!IsServer) return;



        for (int i = projectileInfos.Count - 1; i >= 0; i--)
        {
            var info = projectileInfos[i];

            info.currentLifeTime += Time.fixedDeltaTime;
            if (info.lifeTime > 0f && info.currentLifeTime >= info.lifeTime)
            {
                Debug.Log($"Projectile ID {info.projectileId} expired after {info.currentLifeTime} seconds.");
                OnProjectileHit(info.projectileId);
                continue;
            }


            // 1. calculate movement for this tick
            float moveDistance = info.Velocity.magnitude * Time.fixedDeltaTime;
            Vector2 direction = info.Velocity.normalized;

            // 2. server-side collision detection?
            if(Physics.Raycast(info.SpawnPosition, direction, out RaycastHit hitInfo, moveDistance))
            {
                // Hit something
                // OnProjectileHit(info.projectileId);
                Debug.Log($"Projectile ID {info.projectileId} hit {hitInfo.collider.name} at position {hitInfo.point}");
                HandleServerSideHit(hitInfo, info.projectileId, i);
                continue; // Skip updating position since projectile is removed
            }
            else
            {
                // move projectile
                // info.SpawnPosition += direction * moveDistance;
                Debug.Log($"Projectile ID {info.projectileId} moving from {info.SpawnPosition} by {info.Velocity * Time.fixedDeltaTime}");
                Debug.Log($"Before: {projectileInfos[i].SpawnPosition}");
                info.SpawnPosition += info.Velocity * Time.fixedDeltaTime;
                Debug.Log($"After: {info.SpawnPosition}");
                Debug.Log($"Is dirty before assignment? " + projectileInfos.IsDirty());
                projectileInfos[i] = info;

                Debug.Log($"Updated projectile ID {projectileInfos[i].projectileId} position to {projectileInfos[i].SpawnPosition}"
                    + " is dirty? " + projectileInfos.IsDirty()
                    );
            }


            //Vector2 newPosition = info.SpawnPosition + info.Velocity * Time.fixedDeltaTime;
            //info.SpawnPosition = newPosition;
            //projectileInfos[i] = info;
        }
    }
    private void HandleServerSideHit(RaycastHit hit, int id, int listIndex)
    {
        // Apply damage, effects, etc. based on hit info
        // ...
        // Remove projectile from the list

        // TODO: health logic goes below
        // if(hit.collider.TryGetComponent(out ))
        //

        // projectileInfos.RemoveAt(listIndex);
        OnProjectileHit(id);
    }

    #endregion Server side updates end


    #region Client side visual updates
    #region prev update
    //private void Update()
    //{
    //    // "Server as the Master, Client as the Mirror" ?
    //    // client works as a slave to server's projectileInfos?

    //    // Update visual projectile positions
    //    foreach (var currentVisual in visualProjectiles)
    //    {
    //        int projectileId = currentVisual.Key;
    //        GameObject currentObj = currentVisual.Value;
    //        // Find the corresponding ProjectileInfo
    //        ProjectileInfo info = GetInfoById(projectileId);

    //        currentObj.transform.position = info.SpawnPosition;


    //        // Smooth Interpolation?
    //        // If the server's FixedUpdate is slow, you can use Lerp here
    //        // to make the movement look smooth on high-refresh monitors.
    //    }
    //}




    #endregion


    private void Update()
    {
        // 보간 비율 계산
        float t = Mathf.Clamp01((Time.time - lastSyncTime) / Time.fixedDeltaTime);

        foreach (var currentVisual in visualProjectiles)
        {
            int projectileId = currentVisual.Key;
            GameObject currentObj = currentVisual.Value;
            ProjectileInfo info = GetInfoById(projectileId);

            Vector3 currentPos = info.SpawnPosition;

            // 이전 위치가 없으면 현재 위치로 초기화
            if (!previousPositions.TryGetValue(projectileId, out Vector3 prevPos))
            {
                prevPos = currentPos;
                previousPositions[projectileId] = prevPos;
            }

            // Lerp로 보간
            currentObj.transform.position = Vector3.Lerp(prevPos, currentPos, t);
        }
    }


    private ProjectileInfo GetInfoById(int id)
    {
        foreach (var info in projectileInfos)
        {
            if (info.projectileId == id) return info;
        }
        return default;
    }
    #endregion


    private void OnProjectileInfosChanged(NetworkListEvent<ProjectileInfo> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<ProjectileInfo>.EventType.Add:
                {
                    var newProjectileInfo = changeEvent.Value;
                    previousPositions[newProjectileInfo.projectileId] = newProjectileInfo.SpawnPosition;
                    SpawnVisualProjectile(newProjectileInfo);
                    lastSyncTime = Time.time;
                    break;
                }
            case NetworkListEvent<ProjectileInfo>.EventType.Remove:
                {
                    var index = changeEvent.Index;
                    var info = projectileInfos[index];
                    previousPositions.Remove(info.projectileId);
                    DespawnVisualProjectile(index);
                    lastSyncTime = Time.time;
                    break;
                }
            case NetworkListEvent<ProjectileInfo>.EventType.Clear:
                {
                    previousPositions.Clear();
                    ClearAllVisualProjectiles();
                    lastSyncTime = Time.time;
                    break;
                }
            //case NetworkListEvent<ProjectileInfo>.EventType.Value:
            //    {
            //        Debug.LogWarning("ProjectileInfo Value change event not handled.");
            //        //var updatedInfo = changeEvent.Value;
            //        //previousPositions[updatedInfo.projectileId] = updatedInfo.SpawnPosition;
            //        //lastSyncTime = Time.time;
            //        break;
            //    }
                // Handle other event types if necessary
        }
    }

    private void ClearAllVisualProjectiles()
    {
        foreach (var visual in visualProjectiles)
        {
            projectilePool.Release(visual.Value);
        }
        visualProjectiles.Clear();
    }

    private void DespawnVisualProjectile(int index)
    {
        if (visualProjectiles.TryGetValue(index, out GameObject visual))
        {
            projectilePool.Release(visual);
            visualProjectiles.Remove(index);
        }

    }

    private void SpawnVisualProjectile(ProjectileInfo newProjectileInfo)
    {
        GameObject visual = projectilePool.Get();
        visual.transform.position = newProjectileInfo.SpawnPosition;
        visual.transform.rotation = Quaternion.LookRotation(newProjectileInfo.Velocity.normalized);


        visualProjectiles[newProjectileInfo.projectileId] = visual;
        Debug.Log($"Spawning visual projectile ID {newProjectileInfo.projectileId} at {newProjectileInfo.SpawnPosition} "
            + " count of visualProjectiles: " + visualProjectiles.Count
            + " count of projectileInfos: " + projectileInfos.Count
            );

        //GameObject visual = Instantiate(projectilePrefab, newProjectileInfo.SpawnPosition, Quaternion.identity);
        //if(index >= projectileInfos.Count)
        //{
        //    // Debug.LogError("Index out of range when spawning visual projectile.");
        //    // Destroy(visual);
        //    // return;
        //    projectileInfos.Add(newProjectileInfo);
        //}
        //else
        //{
        //    projectileInfos[index] = newProjectileInfo;
        //}
        //// projectileInfos.Add(newProjectileInfo);
        //// throw new NotImplementedException();
    }


    public void TestFireProjectile()
    {
        if(!IsServer)
        {
            Debug.LogError("Only the server can fire projectiles.");
        }
        
        Vector2 spawnPosition = Vector2.zero;
        Vector2 velocity = new Vector2(1, 2).normalized * 4f;
        ushort projectileTypeId = 0;

        FireProjectile(spawnPosition, velocity, projectileTypeId);
    }


    [Rpc(SendTo.Server)]
    public void FireRequestFromClientRpc(Vector2 spawnPosition, Vector2 velocity, ushort projectileTypeId)
    {
        if (!IsServer)
        {
            Debug.Log("FireRequestFromClientRpc called on non-server instance.");
        }
        else
        {
            Debug.Log("FireRequestFromClientRpc received on server.");
        }
        FireProjectile(spawnPosition, velocity, projectileTypeId);
    }


}
