using System.Collections;
using Mirror;
using UnityEngine;

public class RadiusDamage : NetworkBehaviour
{
    [SerializeField] private float _radius;
    [SerializeField] private float _damage;

    public override void OnStartAuthority()
    {
        CastRadiusDamage();
    }

    private void CastRadiusDamage()
    {
        Collider[] all = Physics.OverlapSphere(transform.position, _radius);

        foreach (Collider obj in all)
        {
            NetworkPlayer outPlayer;

            if (obj.TryGetComponent<NetworkPlayer>(out outPlayer))
            {
                PlayerCurrentStats.Singleton.Damage = _damage;
                outPlayer.CmdHitPlayer(NetworkClient.localPlayer, _damage + PlayerMutationStats.Singleton.Damage);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = ColorISH.Yellow;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
