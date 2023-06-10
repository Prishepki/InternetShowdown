using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoop : NetworkBehaviour
{
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

    private GameState _currentGameState;

    public CanvasGameStates CurrentUIState { get; private set; }
    public MusicGameStates CurrentMusicState { get; private set; }

    public int CurrentMusicOffset { get; private set; }

    private int _currentGamesPlayed;

    private int _timeCounter;
    private int _repeatSeconds;

    private Dictionary<string, int> _votes = new Dictionary<string, int>();
    private string _votedMap;

    private bool _isSceneLoaded;

    private bool _isSkipNeeded;

    public void OnSceneLoaded()
    {
        _isSceneLoaded = true;
    }

    public void OnSceneUnloaded()
    {
        _isSceneLoaded = false;
    }

    [ServerCallback]
    public static GameLoop Singleton()
    {
        return FindObjectOfType<GameLoop>(true);
    }

    [Server]
    public void AddMapVote(string mapName)
    {
        if (_votes.ContainsKey(mapName))
        {
            _votes[mapName]++;
        }
        else
        {
            _votes.Add(mapName, 1);
        }
    }

    private void Awake()
    {
        if (FindObjectsOfType<GameLoop>(true).Length > 1) // в случае если на сцене уже есть геймлуп он удалит себя нахуй чтоб не было приколов
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(transform);
        }
    }

    [ServerCallback]
    public void StartLoop()
    {
        StartCoroutine(nameof(Loop));
    }

    private void Update()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            _isSkipNeeded = true;
        }
#endif
    }

    private void CancelVoiting()
    {
        StopCoroutine(nameof(HandleMapVoting));

        SceneGameManager sceneGameManager = SceneGameManager.Singleton();

        sceneGameManager.RpcSetMapVoting(false, false);
        sceneGameManager.RpcPlayVotingSound(false);

        OnVotingEnd();
    }

    private IEnumerator HandleMapVoting()
    {
        SceneGameManager sceneGameManager = SceneGameManager.Singleton();

        sceneGameManager.RpcSetMapVoting(false, false);

        yield return new WaitForSeconds(_preVotingTime);

        sceneGameManager.RpcSetMapVoting(true, true);
        sceneGameManager.RpcPlayVotingSound(true);

        yield return new WaitForSeconds(_votingTime);

        sceneGameManager.RpcSetMapVoting(false, true);
        sceneGameManager.RpcPlayVotingSound(false);

        OnVotingEnd();
    }

    private void OnVotingEnd()
    {
        SceneGameManager sceneGameManager = SceneGameManager.Singleton();

        if (_votes.Count == 0)
        {
            Debug.LogWarning("Seems like nobody voted for map");

            sceneGameManager.RpcOnVotingEnd("Nobody voted :(");

            return;
        }

        List<KeyValuePair<string, int>> _votesList = _votes.ToList();
        _votesList.Sort((current, next) => current.Value > next.Value ? -1 : 1);

        _votedMap = _votesList.First().Key;

        string votingEndMessage = $"{Path.GetFileNameWithoutExtension(_votedMap).ToSentence()} won!";

        sceneGameManager.RpcOnVotingEnd(votingEndMessage);
    }

    private IEnumerator Loop() // ебанутый цикл я в ахуе
    {
        WaitUntil _waitForSceneLoaded = new WaitUntil(() => _isSceneLoaded);

        while (NetworkServer.active)
        {
            yield return _waitForSceneLoaded;

            CurrentMusicOffset = 0;

            // ПЕРЕРЫВ
            if (_currentGamesPlayed == _roundsToLagreBreak)
            {
                SetGameState(GameState.LargeBreak, CanvasGameStates.Lobby, MusicGameStates.Lobby, _largeBreakLength);
                _currentGamesPlayed = 0;
            }
            else
            {
                SetGameState(GameState.Break, CanvasGameStates.Lobby, MusicGameStates.Lobby, _breakLength);
                _timeCounter = _breakLength;
            }

            SceneGameManager.Singleton().RpcSwitchUI(CurrentUIState);

            StartCoroutine(nameof(HandleMapVoting));

            for (int i = 0; i < _repeatSeconds; i++)
            {
                if (_isSkipNeeded)
                {
                    _timeCounter = 0;
                    _isSkipNeeded = false;

                    CancelVoiting();

                    break;
                }

                CurrentMusicOffset++;

                bool playSound = _timeCounter <= 10;

                OnTimeCounterUpdate(_timeCounter, Color.white, playSound);
                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            LoadMatch();

            yield return _waitForSceneLoaded;

            CurrentMusicOffset = 0;

            SceneGameManager.Singleton().RpcSwitchUI(CurrentUIState);

            // ПОДГОТОВКА
            SetGameState(GameState.Prepare, CanvasGameStates.Lobby, MusicGameStates.Match, _prepareLength);

            for (int i = 0; i < _repeatSeconds; i++)
            {
                if (_isSkipNeeded)
                {
                    _timeCounter = 0;
                    _isSkipNeeded = false;

                    break;
                }

                CurrentMusicOffset++;

                if (_timeCounter == 3)
                {
                    SceneGameManager.Singleton().RpcPrepareText(3);
                }

                OnTimeCounterUpdate(_timeCounter, Color.gray, true);
                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            OnTimeCounterUpdate(_timeCounter, Color.gray, true);

            yield return new WaitForSeconds(1f);

            // МАТЧ
            StartMatch();
            SetGameState(GameState.Match, CanvasGameStates.Game, MusicGameStates.Match, _roundLength);

            SceneGameManager.Singleton().RpcFadeUI(CurrentUIState);

            for (int i = 0; i < _repeatSeconds; i++)
            {
                if (_isSkipNeeded)
                {
                    _timeCounter = 0;
                    _isSkipNeeded = false;

                    break;
                }

                CurrentMusicOffset++;

                // я мистер читабельность
                Color targetColor = _timeCounter <= _attentionTimeRed ? Color.red : _timeCounter <= _attentionTimeYellow ? Color.yellow : Color.white;
                bool playSound = _timeCounter <= 60;

                OnTimeCounterUpdate(_timeCounter, targetColor, playSound);
                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            OnTimeCounterUpdate(_timeCounter, Color.red, true);

            StopMatch();
            SetGameState(GameState.MatchEnded, CanvasGameStates.Game, MusicGameStates.Match);

            for (int i = 0; i < 5; i++)
            {
                CurrentMusicOffset++;

                yield return new WaitForSeconds(1f);
            }

            _currentGamesPlayed++;

            TimeToBreak();
        }
    }

    private void SetGameState(GameState state, CanvasGameStates uiState, MusicGameStates musicState, int time = 0)
    {
        _currentGameState = state;
        CurrentUIState = uiState;
        CurrentMusicState = musicState;

        _timeCounter = time;
        _repeatSeconds = _timeCounter;
    }

    private void LoadMatch()
    {
        _votes.Clear();

        if (string.IsNullOrEmpty(_votedMap))
        {
            Debug.LogWarning($"Voted Map is empty, loading random map instead");

            string randomMap = _maps[Random.Range(0, _maps.Count)];
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
        foreach (var projectile in allProjectiles)
        {
            NetworkServer.Destroy(projectile.gameObject);
        }

        SceneGameManager sceneGameManager = SceneGameManager.Singleton();

        sceneGameManager.RpcHideDeathScreen();
        sceneGameManager.RpcRemoveMutations();
        sceneGameManager.RpcAllowMovement(false);
    }

    private void TimeToBreak()
    {
        NetworkManager.singleton.ServerChangeScene("Lobby");
    }

    public void OnTimeCounterUpdate(int counter, Color color, bool playSound)
    {
        SceneGameManager sceneGameManager = SceneGameManager.Singleton();

        if (sceneGameManager == null) return;

        sceneGameManager.RpcOnTimeCounterUpdate(counter, color, playSound);
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
