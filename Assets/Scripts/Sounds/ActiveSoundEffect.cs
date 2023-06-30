using UnityEngine;

public class ActiveSoundEffect : MonoBehaviour
{
    public float RemoveTime;
    public bool Locked { get; private set; }
    private Transform _target;

    public void LockSound(Transform t)
    {
        _target = t;
        Locked = true;
    }

    private void Update()
    {
        if (Locked && _target != null)
        {
            transform.position = _target.position;
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
