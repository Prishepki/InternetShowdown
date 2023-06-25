using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EverywhereCanvas : MonoBehaviour, IGameCanvas // юи которое будет видно ВЕЗДЕ
{
    [Header("Main")]
    [SerializeField] private GameObject _canvas;
    [SerializeField] private Transition _transition;

    [Space(9)]

    [SerializeField] private CanvasGroup _inLobby;
    [SerializeField] private CanvasGroup _inGame;

    [Header("Other")]
    public TMP_Text Timer;

    public TMP_Text OthersNickname;

    public Slider UseTimer;
    public Image UseTimerFill;

    public GameObject _playerDebugPanel;
    public TMP_Text[] _playerDebugStats;

    public CanvasGroup _killLog;

    [Header("Preparing")]
    [SerializeField] private TMP_Text _TTOText;

    [Space(9)]

    [SerializeField] private AudioClip _matchBegins;

    [Space(9)]

    [SerializeField] private AudioClip _preMatchOne;
    [SerializeField] private AudioClip _preMatchTwo;
    [SerializeField] private AudioClip _preMatchThree;

    [Header("Voting")]
    [SerializeField] private CanvasGroup _mapVoting;
    [SerializeField] private TMP_Text _votingEndText;

    [Space(9)]

    [SerializeField] private List<AudioClip> _keyboardTyping = new List<AudioClip>();

    [Header("Health Slider")]
    public Slider Health;
    public Image HealthFill;

    [Space(9)]

    [SerializeField] private float _healthBarAniamtionSpeed = 5;

    [SerializeField] private Color _healthMaxColor;
    [SerializeField] private Color _healthMinColor;

    private float _targetHealth;

    [Header("Death Screen")]
    [SerializeField] private GameObject _deathScreen;
    [SerializeField] private TMP_Text _respawnCountdown;
    [SerializeField] private TMP_Text _deathPhrase;
    [SerializeField] private List<string> _deathPhrases = new List<string>();

    [Space(9)]

    [SerializeField] private CanvasGroup _kafifEasterEgg;

    [HideInInspector] public bool IsVotingActive { get; private set; }

    private bool _isExitingServer;

    private TweenerCore<float, float, FloatOptions> _lobbyGroupTween;
    private TweenerCore<float, float, FloatOptions> _gameGroupTween;

    private TweenerCore<float, float, FloatOptions> _killLogTween;

    private TweenerCore<float, float, FloatOptions> _mapVotingColorTween;

    private TweenerCore<Vector3, Vector3, VectorOptions> _ttoTextTweenY;
    private TweenerCore<Vector3, Vector3, VectorOptions> _ttoTextTweenX;
    private TweenerCore<Color, Color, ColorOptions> _ttoTextColorTween;
    private TweenerCore<Vector3, Vector3, VectorOptions> _mapVotingScaleTween;

    private GroupsManager _groupsManager;
    private ResultsWindow _resultsWindow;
    private PauseMenu _pauseMenu;

    [SerializeField] private UnityEvent _onStart;

    private void Awake()
    {
        if (FindObjectsOfType<EverywhereCanvas>(true).Length > 1)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(transform);

        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (var button in buttons)
        {
            button.onClick.AddListener(ClearSelections);
        }
    }

    private void Start()
    {
        EnableUseTimer(false);

        _killLog.alpha = 0;
        _kafifEasterEgg.alpha = 0;
        _mapVoting.alpha = 0;

        _TTOText.color = ColorISH.Invisible;

        HideDeathScreen();

        _groupsManager = GroupsManager.Singleton();
        _resultsWindow = Results();
        _pauseMenu = PauseMenu();

        _onStart.Invoke();
    }

    private void Update()
    {
        _playerDebugPanel.SetActive(false);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        _playerDebugPanel.SetActive(true);
        DebugStats();
#endif

        UpdateHealthDisplay();
    }

    public void QuitAction()
    {
        _transition.AwakeTransition(TransitionMode.In, Application.Quit);
    }

    public void MenuAction()
    {
        if (_isExitingServer || !NetworkClient.isConnected)
        {
            return;
        }

        StartCoroutine(nameof(OnDisconnectPressed));

        _isExitingServer = true;

        _transition.AwakeTransition(TransitionMode.In, ExitMatch);
    }

    private void ExitMatch()
    {
        NetworkManager.singleton.StopHost();
    }

    public void OnDisconnect() { }

    private IEnumerator OnDisconnectPressed()
    {
        yield return new WaitUntil(() => !NetworkManager.singleton.isNetworkActive);

        _transition.AwakeTransition(TransitionMode.Out);

        _isExitingServer = false;
    }

    public void OnVotingEnd(string message)
    {
        StopCoroutine(nameof(OnVotingEndCoroutine));
        StartCoroutine(nameof(OnVotingEndCoroutine), message);
    }

    public void PreMatchText(int fromCount)
    {
        StartCoroutine(nameof(PreMatchCoroutine), fromCount);
    }

    private IEnumerator PreMatchCoroutine(int fromCount)
    {
        Color targetColor = Color.white;
        AudioClip targetSound = null;
        string targetText = string.Empty;

        void SetTextParams(Color color, AudioClip sound, string text)
        {
            targetColor = color;
            targetSound = sound;
            targetText = text;
        }

        for (int count = fromCount; count >= 0; count--)
        {
            switch (count)
            {
                case 0:
                    SetTextParams(ColorISH.Magenta, _matchBegins, "Let's Go!");
                    break;

                case 1:
                    SetTextParams(ColorISH.Red, _preMatchOne, count.ToString());
                    break;

                case 2:
                    SetTextParams(ColorISH.Yellow, _preMatchTwo, count.ToString());
                    break;

                case 3:
                    SetTextParams(ColorISH.Green, _preMatchThree, count.ToString());
                    break;

                default:
                    SetTextParams(Color.white, null, count.ToString());
                    break;

            }

            BounceTTOText(targetText, targetColor, targetSound);

            yield return new WaitForSeconds(1f);
        }
    }

    public void BounceTTOText(string text, Color color, AudioClip sound)
    {
        _TTOText.text = text;

        _ttoTextTweenY.Kill(true);
        _ttoTextTweenX.Kill(true);
        _ttoTextColorTween.Kill(true);

        _TTOText.transform.localScale = Vector2.one * 1.25f;
        _TTOText.color = color;

        _ttoTextTweenY = _TTOText.transform.DOScaleY(1f, 0.55f).SetEase(Ease.OutElastic);
        _ttoTextTweenX = _TTOText.transform.DOScaleX(1f, 0.75f).SetEase(Ease.OutBack);
        _ttoTextColorTween = _TTOText.DOColor(ColorISH.Invisible * color, 2f);

        if (sound == null) return;

        SoundSystem.PlayInterfaceSound(new SoundTransporter(sound), volume: 0.35f);
    }

    private IEnumerator OnVotingEndCoroutine(string message)
    {
        _votingEndText.text = string.Empty;

        char[] messageChars = message.ToCharArray();

        foreach (var messageChar in messageChars)
        {
            _votingEndText.text += messageChar;

            SoundSystem.PlayInterfaceSound(new SoundTransporter(_keyboardTyping), volume: 0.3f);

            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
        }

        _votingEndText.text = message;

        yield return new WaitForSeconds(2.5f);

        for (int ch = 0; ch < messageChars.Length; ch++)
        {
            _votingEndText.text = _votingEndText.text.Remove(_votingEndText.text.Length - 1);

            SoundSystem.PlayInterfaceSound(new SoundTransporter(_keyboardTyping), volume: 0.3f);

            for (int i = 0; i < 5; i++)
            {
                yield return null;
            }
        }

        _votingEndText.text = string.Empty;
    }

    public void SetMapVoting(bool enable, bool animation)
    {
        if (IsVotingActive == enable) return;

        IsVotingActive = enable;

        foreach (var votingVariant in _mapVoting.GetComponentsInChildren<MapVoting>())
        {
            votingVariant.SetActive(enable);
        }

        if (animation)
        {
            _mapVotingColorTween.Kill(true);
            _mapVotingScaleTween.Kill(true);
        }

        if (enable)
        {
            _resultsWindow.SetWindow(false);

            if (animation)
            {
                _mapVoting.transform.localScale = Vector3.one;
                _mapVoting.alpha = 0;

                _mapVotingColorTween = _mapVoting.DOFade(1, 0.6f).SetEase(Ease.OutCirc);
            }
            else
            {
                _mapVoting.transform.localScale = Vector3.one;
                _mapVoting.alpha = 1;
            }

            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            if (animation)
            {
                _mapVoting.alpha = 1;
                _mapVoting.transform.localScale = Vector3.one;

                _mapVotingScaleTween = _mapVoting.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
                _mapVotingColorTween = _mapVoting.DOFade(0, 0.5f).SetEase(Ease.OutCirc);
            }
            else
            {
                _mapVoting.transform.localScale = Vector3.one;
                _mapVoting.alpha = 0;
            }

            if (_pauseMenu.PauseMenuOpened || _resultsWindow.IsActive) return;

            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void SwitchNicknameVisibility(bool show, string target = "")
    {
        OthersNickname.text = target;
        OthersNickname.gameObject.SetActive(show);
    }

    public void EnableCanvasElements(bool enable)
    {
        _canvas.SetActive(enable);
    }

    public void FadeUIGameState(CanvasGameStates state)
    {
        (float lobbyEnd, float gameEnd) endValues = state == CanvasGameStates.Lobby ? (1, 0) : (0, 1);

        KillGameStateTweens();

        _lobbyGroupTween = _inLobby.DOFade(endValues.lobbyEnd, 0.6f);
        _gameGroupTween = _inGame.DOFade(endValues.gameEnd, 0.6f);
    }

    private void KillGameStateTweens()
    {
        _lobbyGroupTween.Kill(true);
        _gameGroupTween.Kill(true);
    }

    public void SwitchUIGameState(CanvasGameStates state)
    {
        if (state == CanvasGameStates.Lobby)
        {
            _inLobby.alpha = 1;
            _inGame.alpha = 0;
        }
        else if (state == CanvasGameStates.Game)
        {
            _inLobby.alpha = 0;
            _inGame.alpha = 1;
        }
    }

    private void UpdateHealthDisplay()
    {
        Health.value = Mathf.Lerp(Health.value, _targetHealth, Time.deltaTime * _healthBarAniamtionSpeed);
        HealthFill.color = Color.Lerp(_healthMinColor, _healthMaxColor, Health.value / Health.maxValue);
    }

    public void CancelUseTimer()
    {
        EnableUseTimer(false);

        StopCoroutine(nameof(LerpUseTimer));
    }

    public void StartUseTimer(float time)
    {
        EnableUseTimer(true);

        StartCoroutine(nameof(LerpUseTimer), time);
    }

    private void EnableUseTimer(bool enable)
    {
        UseTimer.gameObject.SetActive(enable);
    }

    public void SetMaxHealth(float value)
    {
        Health.maxValue = value;
    }

    public void SetDisplayHealth(float value)
    {
        _targetHealth = value;
    }

    public void LogKill()
    {
        StartCoroutine(LogKillCoroutine(2, 0.65f));
    }

    private IEnumerator LogKillCoroutine(int staticDuration, float fadeDuration)
    {
        _killLog.alpha = 1;

        yield return new WaitForSeconds(staticDuration);

        _killLogTween = _killLog.DOFade(0, fadeDuration);
    }

    public void StartDeathScreen(ref Action onRespawn)
    {
        StopCoroutine(nameof(RespawnScreenCoroutine));
        StartCoroutine(nameof(RespawnScreenCoroutine), onRespawn);
    }

    public void HideDeathScreen()
    {
        StopCoroutine(nameof(RespawnScreenCoroutine));
        _deathScreen.SetActive(false);
    }

    private IEnumerator RespawnScreenCoroutine(Action onRespawn)
    {
        int easterEgg = UnityEngine.Random.Range(1, 100);

        if (easterEgg == 1)
        {
            _kafifEasterEgg.alpha = 0.5f;
            _kafifEasterEgg.DOFade(0, 0.5f);
        }

        _deathScreen.SetActive(true);
        _deathPhrase.text = _deathPhrases[UnityEngine.Random.Range(0, _deathPhrases.Count)];

        for (int i = 5; i > 0; i--)
        {
            _respawnCountdown.text = $"Respawning in {i}";
            yield return new WaitForSeconds(1f);
        }

        if (onRespawn != null)
        {
            onRespawn.Invoke();
        }

        _deathScreen.SetActive(false);
    }

    private class FadeGroupParams
    {
        public bool FadeIn;
        public float FadeDuration;
        public CanvasGroup Target;

        public FadeGroupParams(bool fadeIn, float fadeDuration, CanvasGroup target)
        {
            FadeIn = fadeIn;
            FadeDuration = fadeDuration;
            Target = target;
        }
    }

    private IEnumerator LerpUseTimer(float duration)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            UseTimer.value = Mathf.Lerp(0, 1, elapsed / duration);
            UseTimerFill.color = new Color(1, 1 - UseTimer.value, 1 - UseTimer.value / 3, Mathf.Sin(1 - UseTimer.value));

            elapsed += Time.deltaTime;
            yield return null;
        }

        UseTimer.value = 1;

        EnableUseTimer(false);
    }

    private void ClearSelections()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    public static EverywhereCanvas Singleton()
    {
        return FindObjectOfType<EverywhereCanvas>(true);
    }

    public static ResultsWindow Results()
    {
        return FindObjectOfType<ResultsWindow>(true);
    }

    public static PauseMenu PauseMenu()
    {
        return FindObjectOfType<PauseMenu>(true);
    }

    public static Leaderboard Leaderboard()
    {
        return FindObjectOfType<Leaderboard>(true);
    }

    private void DebugStats()
    {
        for (int i = 0; i < _playerDebugStats.Length; i++)
        {
            switch (i)
            {
                case 0:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Speed}";
                    break;

                case 1:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Bounce}";
                    break;

                case 2:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Luck}";
                    break;

                case 3:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerMutationStats.Singleton.Damage}";
                    break;

                case 4:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Speed}";
                    break;

                case 5:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Bounce}";
                    break;

                case 6:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Luck}";
                    break;

                case 7:
                    _playerDebugStats[i].text = $"{_playerDebugStats[i].name} {PlayerCurrentStats.Singleton.Damage}";
                    break;
            }
        }
    }
}

public enum CanvasGameStates
{
    Lobby,
    Game
}

public static class ColorISH
{
    public static readonly Color32 Red = new Color32(255, 66, 78, 255);
    public static readonly Color32 Green = new Color32(78, 255, 163, 255);
    public static readonly Color32 Blue = new Color32(78, 97, 255, 255);

    public static readonly Color32 Cyan = new Color32(78, 242, 255, 255);
    public static readonly Color32 Magenta = new Color32(255, 57, 204, 255);
    public static readonly Color32 Yellow = new Color32(255, 217, 78, 255);

    public static readonly Color32 Gold = new Color32(255, 214, 48, 255);
    public static readonly Color32 Silver = new Color32(168, 169, 173, 255);
    public static readonly Color32 Bronze = new Color32(187, 123, 61, 255);

    public static readonly Color Invisible = new Color(1, 1, 1, 0);
}
