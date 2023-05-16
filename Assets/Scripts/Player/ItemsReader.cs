using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ItemsReader : NetworkBehaviour
{
    [SerializeField] private Transform _itemHolder;
    [SerializeField] private List<UsableItem> _registeredItems = new List<UsableItem>();
    private UsableItem _currentItem; // паша я ещё не добавил визуал предмету, но домой со школы приду сделаю

    private NetworkPlayer _player;

    private GameObject _currentVisual;

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
            if (_currentItem == null)
            {
                GetItem();
            }
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return; // выходим из метода если игрок не локальный

        if (Input.GetMouseButtonDown(0)) // если лкм нажата
        {
            UseItem();
        }
    }

    public void UseItem()
    {
        if (_currentItem == null) return; // если у игрока нету предмета в руках, то выходим из метода
        
        foreach (InspectorMutation insMutation in _currentItem.Mutations) // проходим по каждой инспекторной мутации
        {
            Mutation mutation = MutationJobs.InspectorToMutation(insMutation); // преобразуем её в нормальную
            
            StartCoroutine(mutation.Execute()); // запускаем мутацию на время
        }

        LoseItem(); // теряем предмет из рук
    }

    private void LoseItem()
    {
        _currentItem = null;
        RemoveVisual();
    }

    public void GetItem() // аааааааа
    {
        // я в полном абалдуе
        void Generate(out int genChoice, out List<UsableItem> genSortedItems, out Rarity genClosestRarity)
        {
            int choice = UnityEngine.Random.Range(0, 255);

            List<UsableItem> sortedItems = RarityJobs.SortAllItems(_registeredItems.ToArray()).ToList();

            var closestRarity = RarityJobs.Rarities.ToList<KeyValuePair<string, byte>>().OrderBy((value) => Math.Abs(choice - value.Value)).First(); 
            Rarity convertedClosestRarity = RarityJobs.KeyValueRarityToRarity(closestRarity);

            genChoice = choice; genSortedItems = sortedItems; genClosestRarity = convertedClosestRarity;
        }

        // я больше не в полном абалдуе

        int choice;
        List<UsableItem> sortedItems = new List<UsableItem>();
        Rarity closestRarity;

        Generate(out choice, out sortedItems, out closestRarity);

        while (RarityJobs.ItemRaritySortHelper(sortedItems.ToArray(), closestRarity).Length == 0)
        {
            Generate(out choice, out sortedItems, out closestRarity);
        }

        List<UsableItem> choosedCategory = new List<UsableItem>();

        foreach (var item in sortedItems)
        {
            if (item.ItemRarity == closestRarity)
            {
                choosedCategory.Add(item);
            }
        }
        
        _currentItem = choosedCategory[UnityEngine.Random.Range(0, choosedCategory.Count)];

        MakeVisual(_currentItem.ItemVisual);
    }

    private void MakeVisual(GameObject visual)
    {
        _currentVisual = Instantiate(visual, _itemHolder);
    }

    private void RemoveVisual()
    {
        Destroy(_currentVisual);
    }
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
        }

        return null;
    }
}
