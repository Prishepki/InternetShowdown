using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private float _force;

    private void OnCollisionEnter(Collision other)
    {
        Rigidbody hit;

        if (other.gameObject.TryGetComponent<Rigidbody>(out hit))
        {
            hit.velocity = new Vector3(hit.velocity.x + (transform.up.x * _force), transform.up.y * _force, hit.velocity.z + (transform.up.z * _force));
        }
    }
}
