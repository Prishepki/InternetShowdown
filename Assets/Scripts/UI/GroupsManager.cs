using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

using UnityEngine;

public class GroupsManager : MonoBehaviour
{
    public Dictionary<CanvasGroup, GroupTweens> WindowTweens { get; private set; } = new Dictionary<CanvasGroup, GroupTweens>();

    public List<CanvasGroup> EnabledGroups { get; private set; } = new List<CanvasGroup>();

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

    public void SetGroup(CanvasGroup target, bool value, bool changeVisibility = true, bool checkInList = true)
    {
        if (changeVisibility) target.alpha = value ? 1 : 0;

        target.interactable = value;
        target.blocksRaycasts = value;

        if (checkInList)
        {
            if (value)
            {
                if (!EnabledGroups.Contains(target))
                {
                    EnabledGroups.Add(target);
                }
            }
            else
            {
                if (EnabledGroups.Contains(target))
                {
                    EnabledGroups.Remove(target);
                }
            }
        }
    }

    public void ShowGroup(CanvasGroup target)
    {
        AnimateGroup(target, true);
        ActivateGroup(target);
    }

    public void HideGroup(CanvasGroup target)
    {
        AnimateGroup(target, false);
        DeactivateGroup(target);
    }

    public void SecretlyShowGroup(CanvasGroup target)
    {
        AnimateGroup(target, true);
        SetGroup(target, true, false, false);
    }

    public void SecretlyHideGroup(CanvasGroup target)
    {
        AnimateGroup(target, false);
        SetGroup(target, false, false, false);
    }

    public void AnimateGroup(CanvasGroup target, bool show)
    {
        if (show) target.transform.localScale = Vector2.one * 1.15f;

        if (WindowTweens.ContainsKey(target))
        {
            WindowTweens[target].AlphaTween?.Kill(true);
            WindowTweens[target].ScaleTween?.Kill(true);
        }

        float endAlpha = show ? 1 : 0;
        float endScale = show ? 1 : 1.15f;
        Ease scaleTween = show ? Ease.OutBack : Ease.InBack;

        TweenerCore<float, float, FloatOptions> newAlphaTween = target.DOFade(endAlpha, 0.15f).SetEase(Ease.OutSine);
        TweenerCore<Vector3, Vector3, VectorOptions> newScaleTween = target.transform.DOScale(endScale, 0.65f).SetEase(Ease.OutCubic);

        if (!WindowTweens.ContainsKey(target))
        {
            WindowTweens.Add(target, new GroupTweens() { AlphaTween = newAlphaTween, ScaleTween = newScaleTween });
        }
    }
}

public class GroupTweens
{
    public TweenerCore<float, float, FloatOptions> AlphaTween;
    public TweenerCore<Vector3, Vector3, VectorOptions> ScaleTween;

    public void KillAll()
    {
        AlphaTween.Kill();
        ScaleTween.Kill();
    }
}
