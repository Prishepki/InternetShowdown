using UnityEngine;

public class DestroyableSound : MonoBehaviour
{
    public float RemoveTime;

    private void Start() 
    {
        Invoke(nameof(DestroySound), RemoveTime);
    }

    private void DestroySound()
    {
        Destroy(gameObject);
    }
}
