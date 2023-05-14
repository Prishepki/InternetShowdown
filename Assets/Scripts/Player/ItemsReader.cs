using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ItemsReader : NetworkBehaviour
{
    [SerializeField] private List<UsableItem> _registeredItems = new List<UsableItem>();
    [HideInInspector] public UsableItem CurrentItem; // паша я ещё не добавил визуал предмету, но домой со школы приду сделаю

    private NetworkPlayer _player;

    private void Start()
    {
        _player = GetComponent<NetworkPlayer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLocalPlayer) return; // выходим из метода если игрок не локальный

        PickableItem item;

        if (other.TryGetComponent<PickableItem>(out item)) // тупая идея использовать трайгеткомпонент здесь и сейчас, но вдруг потом пригодится
        {
            if (CurrentItem == null)
            {
                GetItem();
            }
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return; // выходим из метода если игрок не локальный

        if (Input.GetMouseButtonDown(0))
        {
            UseItem();
        }
    }

    public void UseItem()
    {
        if (CurrentItem == null) return; // если у игрока нету предмета в руках, то выходим из метода
        
        foreach (InspectorMutation insMutation in CurrentItem.Mutations) // проходим по каждой инспекторной мутации
        {
            Mutation mutation = MutationJobs.InspectorToMutation(insMutation, _player); // преобразуем её в нормальную
            
            StartCoroutine(mutation.Execute()); // запускаем мутацию на время
        }

        LoseItem(); // теряем предмет из рук
    }

    private void LoseItem()
    {
        CurrentItem = null;
    }

    public void GetItem()
    {
        CurrentItem = _registeredItems[UnityEngine.Random.Range(0, _registeredItems.Count)]; // рандомайзер
    }
}

public static class MutationJobs // этот класс нужен для работ с классами мутаций
{
    public static Mutation InspectorToMutation(InspectorMutation input, NetworkPlayer player) // этот метод способен преобразовать инспекторную мутацию в настоящую
    {
        switch (input.Type)
        {
            case MutationType.Speed:
                return new SpeedMutation(input.ChangeAs, input.Amount, input.Time, player);
            
            case MutationType.Bounce:
                return new BounceMutation(input.ChangeAs, input.Amount, input.Time, player);
        }

        return null;
    }
}
