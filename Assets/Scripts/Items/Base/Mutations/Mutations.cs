using System;
using System.Collections;
using UnityEngine;

[Serializable]
public abstract class Mutation // базовый класс мутации
{
    public ChangeType ChangeAs;
    public float Amount;
    public float Time;

    protected NetworkPlayer _player;

    public abstract void OnAdd(); // вызывается когда надо сложить стату
    public abstract void OnMultiply(); // вызывается когда надо умножить стату

    public abstract void OnDecrease(); // вызывается когда надо убавить стату
    public abstract void OnDivide(); // вызывается когда надо разделить стату стату

    public IEnumerator Execute()
    {
        if (ChangeAs == ChangeType.Add)
        {
            OnAdd();
            yield return new WaitForSeconds(Time);
            OnDecrease();
        }
        
        else if (ChangeAs == ChangeType.Multiply)
        {
            OnMultiply();
            yield return new WaitForSeconds(Time);
            OnDivide();
        }
    }

    public Mutation(ChangeType change, float amount, float time, NetworkPlayer player) // конструктор
    {
        ChangeAs = change;
        Amount = amount;
        Time = time;

        _player = player;
    }
}

public class SpeedMutation : Mutation // мутация скорости
{
    public SpeedMutation(ChangeType change, float amount, float time, NetworkPlayer player) : base(change, amount, time, player) { }

    private float _speedMultiply;

    public override void OnAdd()
    {
        PlayerMutationStats.AdditionalSpeed += Amount;
    }

    public override void OnMultiply()
    {
        _speedMultiply = (_player.TargetMoveForce * Amount) - _player.TargetMoveForce;

        PlayerMutationStats.AdditionalSpeed += _speedMultiply;
    }
    
    public override void OnDecrease()
    {
        PlayerMutationStats.AdditionalSpeed -= Amount;
    }
    
    public override void OnDivide()
    {
        PlayerMutationStats.AdditionalSpeed -= _speedMultiply;
    }
}

public class BounceMutation : Mutation // мутация прыгучести
{
    public BounceMutation(ChangeType change, float amount, float time, NetworkPlayer player) : base(change, amount, time, player) { }

    private float _bounceMultiply;

    public override void OnAdd()
    {
        PlayerMutationStats.AdditionalBounce += Amount;
    }

    public override void OnMultiply()
    {
        _bounceMultiply = (_player.TargetJumpForce * Amount) - _player.TargetJumpForce;

        PlayerMutationStats.AdditionalBounce += _bounceMultiply;
    }
    
    public override void OnDecrease()
    {
        PlayerMutationStats.AdditionalBounce -= Amount;
    }
    
    public override void OnDivide()
    {
        PlayerMutationStats.AdditionalBounce -= _bounceMultiply;
    }
}
