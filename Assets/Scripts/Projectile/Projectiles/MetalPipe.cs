using UnityEngine;

public class MetalPipe : ProjectileBase
{
    protected override void OnHitMap(Vector3 velocity, ContactPoint contactPoint)
    {
        _rb.velocity = Vector3.Reflect(velocity.normalized, contactPoint.normal).normalized * 50 + contactPoint.normal * 50;
    }
}
