using System.Collections.Generic;
using UnityEngine;

public static class RarityJobs
{
    public static readonly Dictionary<string, byte> Rarities = new()
    {
        { "Legendary", 7 },
        { "Epic", 25 },
        { "Unique", 65 },
        { "Rare", 105 },
        { "Quaint", 155 },
        { "Common", 255 },
    };

    public static Rarity RarityFromName(this string value) => (Rarity)Rarity.Parse(typeof(Rarity), value, true); // конвертирует имя категории редкости в энам редкости

    public static List<UsableItem> ItemRaritySortHelper(List<UsableItem> input, Rarity sortBy) // возвращает все UsableItem из input, у которых ItemRarity равно sortBy
    {
        List<UsableItem> toReturn = new List<UsableItem>();

        foreach (UsableItem item in input)
        {
            if (item.ItemRarity == sortBy)
            {
                toReturn.Add(item);
            }
        }

        return toReturn;
    }

    public static List<UsableItem> SortAllItems(List<UsableItem> input) // сортирует каждый UsableItem из input по уровню редкости в порядке возрастания
    {
        List<UsableItem> toReturn = input;

        toReturn.Sort((first, second) => (byte)first.ItemRarity < (byte)second.ItemRarity ? -1 : 1);

        return toReturn;
    }

    public static Rarity KeyValueRarityToRarity(KeyValuePair<string, byte> input) // конвертирует категорию редкости в энам редкости
    {
        return RarityFromName(input.Key);
    }

    public static byte ChoicRandomizer(byte modifier)
    {
        bool isPositive = modifier > 0;

        byte absoluteModifier = (byte)Mathf.Abs(modifier);

        if (isPositive)
        {
            return (byte)UnityEngine.Random.Range(0, 255 - absoluteModifier);
        }
        else
        {
            return (byte)UnityEngine.Random.Range(absoluteModifier, 255);
        }
    }
}
