using System;
using System.Collections.Generic;
using System.Linq;

public static class RarityJobs
{
    public static readonly Dictionary<string, byte> Rarities = new()
    {
        { "Legendary", 5 },
        { "Epic", 20 },
        { "Unique", 36 },
        { "Rare", 62 },
        { "Quaint", 93 },
        { "Common", 255 },
    };

    public static int RarityToChance(Rarity input) // конвертирует энам редкости в шанс категории редкости
    {
        string key = RarityToName(input);

        return Rarities[key];
    }

    public static string RarityToName(Rarity value) => Rarity.GetName(typeof(Rarity), value); // конвертирует энам редкости в имя категории редкости
    public static Rarity RarityFromName(this string value) => (Rarity)Rarity.Parse(typeof(Rarity), value, true); // конвертирует имя категории редкости в энам редкости

    public static UsableItem[] ItemRaritySortHelper(UsableItem[] input, Rarity sortBy) // возвращает все UsableItem из input, у которых ItemRarity равно sortBy
    {
        List<UsableItem> toReturn = new List<UsableItem>();

        foreach (UsableItem item in input)
        {
            if (item.ItemRarity == sortBy)
            {
                toReturn.Add(item);
            }
        }

        return toReturn.ToArray();
    }

    public static UsableItem[] SortAllItems(UsableItem[] input) // сортирует каждый UsableItem из input по уровню редкости в порядке возрастания
    {
        List<UsableItem> toSort = input.ToList();

        toSort.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        return toSort.ToArray();
    }

    public static Rarity KeyValueRarityToRarity(KeyValuePair<string, byte> input) // конвертирует категорию редкости в энам редкости
    {
        return RarityFromName(input.Key);
    }
}
