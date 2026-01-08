using System;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public EventHandler OnNetWorkPostSpawned;
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    public override void OnNetworkSpawn()
    {
        Debug.Log("GameManager OnNetworkSpawn called");
        // OnNetWorkPostSpawned?.Invoke(this, EventArgs.Empty);
    }
    protected override void OnNetworkPostSpawn()
    {
        Debug.Log("GameManager OnNetworkPostSpawn called");
        // base.OnNetworkPostSpawn();
        OnNetWorkPostSpawned?.Invoke(this, EventArgs.Empty);
    }

}
