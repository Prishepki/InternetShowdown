using System;
using System.Collections;
using UnityEngine;

[Serializable]
public abstract class Mutation // базовый класс мутации
{
    public ChangeType ChangeAs;
    public float Amount;
    public float Time;

    protected abstract void OnAdd(); // вызывается когда надо сложить стату
    protected abstract void OnMultiply(); // вызывается когда надо умножить стату

    protected abstract void OnDecrease(); // вызывается когда надо убавить стату
    protected abstract void OnDivide(); // вызывается когда надо разделить стату стату

    protected float _multipliedStats; // переменая для удобности умножения и деления стат

    protected float MultiplyTool(float s) // метод для удобности умножения и деления стат
    {
        return (s * Amount) - s;
    }

    public IEnumerator Execute() // вроде не очень делать такое корутинами, но блять если оно работает то оно работает отъебистесь
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

    public Mutation(ChangeType change, float amount, float time) // конструктор
    {
        ChangeAs = change;
        Amount = amount;
        Time = time;
    }
}

public class SpeedMutation : Mutation // мутация скорости
{
    public SpeedMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        PlayerMutationStats.Singleton.Speed += Amount;
    }

    protected override void OnMultiply()
    {
        _multipliedStats = MultiplyTool(PlayerCurrentStats.Singleton.Speed);

        PlayerMutationStats.Singleton.Speed += _multipliedStats;
    }
    
    protected override void OnDecrease()
    {
        PlayerMutationStats.Singleton.Speed -= Amount;
    }
    
    protected override void OnDivide()
    {
        PlayerMutationStats.Singleton.Speed -= _multipliedStats;
    }
}

public class BounceMutation : Mutation // мутация прыгучести
{
    public BounceMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        PlayerMutationStats.Singleton.Bounce += Amount;
    }

    protected override void OnMultiply()
    {
        _multipliedStats = MultiplyTool(PlayerCurrentStats.Singleton.Bounce);

        PlayerMutationStats.Singleton.Bounce += _multipliedStats;
    }
    
    protected override void OnDecrease()
    {
        PlayerMutationStats.Singleton.Bounce -= Amount;
    }
    
    protected override void OnDivide()
    {
        PlayerMutationStats.Singleton.Bounce -= _multipliedStats;
    }
}

public class LuckMutation : Mutation // мутация удачи
{
    public LuckMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        PlayerMutationStats.Singleton.Luck += ((byte)Amount);
        Debug.Log("youre luck is " + PlayerMutationStats.Singleton.Luck);
    }

    protected override void OnMultiply()
    {
        _multipliedStats = MultiplyTool(PlayerCurrentStats.Singleton.Luck);

        PlayerMutationStats.Singleton.Luck += ((byte)_multipliedStats);
    }
    
    protected override void OnDecrease()
    {
        PlayerMutationStats.Singleton.Luck -= ((byte)Amount);
    }
    
    protected override void OnDivide()
    {
        PlayerMutationStats.Singleton.Luck -= ((byte)_multipliedStats);
    }
}
