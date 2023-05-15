using UnityEngine;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Collections;

[CreateAssetMenu()]
public class UsableItem : ScriptableObject
{
    [Header("Base Settings")]
    [Tooltip("От редкости предмета зависит шанс выпадания")] public Rarity ItemRarity = Rarity.Common;

    [Header("Use Settings")]
    [Tooltip("Надо ли зажимать чтоб использовать предмет")] public bool HoldToUse = false;
    [SerializeField, ShowIf(nameof(HoldToUse)), AllowNesting(), Min(0), Tooltip("На сколько секунд надо зажать, чтоб предмет использовался")] public float UseTime = 1;

    [Space(9)]

    [SerializeField, Tooltip("Игроку дадут эти мутации при использовании")] public List<InspectorMutation> Mutations = new List<InspectorMutation>();
}

public enum Rarity : byte
{
    Legendary,
    Epic,
    Unique,
    Rare,
    Quaint,
    Common
}

[Serializable]
public class InspectorMutation // этот класс нужен чтоб отоброжать параметры мутации в инспекторе
{
    [Tooltip("ПОКА ЧТО ДОСТУПЫ ТОЛЬКО МУТАЦИИ СКОРОСТИ И ПРЫГУЧЕСТИ (другие мутации возможно будут срать ошибкой в консоль)")] public MutationType Type = MutationType.Speed;
    [Tooltip("Что надо сделать со статой игрока? Сложить, или умножить?")] public ChangeType ChangeAs = ChangeType.Add;
    [Tooltip("На сколько или во сколько раз надо увеличить стату игрока?")] public float Amount = 10;
    [Tooltip("Сколько секунд бафф будет действовать")] public float Time = 5;
}

public enum MutationType
{
    Damage,
    Speed,
    Bounce,
    Luck
}

public enum ChangeType
{
    Add,
    Multiply
}
