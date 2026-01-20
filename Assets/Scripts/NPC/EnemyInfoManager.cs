using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


// Rule no.1
// not hundreds of enemy info...of networkobjects
// Just one manager that holds them all

// and some manager like charge of visuals
// will read from this manager

public class EnemyInfoManager : NetworkBehaviour
{
    // NetworkObject
    // public List<EnemyInfo> enemyInfoList = new List<EnemyInfo>();
    private NetworkList<EnemyInfoStruct> enemyInfoList;

    private void Awake()
    {
        enemyInfoList = new NetworkList<EnemyInfoStruct>();
    }
}
