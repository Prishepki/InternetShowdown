using UnityEngine;

public class MetalPipe : ProjectileBase
{
    [Header("Metal Pipe")]
    [SerializeField, Tooltip("Звук который будет БАМ БАРАРАРАМ БИРАПВРИЛОЫИФ")] protected AudioClip _soundClip;

    protected override void OnHitMap()
    {
        FindObjectOfType<SoundSystem>().PlaySyncedSound(new SoundTransporter(_soundClip), transform.position, volume: 2);
    }
}