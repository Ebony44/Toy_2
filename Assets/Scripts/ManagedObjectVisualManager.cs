using UnityEngine;

public class ManagedObjectVisualManager : MonoBehaviour
{
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
    }
}
