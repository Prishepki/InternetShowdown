using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GroupsManager : MonoBehaviour
{
    public void DisableGroup(CanvasGroup target)
    {
        SetGroup(target, false);
    }

    public void EnableGroup(CanvasGroup target)
    {
        SetGroup(target, true);
    }

    public void DeactivateGroup(CanvasGroup target)
    {
        SetGroup(target, false, false);
    }

    public void ActivateGroup(CanvasGroup target)
    {
        SetGroup(target, true, false);
    }

    public void SetGroup(CanvasGroup target, bool value, bool changeVisibility = true)
    {
        if (changeVisibility)
        {
            target.alpha = value ? 1 : 0;
        }

        target.interactable = value;
        target.blocksRaycasts = value;
    }

    public static GroupsManager Singleton()
    {
        return FindObjectOfType<GroupsManager>(true);
    }
}
