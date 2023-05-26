using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class RadiusDamage : NetworkBehaviour
{
    [SerializeField] private float _radius;
    [SerializeField] private float _damage;

    public override void OnStartAuthority()
    {
        StartCoroutine(CastRadiusDamage());
    }

    private IEnumerator CastRadiusDamage()
    {
        Collider[] all = Physics.OverlapSphere(transform.position, _radius);

        foreach (Collider obj in all)
        {
            NetworkPlayer outPlayer;

            if (obj != null && obj.TryGetComponent<NetworkPlayer>(out outPlayer))
            {
                yield return new WaitUntil(() => outPlayer != null);

                PlayerCurrentStats.Singleton.Damage = _damage;
                outPlayer.CmdHitPlayer(NetworkClient.localPlayer, _damage + PlayerMutationStats.Singleton.Damage);
            }
        }
    }

    private void OnDrawGizmos() 
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
