using UnityEngine;
using Mirror;
using Mirror.Experimental;
using NaughtyAttributes;

public class MetalPipe : ProjectileBase
{
    [Header("Metal Pipe")]
    [SerializeField, Tooltip("Источник звука")] protected AudioSource _soundSource;
    [SerializeField, Tooltip("Сам звук")] protected AudioClip _soundClip;

    override public void OnHitMap() {
        _soundSource.PlayOneShot(_soundClip);
    }
}