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
}
