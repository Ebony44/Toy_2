using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class EnemyInfo
{
    private string enemyName;
    private int health;
}

public struct EnemyInfoStruct : INetworkSerializable, IEquatable<EnemyInfoStruct>
{
    // public string enemyName;
    public FixedString32Bytes name;
    public int health;

    public bool Equals(EnemyInfoStruct other)
    {
        var result = name.Equals(other.name) && health == other.health;
        return result;
        // return health == other.health;
        // throw new NotImplementedException();
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // serializer.SerializeValue(ref enemyName);
        serializer.SerializeValue(ref health);
        // throw new System.NotImplementedException();
    }
}