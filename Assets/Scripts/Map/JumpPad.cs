using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float _force;
    [SerializeField] private AudioClip _sound;

    private void OnCollisionEnter(Collision other)
    {
        Rigidbody hit;

        if (other.gameObject.TryGetComponent<Rigidbody>(out hit))
        {
            // я в ахуе
            hit.velocity = new Vector3
            (
                hit.velocity.x + (transform.up.x * _force),
                transform.up.y * _force,
                hit.velocity.z + (transform.up.z * _force)
            );

            SoundSystem.PlaySound(new SoundTransporter(_sound), new SoundPositioner(transform.position), SoundType.SFX, volume: 0.45f, pitchMin: 0.95f, pitchMax: 1.1f);
        }
    }
}
