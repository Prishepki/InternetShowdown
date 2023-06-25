using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public abstract class Mutation // базовый класс мутации
{
    public ChangeType ChangeAs { get; protected set; }
    public float Amount { get; protected set; }
    public float Time { get; protected set; }

    public CancellationTokenSource Source { get; protected set; }

    protected abstract void OnAdd(); // вызывается когда надо сложить стату
    protected abstract void OnMultiply(); // вызывается когда надо умножить стату

    protected abstract void OnDecrease(); // вызывается когда надо убавить стату
    protected abstract void OnDivide(); // вызывается когда надо разделить стату стату

    protected bool _isCanceled;

    protected float _changedStats;

    protected float MultiplyTool(float s) // метод для удобности умножения и деления стат
    {
        return (s * Amount) - s;
    }

    public void Execute() // корутины идут нахуй
    {
        int mili = (int)TimeSpan.FromSeconds(Time).TotalMilliseconds;

        if (ChangeAs == ChangeType.Add)
        {
            OnAdd();

            Task.Delay(mili, Source.Token).ContinueWith(o => { OnDecrease(); });
        }

        else if (ChangeAs == ChangeType.Multiply)
        {
            OnMultiply();

            Task.Delay(mili, Source.Token).ContinueWith(o => { OnDivide(); });
        }
    }

    public void CancelMutation()
    {
        _isCanceled = true;
        Source.Cancel();
    }

    public Mutation(ChangeType change, float amount, float time) // конструктор
    {
        ChangeAs = change;
        Amount = amount;
        Time = time;

        Source = new CancellationTokenSource();
    }

    ~Mutation()
    {
        Debug.Log("GC: Mutation has been disposed!");
    }
}

[Serializable]
public class SpeedMutation : Mutation // мутация скорости
{
    public SpeedMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        _changedStats = Amount;

        PlayerMutationStats.Singleton.Speed += _changedStats;
    }

    protected override void OnMultiply()
    {
        _changedStats = MultiplyTool(PlayerCurrentStats.Singleton.Speed);

        PlayerMutationStats.Singleton.Speed += _changedStats;
    }

    protected override void OnDecrease()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Speed -= _changedStats;
    }

    protected override void OnDivide()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Speed -= _changedStats;
    }
}

[Serializable]
public class BounceMutation : Mutation // мутация прыгучести
{
    public BounceMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        _changedStats = Amount;

        PlayerMutationStats.Singleton.Bounce += _changedStats;
    }

    protected override void OnMultiply()
    {
        _changedStats = MultiplyTool(PlayerCurrentStats.Singleton.Bounce);

        PlayerMutationStats.Singleton.Bounce += _changedStats;
    }

    protected override void OnDecrease()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Bounce -= _changedStats;
    }

    protected override void OnDivide()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Bounce -= _changedStats;
    }
}

[Serializable]
public class LuckMutation : Mutation // мутация удачи
{
    public LuckMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        _changedStats = Amount;

        PlayerMutationStats.Singleton.Luck += ((byte)_changedStats);
    }

    protected override void OnMultiply()
    {
        _changedStats = MultiplyTool(PlayerCurrentStats.Singleton.Luck);

        PlayerMutationStats.Singleton.Luck += ((byte)_changedStats);
    }

    protected override void OnDecrease()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Luck -= ((byte)_changedStats);
    }

    protected override void OnDivide()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Luck -= ((byte)_changedStats);
    }
}

[Serializable]
public class DamageMutation : Mutation // мутация удачи
{
    public DamageMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        _changedStats = Amount;

        PlayerMutationStats.Singleton.Damage += (_changedStats);
    }

    protected override void OnMultiply()
    {
        _changedStats = MultiplyTool(PlayerCurrentStats.Singleton.Damage);

        PlayerMutationStats.Singleton.Damage += (_changedStats);
    }

    protected override void OnDecrease()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Damage -= (_changedStats);
    }

    protected override void OnDivide()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Damage -= (_changedStats);
    }
}
