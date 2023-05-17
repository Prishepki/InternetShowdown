using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ItemsReader : NetworkBehaviour
{
    [SerializeField] private byte _luckModifier = 0;
    [SerializeField] private Transform _itemHolder;
    [SerializeField] private List<UsableItem> _registeredItems = new List<UsableItem>();

    private UsableItem _currentItem;

    private NetworkPlayer _player;

    private GameObject _currentVisual;

    private void Start()
    {
        _player = GetComponent<NetworkPlayer>();

        if (!isLocalPlayer) return;
        _itemHolder.SetParent(_player.PlayerCamera.transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLocalPlayer) return; // выходим из метода если игрок не локальный

        PickableItem item;

        if (other.TryGetComponent<PickableItem>(out item)) // тупая идея использовать трайгеткомпонент здесь и сейчас, но вдруг потом пригодится
        {
            if (_currentItem == null)
            {
                GetItem();
            }
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return; // выходим из метода если игрок не локальный

        CheckForItem();
    }

    private void CheckForItem()
    {
        if (_currentItem == null) return; // если у игрока нету предмета в руках, то выходим из метода

        bool holdToUse = _currentItem.HoldToUse;

        if (holdToUse)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Invoke(nameof(UseItem), _currentItem.UseTime);
            }

            if (Input.GetMouseButtonUp(0))
            {
                CancelInvoke(nameof(UseItem));
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) // 0 - лкм (на случай альгеймера у паши или у никиты)
            {
                UseItem();
            }
        }
    }

    public void UseItem()
    {
        foreach (InspectorMutation insMutation in _currentItem.Mutations) // проходим по каждой инспекторной мутации
        {
            Mutation mutation = MutationJobs.InspectorToMutation(insMutation); // преобразуем её в нормальную
            
            StartCoroutine(mutation.Execute()); // запускаем мутацию на время
        }

        for (int i = 0; i < _currentItem.Projectiles.Count; i++)
        {
            CmdSpawnProjectile(i, _player.PlayerCamera.transform.forward, connectionToClient);
        }

        LoseItem(); // теряем предмет из рук
    }

    private void LoseItem()
    {
        SetCurrentItem(null);
        RemoveVisual();
    }

    public void GetItem() // меня касирша послала нахуй // да пошёл ты нахуй
    {
        PlayerCurrentStats.Singleton.Luck = _luckModifier;

        int choice;
        List<UsableItem> sortedItems = new List<UsableItem>();
        Rarity closestRarity;

        (choice, sortedItems, closestRarity) = Generate();

        while (RarityJobs.ItemRaritySortHelper(sortedItems, closestRarity).Count == 0)
        {
            (choice, sortedItems, closestRarity) = Generate();
        }

        List<UsableItem> choosedCategory = new List<UsableItem>();

        foreach (var item in sortedItems)
        {
            if (item.ItemRarity == closestRarity)
            {
                choosedCategory.Add(item);
            }
        }

        Debug.Log("youre choice is " + choice);
        
        SetCurrentItem(choosedCategory[UnityEngine.Random.Range(0, choosedCategory.Count)]);
    }

    private (int choice, List<UsableItem> sortedItems, Rarity closestRarity) Generate()
    {
        byte choice = RarityJobs.ChoicRandomizer((byte)(PlayerCurrentStats.Singleton.Luck + PlayerMutationStats.Singleton.Luck));

        List<UsableItem> sortedItems = RarityJobs.SortAllItems(_registeredItems).ToList();

        var closestRarity = RarityJobs.Rarities.ToList<KeyValuePair<string, byte>>().OrderBy((value) => Math.Abs(choice - value.Value)).First(); 
        Rarity convertedClosestRarity = RarityJobs.KeyValueRarityToRarity(closestRarity);

        return (choice, sortedItems, convertedClosestRarity);
    }

    private void MakeVisual(GameObject visual)
    {
        _currentVisual = Instantiate(visual, _itemHolder);
    }

    private void RemoveVisual()
    {
        Destroy(_currentVisual);
    }

    private void OnCurrentItemChange()
    {
        if (isLocalPlayer)
        {
            if (_currentItem != null)
            {
                MakeVisual(_currentItem.ItemVisual);
            }
        }
    }

#region NETWORK

    [Command]
    private void CmdSpawnProjectile(int idx, Vector3 dir, NetworkConnectionToClient connection)
    {
        GameObject newProjectile = Instantiate(connection.identity.GetComponent<ItemsReader>()._currentItem.Projectiles[idx].gameObject, transform.position, Quaternion.identity);
        NetworkServer.Spawn(newProjectile, connection);

        RpcOnProjectileSpawned(newProjectile, dir);
    }

    [ClientRpc]
    private void RpcOnProjectileSpawned(GameObject proj, Vector3 dir)
    {
        if (proj != null)
        {
            proj.GetComponent<ProjectileBase>().Initialize(dir);
        }
    }

    private void SetCurrentItem(UsableItem target)
    {
        List<UsableItem> toCheck = _registeredItems;
        toCheck.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        if (target == null)
        {
            CmdSetCurrentItem(null);
        }
        else
        {
            CmdSetCurrentItem(toCheck.IndexOf(target));
        }
    }

    [Command]
    private void CmdSetCurrentItem(int? idx)
    {
        RpcSetCurrentItem(idx);
    }

    [ClientRpc]
    private void RpcSetCurrentItem(int? idx)
    {
        List<UsableItem> toCheck = _registeredItems;
        toCheck.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        if (idx == null)
        {
            _currentItem = null;
        }
        else
        {
            _currentItem = toCheck[idx.Value];
        }

        OnCurrentItemChange();
    }

#endregion
}

public static class MutationJobs // этот класс нужен для работ с классами мутаций
{
    public static Mutation InspectorToMutation(InspectorMutation input) // этот метод способен преобразовать инспекторную мутацию в настоящую
    {
        switch (input.Type)
        {
            case MutationType.Speed:
                return new SpeedMutation(input.ChangeAs, input.Amount, input.Time);
            
            case MutationType.Bounce:
                return new BounceMutation(input.ChangeAs, input.Amount, input.Time);

            case MutationType.Luck:
                return new LuckMutation(input.ChangeAs, input.Amount, input.Time);
        }

        return null;
    }
}
