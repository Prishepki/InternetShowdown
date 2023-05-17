using UnityEngine;
using Mirror;
using Mirror.Experimental;
using NaughtyAttributes;
using System;

/*
КОРОЧЕ ПАВЕЛ
Если хочешь сделать кастомное повидение прожектайлу, то можешь создать новый скрипт, в классе которого будешь наследовать ЭТОТ класс (ProjectileBase)
Для кастомного повидения перезаписывай уже готовые методы OnCollide(), OnHitPlayer(), OnHitMap(), и т.д. (пустые виртуальные методы)

К ПРИМЕРУ:

public class MyProjectile : ProjectileBase
{
    protected override void OnCollide(int layer)
    {
        if (layer == 2)
        {
            Debug.Log("Ало пошол нахуй");
        }
    }
}

Если ты сделал кастомный скрипт для прожектайла, то удали ProjectileBase скрипт с инспектора и добавь свой скрипт (где у тебя кастомное повидение)

*/
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(NetworkRigidbody))]
public class ProjectileBase : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] protected Rigidbody _rb;
    [SerializeField] protected NetworkRigidbody _nrb;

    [Header("Behaviour Settings")]
    [SerializeField, Tooltip("Из-за чего должен удалиться снаряд? PLAYER HIT НЕ РАБОТАЕТ! я хз почему"), EnumFlags] protected HitDestroy _destroyMode;
    [SerializeField, Tooltip("После скольких секунд снаряд удалится?"), ShowIf(nameof(_destroyMode), HitDestroy.OnTime), AllowNesting] protected float _destroyTime = 3f;

    [Header("Force Settings")]
    [SerializeField, Tooltip("Скорость снаряда")] protected float _projectileSpeed = 10;

    [Space(9)]

    [SerializeField, Tooltip("Как будет применяться скорость снаряду? Через _rb.velocity = force, или через _rb.AddForce(force)?")] protected ForceApplyMode _forceApplyMode = ForceApplyMode.SetForce;
    [SerializeField, Tooltip("Снаряду будет постояно применяться скорость, или только тогда, когда он заспавнился?")] protected bool _continiousForceApply = true;

    private Vector3 _targetDirection;

    protected virtual void OnCollide(int layer) { } // вызывается когда снаряд касается чего либо (в параметр возвращает слой объекта)
    protected virtual void OnHitPlayer() { } // вызывается когда снаряд касается игрока
    protected virtual void OnHitMap() { } // вызывается когда снаряд касается карты

    protected virtual void OnInit() { } // вызывается когда снаряд инициализируется
    protected virtual void OnTime() { } // вызывается так же как и FixedUpdate

    private void OnValidate()
    {   
        if (TryGetComponent<Rigidbody>(out _rb))
        {
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        if (TryGetComponent<NetworkRigidbody>(out _nrb))
        {
            _nrb.syncDirection = SyncDirection.ClientToServer;
            _nrb.clientAuthority = true;
        }
    }

    public void Initialize(Vector3 direction)
    {
        OnInit(); // вызов калбека для кастомного повидения

        _targetDirection = direction;
        gameObject.layer = 10;

        ApplyForce();

        if (!isOwned) return;
        
        if (_destroyMode.HasFlag(HitDestroy.OnTime))
        {
            Invoke(nameof(CmdDestroySelf), _destroyTime); // инвок вызывает метод через время
        }
    }

    private void FixedUpdate()
    {
        OnTime(); // вызов калбека для кастомного повидения

        if (_continiousForceApply)
        {
            ApplyForce();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!isOwned) return;

        OnCollide(other.gameObject.layer); // вызов калбека для кастомного повидения

        if (_destroyMode.HasFlag(HitDestroy.OnCollide))
        {
            CmdDestroySelf();
        }

        if (other.gameObject.layer == 11)
        {
            OnHitPlayer(); // вызов калбека для кастомного повидения

            if (_destroyMode.HasFlag(HitDestroy.OnPlayer))
            {
                CmdDestroySelf();
            }
        }

        if (other.gameObject.layer == 6)
        {
            OnHitMap(); // вызов калбека для кастомного повидения

            if (_destroyMode.HasFlag(HitDestroy.OnMap))
            {
                CmdDestroySelf();
            }
        }
    }

    private void ApplyForce()
    {
        if (!isOwned) return;

        bool isSetForce = _forceApplyMode == ForceApplyMode.SetForce;

        Vector3 targetForce = _targetDirection * _projectileSpeed;
        
        if (isSetForce)
        {
            _rb.velocity = targetForce;
        }

        else if (!isSetForce)
        {
            _rb.AddForce(targetForce);
        }
    }

    [Command]
    private void CmdDestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}

public enum ForceApplyMode
{
    SetForce,
    AddForce
}

[Flags]
public enum HitDestroy
{
    Nothing = 0,
    OnPlayer = 1,
    OnMap = 2,
    OnCollide = 4,
    OnTime = 8
}
