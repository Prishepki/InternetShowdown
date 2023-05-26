using UnityEngine;

public class ActiveSoundEffect : MonoBehaviour
{
    public float RemoveTime;
    public bool Locked;
    private Transform target;

    public void LockSound(Transform t)
    {
        target = t;
        Locked = true;
    }

    private void Update()
    {
        if (Locked && target != null)
        {
            transform.position = target.position;
        }
    }

    private void Start()
    {
        Invoke(nameof(DestroySound), RemoveTime);
    }

    private void DestroySound()
    {
        Destroy(gameObject);
    }
}
