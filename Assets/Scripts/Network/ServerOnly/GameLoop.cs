using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoop : NetworkBehaviour
{
    [SerializeField] private GameInfo _gameInfo;

    [Header("Break")]
    [SerializeField, Min(10), Tooltip("Долгота перерыва между раундами в секундах")] private int _breakLength = 60;

    [Space(9)]

    [SerializeField, Min(2), Tooltip("Каждые _roundsToLagreBreak раундов будет наступать большой перерыв")] private int _roundsToLagreBreak = 5;
    [SerializeField, Min(20), Tooltip("Долгота большого перерыва в секундах")] private int _largeBreakLength = 180;

    [Header("Match")]
    [SerializeField, Min(30), Tooltip("Долгота подготовки перед раундом в секундах")] private int _prepareLength = 10;
    [SerializeField, Min(30), Tooltip("Долгота раунда в секундах")] private int _roundLength = 340;

    [Space(9)]

    [SerializeField, Min(10), Tooltip("Время в секундах, когда счетчик времени станет желтым")] private int _attentionTimeYellow = 60;
    [SerializeField, Min(10), Tooltip("Время в секундах, когда счетчик времени станет красным")] private int _attentionTimeRed = 10;

    [Header("Voting")]
    [SerializeField, Scene] private List<string> _maps;
    [SerializeField, Min(5), Tooltip("Время перед голосованием в секундах")] private int _preVotingTime = 10;
    [SerializeField, Min(5), Tooltip("Долгота голосования в секундах")] private int _votingTime = 15;

    private int _currentGamesPlayed;

    private Dictionary<string, int> _votes = new Dictionary<string, int>();
    private string _votedMap;

    private bool _isSceneLoaded;
    private bool _isSkipNeeded;

    private int _timeCounter;
    private int _repeatSeconds;

    public Dictionary<string, (int score, int activity)> LeftedPlayers = new Dictionary<string, (int score, int activity)>();

    public void SetSceneLoaded(bool loaded) => _isSceneLoaded = loaded;

    [Server]
    public void AddMapVote(string mapName)
    {
        if (_votes.ContainsKey(mapName))
            _votes[mapName]++;
        else
            _votes.Add(mapName, 1);
    }

    private void Awake()
    {
        if (FindObjectsOfType<GameLoop>(true).Length > 1) // в случае если на сцене уже есть геймлуп он удалит себя нахуй чтоб не было приколов
            Destroy(gameObject);
        else
            DontDestroyOnLoad(transform);
    }

    [ServerCallback]
    public void StartLoop()
    {
        StartCoroutine(nameof(Loop));
    }

    [ServerCallback]
    private void Update()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Backspace)) _isSkipNeeded = true;
#endif
    }

    private IEnumerator HandleMapVoting()
    {
        SceneGameManager.Singleton.RpcSetMapVoting(false, false);

        yield return new WaitForSeconds(_preVotingTime);

        GameInfo.Singleton.IsVotingTime = true;

        SceneGameManager.Singleton.RpcSetMapVoting(true, true);
        SceneGameManager.Singleton.RpcPlayVotingSound(true);

        yield return new WaitForSeconds(_votingTime);

        SceneGameManager.Singleton.RpcSetMapVoting(false, true);
        SceneGameManager.Singleton.RpcPlayVotingSound(false);

        OnVotingEnd();
    }

    private void OnVotingEnd()
    {
        GameInfo.Singleton.IsVotingTime = false;

        if (_votes.Count == 0)
        {
            Debug.LogWarning("Seems like nobody voted for map");

            SceneGameManager.Singleton.RpcOnVotingEnd("Nobody voted :(");

            return;
        }

        List<KeyValuePair<string, int>> _votesList = _votes.ToList();
        _votesList.Sort((current, next) => current.Value > next.Value ? -1 : 1);

        _votedMap = _votesList.First().Key;

        string votingEndMessage = $"{Path.GetFileNameWithoutExtension(_votedMap).ToSentence()} won!";

        SceneGameManager.Singleton.RpcOnVotingEnd(votingEndMessage);
    }

    private void CancelVoiting()
    {
        StopCoroutine(nameof(HandleMapVoting));

        SceneGameManager.Singleton.RpcSetMapVoting(false, false);
        SceneGameManager.Singleton.RpcPlayVotingSound(false);

        OnVotingEnd();
    }

    private record ColorFrom
    {
        public Color Color;
        public int From;

        public ColorFrom(Color color, int from)
        {
            Color = color;
            From = from;
        }
    }

    private IEnumerator Timer(List<ColorFrom> colors = null, int soundFrom = -1, int prepareFrom = -1)
    {
        Color targetColor = Color.white;
        bool playSound = false;

        for (int i = 0; i < _repeatSeconds; i++)
        {
            if (_isSkipNeeded)
            {
                _timeCounter = 0;
                _isSkipNeeded = false;

                if (GameInfo.Singleton.IsVotingTime)
                    CancelVoiting();
                else
                    StopCoroutine(nameof(HandleMapVoting));

                break;
            }

            playSound = _timeCounter <= soundFrom;

            if (colors != null)
            {
                foreach (var color in colors)
                {
                    if (_timeCounter <= color.From)
                    {
                        targetColor = color.Color;
                        break;
                    }
                }
            }

            if (_timeCounter == prepareFrom) SceneGameManager.Singleton.RpcPrepareText(prepareFrom);

            OnTimeCounterUpdate(_timeCounter, targetColor, playSound);
            _timeCounter--;

            yield return new WaitForSecondsRealtime(1f);
            yield return new WaitForFixedUpdate();
        }

        OnTimeCounterUpdate(_timeCounter, targetColor, playSound);

        yield return new WaitForSecondsRealtime(1f);
    }

    private IEnumerator Loop() // ебанутый цикл я в ахуе
    {
        WaitUntil _waitForSceneLoaded = new WaitUntil(() => _isSceneLoaded);

        _currentGamesPlayed = 1;

        GameObject newGameInfo = Instantiate(_gameInfo.gameObject);
        NetworkServer.Spawn(newGameInfo);

        while (NetworkServer.active)
        {
            yield return _waitForSceneLoaded;

            GameInfo.Singleton.CurrentMusicIndex = MusicSystem.GetIndex(MusicGameState.Lobby);
            GameInfo.Singleton.StartMusicOffset();

            // ПЕРЕРЫВ
            if (_currentGamesPlayed % _roundsToLagreBreak == 0)
            {
                SetGameState(GameState.LargeBreak, CanvasGameState.Lobby, MusicGameState.Lobby, _largeBreakLength);
            }
            else
            {
                SetGameState(GameState.Break, CanvasGameState.Lobby, MusicGameState.Lobby, _breakLength);
                _timeCounter = _breakLength;
            }

            StartCoroutine(nameof(HandleMapVoting));

            yield return StartCoroutine(Timer(soundFrom: 10));

            SceneGameManager.Singleton.RpcTransition(TransitionMode.In);
            OnTimeCounterUpdate(null, Color.gray, false);
            yield return new WaitForSeconds(Transition.Singleton().FullDurationIn);

            GameInfo.Singleton.StopMusicOffset();

            LoadMatch();
            yield return _waitForSceneLoaded;

            GameInfo.Singleton.CurrentMusicIndex = MusicSystem.GetIndex(MusicGameState.Match);
            GameInfo.Singleton.StartMusicOffset();

            // ПОДГОТОВКА
            SetGameState(GameState.Prepare, CanvasGameState.Lobby, MusicGameState.Match, _prepareLength);

            yield return new WaitForSeconds(0.75f); // хуета чтоб всинковать сонг

            yield return StartCoroutine(Timer(new List<ColorFrom>() { new ColorFrom(Color.gray, _repeatSeconds) }, _repeatSeconds, 3));

            // МАТЧ
            StartMatch();
            SetGameState(GameState.Match, CanvasGameState.Game, MusicGameState.Match, _roundLength);

            SceneGameManager.Singleton.RpcOnMatchStarted();

            List<ColorFrom> colorFroms = new List<ColorFrom>()
            {
                new ColorFrom(ColorISH.Red, _attentionTimeRed),
                new ColorFrom(ColorISH.Yellow, _attentionTimeYellow)
            };

            yield return StartCoroutine(Timer(colorFroms, 60));

            StopMatch();
            SetGameState(GameState.MatchEnded, CanvasGameState.Game, MusicGameState.Match);
            yield return new WaitForSeconds(5f);

            _currentGamesPlayed++;
            GameInfo.Singleton.StopMusicOffset();

            SceneGameManager.Singleton.RpcTransition(TransitionMode.In);
            yield return new WaitForSeconds(Transition.Singleton().FullDurationIn);

            TimeToBreak();
        }
    }

    private void SetGameState(GameState state, CanvasGameState uiState, MusicGameState musicState, int time = 0)
    {
        GameInfo.Singleton.CurrentGameState = state;
        GameInfo.Singleton.CurrentCanvasGameState = uiState;
        GameInfo.Singleton.CurrentMusicGameState = musicState;

        _timeCounter = time;
        _repeatSeconds = _timeCounter;
    }

    private void LoadMatch()
    {
        _votes.Clear();

        if (string.IsNullOrEmpty(_votedMap))
        {
            Debug.LogWarning($"Voted Map is empty, loading random map instead");

            string randomMap = _maps[UnityEngine.Random.Range(0, _maps.Count)];
            NetworkManager.singleton.ServerChangeScene(randomMap);

            return;
        }

        NetworkManager.singleton.ServerChangeScene(_votedMap);
    }

    private void StartMatch()
    {
        FindObjectOfType<ItemSpawner>().StartSpawnProcces();
    }

    private void StopMatch()
    {
        ItemSpawner itemSpawner = FindObjectOfType<ItemSpawner>();

        itemSpawner.StopSpawnProcces();
        itemSpawner.DestroyAll();

        ProjectileBase[] allProjectiles = FindObjectsOfType<ProjectileBase>();
        foreach (var projectile in allProjectiles) { NetworkServer.Destroy(projectile.gameObject); }

        SceneGameManager.Singleton.RpcHideDeathScreen();
        SceneGameManager.Singleton.RpcRemoveMutations();
        SceneGameManager.Singleton.RpcAllowMovement(false);
        SceneGameManager.Singleton.RpcOnMatchEnd();

        LeftedPlayers.Clear();
    }

    private void TimeToBreak()
    {
        NetworkManager.singleton.ServerChangeScene("Lobby");
    }

    public void OnTimeCounterUpdate(int? counter, Color color, bool playSound)
    {
        if (SceneGameManager.Singleton == null) return;

        SceneGameManager.Singleton.RpcOnTimeCounterUpdate(counter, color, playSound);
    }

    public static GameLoop Singleton()
    {
        return FindObjectOfType<GameLoop>(true);
    }
}

public enum GameState
{
    Break,
    LargeBreak,
    Prepare,
    Match,
    MatchEnded
}
