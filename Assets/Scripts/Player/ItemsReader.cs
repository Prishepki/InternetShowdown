using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ItemsReader : NetworkBehaviour
{
    [SerializeField] private byte _luckModifier = 0;
    [SerializeField] private Transform _itemHolder;
    private List<UsableItem> _registeredItems = new List<UsableItem>();

    public bool HasItem { get => _currentItem != null; }

    private UsableItem _currentItem;

    private NetworkPlayer _player;
    private EverywhereCanvas _everywhereCanvas;

    public List<Mutation> ActiveMutations = new List<Mutation>();

    private int _itemsUsed;

    private void Awake()
    {
        _registeredItems = Resources.LoadAll<UsableItem>("Items").ToList();
    }

    private void Start()
    {
        _player = GetComponent<NetworkPlayer>();
        _everywhereCanvas = EverywhereCanvas.Singleton();

        if (!isLocalPlayer) return;
        _itemHolder.SetParent(_player.PlayerCamera.transform);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isLocalPlayer) return; // выходим из метода если игрок не локальный

        if (other.GetComponent<PickableItem>())
        {
            if (!HasItem)
            {
                CmdDestroyObject(other.gameObject);

                GetItem();
            }
        }
    }

    [Command]
    private void CmdDestroyObject(GameObject gameObject)
    {
        NetworkServer.Destroy(gameObject);
    }

    private void Update()
    {
        if (!isLocalPlayer) return; // выходим из метода если игрок не локальный

        CheckForItem();
    }

    private void CheckForItem()
    {
        if (!HasItem) return; // если у игрока нету предмета в руках, то выходим из метода

        if (!_player.AllowMovement) return;

        bool holdToUse = _currentItem.HoldToUse;

        if (holdToUse)
        {
            if (Input.GetMouseButtonDown(0))
            {
                float useTime = _currentItem.UseTime;

                Invoke(nameof(UseItem), useTime);
                _everywhereCanvas.StartUseTimer(useTime);
            }

            if (Input.GetMouseButtonUp(0))
            {
                CancelInvoke(nameof(UseItem));
                _everywhereCanvas.CancelUseTimer();
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
        if (!HasItem)
        {
            Debug.LogWarning("Can't use item because it's NULL");
            return;
        }

        if (!_player.AllowMovement) return;

        foreach (InspectorMutation insMutation in _currentItem.Mutations) // проходим по каждой инспекторной мутации
        {
            Mutation mutation = MutationJobs.InspectorToMutation(insMutation); // преобразуем её в нормальную

            mutation.Execute(); // запускаем мутацию на время

            ActiveMutations.Add(mutation);

            StartCoroutine(nameof(CancelMutationFromList), mutation);
        }

        foreach (var proj in _currentItem.Projectiles)
        {
            SpawnProjectile(proj);
        }

        if (_currentItem.HealAmount > 0)
        {
            _player.Heal(_currentItem.HealAmount);
        }
        else if (_currentItem.HealAmount < 0)
        {
            _player.TakeDamage(Mathf.Abs(_currentItem.HealAmount));
        }

        LoseItem(); // теряем предмет из рук

        _player.OnItemUsed();
    }

    private IEnumerator CancelMutationFromList(Mutation mutation)
    {
        yield return new WaitForSeconds(mutation.Time);

        ActiveMutations.Remove(mutation);
    }

    public void RemoveAllMutations()
    {
        foreach (var mutation in ActiveMutations)
        {
            mutation.CancelMutation();
        }

        PlayerMutationStats.Singleton.ResetStats();
    }

    public void LoseItem()
    {
        if (!HasItem)
        {
            Debug.LogWarning("Can not lose NULL item");
            return;
        }

        StartCoroutine(nameof(LoseItemCoroutine));
    }

    private IEnumerator LoseItemCoroutine()
    {
        yield return new WaitUntil(() => _itemHolder.childCount > 0);

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

        SetCurrentItem(choosedCategory[UnityEngine.Random.Range(0, choosedCategory.Count)]);
    }

    private (int choice, List<UsableItem> sortedItems, Rarity closestRarity) Generate()
    {
        byte choice = RarityJobs.ChoicRandomizer((byte)(PlayerCurrentStats.Singleton.Luck + PlayerMutationStats.Singleton.Luck));

        List<UsableItem> sortedItems = RarityJobs.SortAllItems(_registeredItems).ToList();

        var closestRarity = RarityJobs.Rarities.ToList<KeyValuePair<string, byte>>();
        closestRarity.Sort((first, second) => second.Value > choice ? 1 : -1);

        Rarity convertedClosestRarity = RarityJobs.KeyValueRarityToRarity(closestRarity.First());

        return (choice, sortedItems, convertedClosestRarity);
    }

    private void MakeVisual(GameObject visual)
    {
        RemoveVisual();
        Instantiate(visual, _itemHolder);
    }

    private void RemoveVisual()
    {
        foreach (Transform item in _itemHolder)
        {
            Destroy(item.gameObject);
        }
    }

    private void OnCurrentItemChange()
    {
        if (isLocalPlayer)
        {
            if (HasItem)
            {
                MakeVisual(_currentItem.ItemVisual);
            }
        }
    }

    private void SpawnProjectile(ProjectileBase proj)
    {
        Transform cameraTransform = _player.PlayerCamera.transform;

        CmdSpawnProjectile(_currentItem.Projectiles.IndexOf(proj), transform.position + cameraTransform.forward, cameraTransform.rotation, connectionToClient);
    }

    #region NETWORK

    [Command]
    private void CmdSpawnProjectile(int idx, Vector3 pos, Quaternion dir, NetworkConnectionToClient connection)
    {
        ItemsReader client = connection.identity.GetComponent<ItemsReader>();

        if (client._currentItem == null)
        {
            Debug.LogWarning("Cannot spawn projectile from NULL item");
            return;
        }

        GameObject newProjectile = Instantiate(client._currentItem.Projectiles[idx].gameObject, pos, dir);
        NetworkServer.Spawn(newProjectile, connection);

        RpcOnProjectileSpawned(newProjectile);
    }

    [ClientRpc]
    private void RpcOnProjectileSpawned(GameObject proj) { }

    private void SetCurrentItem(UsableItem target)
    {
        _registeredItems.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        _currentItem = target;

        if (target == null)
        {
            CmdSetCurrentItem(null);
        }
        else
        {
            CmdSetCurrentItem(_registeredItems.IndexOf(target));
        }
    }

    [Command]
    private void CmdSetCurrentItem(int? idx)
    {
        _registeredItems.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        if (idx == null)
        {
            _currentItem = null;
        }
        else
        {
            _currentItem = _registeredItems[idx.Value];
        }

        RpcSetCurrentItem(idx);
    }

    [ClientRpc]
    private void RpcSetCurrentItem(int? idx)
    {
        _registeredItems.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        if (idx == null)
        {
            _currentItem = null;
        }
        else
        {
            _currentItem = _registeredItems[idx.Value];
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

            case MutationType.Damage:
                return new DamageMutation(input.ChangeAs, input.Amount, input.Time);
        }

        return null;
    }
}
