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
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EverywhereCanvas : MonoBehaviour // юи которое будет видно ВЕЗДЕ
{
    [Header("Main")]
    [SerializeField] private GameObject _canvas;

    [Space(9)]

    [SerializeField] private CanvasGroup _inLobby;
    [SerializeField] private CanvasGroup _inGame;

    [Header("Other")]
    [SerializeField] private CanvasGroup _pauseMenu;

    public TMP_Text Timer;

    public TMP_Text OthersNickname;

    public Slider UseTimer;
    public Image UseTimerFill;

    public GameObject _playerDebugPanel;
    public TMP_Text[] _playerDebugStats;

    public CanvasGroup _killLog;

    [Header("Voting")]
    [SerializeField] private CanvasGroup _mapVoting;
    [SerializeField] private TMP_Text _votingEndText;

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

    [Header("Leaderboard")]
    [SerializeField] private Place _placePrefab;
    [SerializeField] private Transform _placeContainer;

    [Space(9)]

    [SerializeField] private Color _firstPlaceColor;
    [SerializeField] private Color _secondPlaceColor;
    [SerializeField] private Color _thirdPlaceColor;

    private List<(string nickname, int score)> _leaderboard = new List<(string nickname, int score)>();

    private NetworkPlayer _player;

    [HideInInspector] public bool PauseMenuOpened { get; private set; }
    [HideInInspector] public bool IsVotingActive { get; private set; }

    private bool _isExitingServer;

    private TweenerCore<float, float, FloatOptions> _lobbyGroupTween;
    private TweenerCore<float, float, FloatOptions> _gameGroupTween;

    private TweenerCore<float, float, FloatOptions> _killLogTween;

    private TweenerCore<float, float, FloatOptions> _pauseMenuTween;

    private TweenerCore<float, float, FloatOptions> _mapVotingTween;

    public void QuitAction()
    {
        Application.Quit();
    }

    public void OnVotingEnd(string message)
    {
        StopCoroutine(nameof(OnVotingEndCoroutine));
        StartCoroutine(nameof(OnVotingEndCoroutine), message);
    }

    private IEnumerator OnVotingEndCoroutine(string message)
    {
        _votingEndText.text = string.Empty;

        char[] messageChars = message.ToCharArray();

        foreach (var messageChar in messageChars)
        {
            _votingEndText.text += messageChar;

            yield return null;
        }

        _votingEndText.text = message;

        yield return new WaitForSeconds(2.5f);

        for (int i = 0; i < messageChars.Length; i++)
        {
            _votingEndText.text = _votingEndText.text.Remove(_votingEndText.text.Length - 1);

            yield return null;
        }

        _votingEndText.text = string.Empty;
    }

    public void SetMapVoting(bool enable, bool animation)
    {
        IsVotingActive = enable;
        _mapVoting.gameObject.SetActive(enable);

        if (animation)
        {
            _mapVotingTween.Kill(true);
        }

        if (enable)
        {
            if (animation)
            {
                _mapVoting.alpha = 0;
                _mapVotingTween = _mapVoting.DOFade(1, 0.6f).SetEase(Ease.OutCirc);
            }

            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void MenuAction()
    {
        Pause(false);

        if (_isExitingServer || !NetworkClient.isConnected)
        {
            return;
        }

        StartCoroutine(nameof(OnDisconnectPressed));

        NetworkManager.singleton.StopHost();
        _isExitingServer = true;
    }

    private IEnumerator OnDisconnectPressed()
    {
        yield return new WaitUntil(() => !NetworkManager.singleton.isNetworkActive);

        _isExitingServer = false;
    }

    private void Update()
    {
        _playerDebugPanel.SetActive(false);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        _playerDebugPanel.SetActive(true);
        DebugStats();
#endif

        UpdateHealthDisplay();

        HandlePauseMenu();
    }

    private void HandlePauseMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Pause(!PauseMenuOpened);
        }
    }

    public void SwitchNicknameVisibility(bool show, string target = "")
    {
        OthersNickname.text = target;
        OthersNickname.gameObject.SetActive(show);
    }

    public void Initialize(NetworkPlayer player)
    {
        _player = player;
        StartCoroutine(nameof(LeaderboardTickLoop));
    }

    private IEnumerator LeaderboardTickLoop()
    {
        while (NetworkClient.isConnected)
        {
            UpdateLeaderboard();

            yield return new WaitForSeconds(1f);
        }

        ClearLeaderboardUI();
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

    public void UpdateLeaderboard()
    {
        List<(string nickname, int score)> newLeaderboardValue = new List<(string nickname, int score)>();

        List<NetworkPlayer> allPlayers = FindObjectsOfType<NetworkPlayer>().ToList();

        foreach (NetworkPlayer connPlayer in allPlayers)
        {
            newLeaderboardValue.Add((connPlayer.Nickname, connPlayer.Score));
        }

        _leaderboard = newLeaderboardValue;

        _leaderboard.Sort((first, second) => first.score < second.score ? 1 : -1);

        UpdateLeaderboardUI();
    }

    private void UpdateLeaderboardUI()
    {
        ClearLeaderboardUI();

        int clampedLeaderboardSize = Mathf.Clamp(_leaderboard.Count, 0, 6);

        for (int place = 0; place < clampedLeaderboardSize; place++)
        {
            Place placeComp = Instantiate(_placePrefab.gameObject, _placeContainer).GetComponent<Place>();

            placeComp.Number.text = $"{place + 1})";

            switch (place + 1)
            {
                default:
                    placeComp.Number.color = Color.white;
                    break;

                case 1:
                    placeComp.Number.color = _firstPlaceColor;
                    break;

                case 2:
                    placeComp.Number.color = _secondPlaceColor;
                    break;

                case 3:
                    placeComp.Number.color = _thirdPlaceColor;
                    break;
            }

            placeComp.Nickname.text = _leaderboard[place].nickname;
            placeComp.Score.text = _leaderboard[place].score.ToString();
        }
    }

    private void ClearLeaderboardUI()
    {
        foreach (Transform place in _placeContainer)
        {
            Destroy(place.gameObject);
        }
    }

    private void UpdateHealthDisplay()
    {
        Health.value = Mathf.Lerp(Health.value, _targetHealth, Time.deltaTime * _healthBarAniamtionSpeed);
        HealthFill.color = Color.Lerp(_healthMinColor, _healthMaxColor, Health.value / Health.maxValue);
    }

    private void Start()
    {
        EnableUseTimer(false);

        _killLog.alpha = 0;
        _pauseMenu.alpha = 0;
        _kafifEasterEgg.alpha = 0;

        HideDeathScreen();
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

    public void Pause(bool enable)
    {
        if (!NetworkManager.singleton.isNetworkActive) return;

        _pauseMenuTween.Kill(true);

        PauseMenuOpened = enable;

        if (enable)
        {
            Cursor.lockState = CursorLockMode.None;

            _pauseMenuTween = _pauseMenu.DOFade(1, 0.3f).SetEase(Ease.InOutCubic);
        }
        else
        {
            _pauseMenuTween = _pauseMenu.DOFade(0, 0.3f).SetEase(Ease.InOutCubic);

            if (_mapVoting.gameObject.activeInHierarchy) return;

            Cursor.lockState = CursorLockMode.Locked;
        }
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

    private void ClearSelections()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    public static EverywhereCanvas Singleton()
    {
        return FindObjectOfType<EverywhereCanvas>(true);
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
