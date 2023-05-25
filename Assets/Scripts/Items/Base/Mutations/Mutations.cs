using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public abstract class Mutation // базовый класс мутации
{
    public ChangeType ChangeAs;
    public float Amount;
    public float Time;

    public CancellationTokenSource Source { get; protected set; }

    protected abstract void OnAdd(); // вызывается когда надо сложить стату
    protected abstract void OnMultiply(); // вызывается когда надо умножить стату

    protected abstract void OnDecrease(); // вызывается когда надо убавить стату
    protected abstract void OnDivide(); // вызывается когда надо разделить стату стату

    protected bool _isCanceled;

    protected float _multipliedStats; // переменая для удобности умножения и деления стат

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
            
            try
            {
                Task.Delay(mili, Source.Token).ContinueWith(o => { OnDecrease(); });
            }
            catch (OperationCanceledException) when (Source.IsCancellationRequested)
            {
                Debug.Log("Mutation Canceled");
                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        else if (ChangeAs == ChangeType.Multiply)
        {
            OnMultiply();
            
            try
            {
                Task.Delay(mili, Source.Token).ContinueWith(o => { OnDivide(); });
            }
            catch (OperationCanceledException) when (Source.IsCancellationRequested)
            {
                Debug.Log("Mutation Canceled");
                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
        PlayerMutationStats.Singleton.Speed += Amount;
    }

    protected override void OnMultiply()
    {
        _multipliedStats = MultiplyTool(PlayerCurrentStats.Singleton.Speed);

        PlayerMutationStats.Singleton.Speed += _multipliedStats;
    }
    
    protected override void OnDecrease()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Speed -= Amount;
    }
    
    protected override void OnDivide()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Speed -= _multipliedStats;
    }
}

[Serializable]
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
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Bounce -= Amount;
    }
    
    protected override void OnDivide()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Bounce -= _multipliedStats;
    }
}

[Serializable]
public class LuckMutation : Mutation // мутация удачи
{
    public LuckMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        PlayerMutationStats.Singleton.Luck += ((byte)Amount);
    }

    protected override void OnMultiply()
    {
        _multipliedStats = MultiplyTool(PlayerCurrentStats.Singleton.Luck);

        PlayerMutationStats.Singleton.Luck += ((byte)_multipliedStats);
    }
    
    protected override void OnDecrease()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Luck -= ((byte)Amount);
    }
    
    protected override void OnDivide()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Luck -= ((byte)_multipliedStats);
    }
}

[Serializable]
public class DamageMutation : Mutation // мутация удачи
{
    public DamageMutation(ChangeType change, float amount, float time) : base(change, amount, time) { }

    protected override void OnAdd()
    {
        PlayerMutationStats.Singleton.Damage += ((byte)Amount);
    }

    protected override void OnMultiply()
    {
        _multipliedStats = MultiplyTool(PlayerCurrentStats.Singleton.Damage);

        PlayerMutationStats.Singleton.Damage += ((byte)_multipliedStats);
    }
    
    protected override void OnDecrease()
    {
        if (_isCanceled) return;

        PlayerMutationStats.Singleton.Damage -= ((byte)Amount);
    }
    
    protected override void OnDivide()
    {
        if (_isCanceled) return;
        
        PlayerMutationStats.Singleton.Damage -= ((byte)_multipliedStats);
    }
}
