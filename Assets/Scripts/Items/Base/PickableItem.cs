using Mirror;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PickableItem : NetworkBehaviour
{
    private void Awake()
    {
        BoxCollider bc;

        if (TryGetComponent<BoxCollider>(out bc))
        {
            bc.isTrigger = true;
        }
    }

    private void FixedUpdate()
    {
        transform.Rotate(Vector3.up, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        ItemsReader reader;

        if (other.TryGetComponent<ItemsReader>(out reader))
        {
            if (!reader.HasItem)
            {
                Destroy(gameObject);
            }
        }
    }
}
