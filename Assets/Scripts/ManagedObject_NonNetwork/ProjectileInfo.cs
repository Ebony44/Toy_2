using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct ProjectileInfo : INetworkSerializable, IEquatable<ProjectileInfo>
{
    public ushort ProjectileTypeId; // Maps to a ScriptableObject/Prefab
    public int projectileId;      // Unique Id for this projectile instance
    public Vector2 SpawnPosition;
    public Vector2 Velocity;        // Includes direction and speed

    public float lifeTime;        // How long this projectile has existed
    public float currentLifeTime; // How long this projectile has existed


    public bool Equals(ProjectileInfo other)
    {
        var result = projectileId == other.projectileId &&
                     ProjectileTypeId == other.ProjectileTypeId;

        return result;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ProjectileTypeId);
        serializer.SerializeValue(ref SpawnPosition);
        serializer.SerializeValue(ref Velocity);
        serializer.SerializeValue(ref projectileId);
        serializer.SerializeValue(ref lifeTime);
        serializer.SerializeValue(ref currentLifeTime);

    }
}
