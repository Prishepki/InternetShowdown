using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

using UnityEngine;

public class GroupsManager : MonoBehaviour
{
    private class GroupTweens
    {
        public TweenerCore<float, float, FloatOptions> AlphaTween;
        public TweenerCore<Vector3, Vector3, VectorOptions> ScaleTween;
    }

    private Dictionary<CanvasGroup, GroupTweens> _windowTweens = new Dictionary<CanvasGroup, GroupTweens>();

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
        if (changeVisibility) target.alpha = value ? 1 : 0;

        target.interactable = value;
        target.blocksRaycasts = value;
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

    public void AnimateGroup(CanvasGroup target, bool show)
    {
        if (show) target.transform.localScale = Vector2.one * 1.15f;

        if (_windowTweens.ContainsKey(target))
        {
            _windowTweens[target].AlphaTween?.Kill(true);
            _windowTweens[target].ScaleTween?.Kill(true);
        }

        float endAlpha = show ? 1 : 0;
        float endScale = show ? 1 : 1.15f;
        Ease scaleTween = show ? Ease.OutBack : Ease.InBack;

        TweenerCore<float, float, FloatOptions> newAlphaTween = target.DOFade(endAlpha, 0.25f).SetEase(Ease.OutSine);
        TweenerCore<Vector3, Vector3, VectorOptions> newScaleTween = target.transform.DOScale(endScale, 0.75f).SetEase(Ease.OutCubic);

        if (!_windowTweens.ContainsKey(target))
        {
            _windowTweens.Add(target, new GroupTweens() { AlphaTween = newAlphaTween, ScaleTween = newScaleTween });
        }
    }

    public static GroupsManager Singleton()
    {
        return FindObjectOfType<GroupsManager>(true);
    }
}
