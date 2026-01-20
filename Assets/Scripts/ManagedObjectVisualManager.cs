using System;
using UnityEngine;

public class ManagedObjectVisualManager : MonoBehaviour
{
    // pickups, collectibles
    // projectiles, bullets
    // destructible environment
    // visual effects

    public static ManagedObjectVisualManager Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
        // Time.frameCount
        var theta = Time.frameCount / 10.0f;
        transform.position = new Vector3((float)Math.Cos(theta), 0.0f, (float)Math.Sin(theta));
        // at 0 degree, cos = 1, sin = 0
        // at 90 degree, cos = 0, sin = 1
        // at 180 degree, cos = -1, sin = 0
        // at 270 degree, cos = 0, sin = -1
    }
}
