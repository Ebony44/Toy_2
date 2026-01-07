using Unity.Netcode;
using UnityEngine;

public class ManagedObjectInfoManager : NetworkBehaviour
{
    // NetworkList<ProjectileInfo> managedObjectInfos;
    NetworkList<ProjectileInfo> projectileInfos = new NetworkList<ProjectileInfo>();
    // NetworkList<>
}
