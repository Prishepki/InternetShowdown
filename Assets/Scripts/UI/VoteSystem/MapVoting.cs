using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapVoting : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float ANIM_SPEED = 0.25f;

    private const float SCALE_NH = 1f;
    private const float SCALE_H = 1.075f;

    [Scene] public string ConnectedMap;

    [Space(9)]

    public Image MapScreenshot;
    public TMP_Text MapName;
    public Button VoteButton;

    [Space(9)]

    [SerializeField] private AudioClip _onVoteSound;
    [SerializeField] private AudioClip _onHoverSound;

    private TweenerCore<Vector3, Vector3, VectorOptions> _scaleTween;
    private TweenerCore<Color, Color, ColorOptions> _colorTween;

    private bool _animationInteractable;

    private void OnValidate()
    {
        if (TryGetComponent<Button>(out VoteButton))
        {
            VoteButton.transition = Selectable.Transition.None;
        }
    }

    private void Start()
    {
        VoteButton.onClick.AddListener(HideMapVoting);

        ResetAnimations();
    }

    public void SetActive(bool enable)
    {
        VoteButton.interactable = enable;
        _animationInteractable = enable;

        ResetAnimations();
    }

    public void HideMapVoting()
    {
        EverywhereCanvas.Singleton.SetMapVoting(false, true);
        SceneGameManager.Singleton.CmdVoteMap(ConnectedMap);

        SoundSystem.PlayInterfaceSound(new SoundTransporter(_onVoteSound), volume: 0.6f);
    }

    public void ResetAnimations()
    {
        CompleteAllTweens();

        transform.localScale = Vector3.one * SCALE_NH;
        VoteButton.targetGraphic.color = Color.gray;
    }

    private void CompleteAllTweens()
    {
        _scaleTween.Complete();
        _colorTween.Complete();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_animationInteractable) return;

        CompleteAllTweens();

        _scaleTween = transform.DOScale(Vector3.one * SCALE_H, ANIM_SPEED).SetEase(Ease.OutBack);
        _colorTween = VoteButton.targetGraphic.DOColor(Color.white, 0.3f).SetEase(Ease.OutCubic);

        SoundSystem.PlayInterfaceSound(new SoundTransporter(_onHoverSound), volume: 0.6f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_animationInteractable) return;

        CompleteAllTweens();

        _scaleTween = transform.DOScale(Vector3.one * SCALE_NH, ANIM_SPEED).SetEase(Ease.OutBack);
        _colorTween = VoteButton.targetGraphic.DOColor(Color.gray, 0.3f).SetEase(Ease.OutCubic);
    }
}
