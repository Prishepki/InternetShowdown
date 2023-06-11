using System;
using System.Collections;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(MaskableGraphic))]
public class TweeningGraphic : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Components")]
    public MaskableGraphic ColorTarget;
    public Transform SizeTarget;

    [Header("Tweens")]
    [SerializeField] private GraphicTween _clickTween;
    [SerializeField] private GraphicTween _enterTween;
    [SerializeField] private GraphicTween _exitTween;

    [Header("Other")]
    [SerializeField] private bool _resizeParent = true;
    [SerializeField] private bool _selectable;

    private TweenerCore<Vector3, Vector3, VectorOptions> _sizeTween;
    private TweenerCore<Color, Color, ColorOptions> _colorTween;

    private bool _isTweenActive;

    private bool _isHighlighted;

    private bool _isSelected;

    private void OnValidate()
    {
        if (_clickTween != null && _enterTween != null && _exitTween != null)
        {
            _clickTween.OnValidate();
            _enterTween.OnValidate();
            _exitTween.OnValidate();
        }

        TryGetComponent<MaskableGraphic>(out ColorTarget);

        if (_resizeParent)
        {
            SizeTarget = transform.parent;
        }
        else
        {
            SizeTarget = transform;
        }
    }

    private void Start()
    {
        SetStats(_exitTween);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SetStats(_clickTween);

        if (_isHighlighted)
        {
            DoTween(_enterTween);
        }
        else
        {
            DoTween(_exitTween);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHighlighted = true;
        DoTween(_enterTween);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHighlighted = false;
        DoTween(_exitTween);
    }

    private void DoTween(GraphicTween tween)
    {
        if (_isSelected) return;

        StopCoroutine(nameof(TweenCoroutine));
        StartCoroutine(nameof(TweenCoroutine), tween);
    }

    private IEnumerator TweenCoroutine(GraphicTween tween)
    {
        yield return new WaitUntil(() => !_isTweenActive);

        MakeTween(tween);
    }

    private void MakeTween(GraphicTween tween)
    {
        _isTweenActive = true;

        if (_sizeTween != null && _colorTween != null)
        {
            _sizeTween.Complete();
            _colorTween.Complete();
        }

        _sizeTween = SizeTarget.DOScale(tween.TweenSize, tween.TweenDuration).SetEase(tween.TweenEase);
        _colorTween = ColorTarget.DOColor(tween.TweenColor, tween.TweenDuration).SetEase(tween.TweenEase);

        _sizeTween.onComplete = OnTweenCompleted;
        _colorTween.onComplete = OnTweenCompleted;
    }

    private void SetStats(GraphicTween stats)
    {
        if (_isSelected) return;

        ColorTarget.color = stats.TweenColor;
        SizeTarget.localScale = Vector3.one * stats.TweenSize;
    }

    private void OnTweenCompleted()
    {
        _isTweenActive = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!_selectable) return;

        DoTween(_enterTween);

        _isSelected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!_selectable) return;

        _isSelected = false;

        DoTween(_exitTween);
    }
}

[Serializable]
public class GraphicTween
{
    public GraphicTweenPresets TweenPreset;

    public void OnValidate()
    {
        void SetParams(Ease ease, float duration, Color color, float size)
        {
            TweenEase = ease;
            TweenDuration = duration;
            TweenColor = color;
            TweenSize = size;
        }

        switch (TweenPreset)
        {
            case GraphicTweenPresets.HighlightNeutral:
                SetParams(Ease.OutCirc, 0.15f, Color.white, 1.025f);
                break;

            case GraphicTweenPresets.HighlightDanger:
                SetParams(Ease.OutCirc, 0.15f, new Color32(255, 78, 90, 255), 1.025f);
                break;

            case GraphicTweenPresets.HighlightPositive:
                SetParams(Ease.OutCirc, 0.15f, new Color32(78, 255, 163, 255), 1.025f);
                break;

            case GraphicTweenPresets.NormalDark:
                SetParams(Ease.OutCirc, 0.15f, Color.gray, 1f);
                break;

            case GraphicTweenPresets.NormalBright:
                SetParams(Ease.OutCirc, 0.15f, new Color(0.75f, 0.75f, 0.75f), 1f);
                break;

            case GraphicTweenPresets.Pressed:
                SetParams(Ease.OutCirc, 0f, Color.white, 1.075f);
                break;
        }
    }

    public Ease TweenEase = Ease.OutCirc;

    [Space(9)]

    public float TweenDuration = 0.25f;
    public Color TweenColor = Color.white;
    public float TweenSize = 1f;
}

public enum GraphicTweenPresets
{
    Custom,
    Pressed,
    HighlightDanger,
    HighlightPositive,
    HighlightNeutral,
    NormalDark,
    NormalBright
}
