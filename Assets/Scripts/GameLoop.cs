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

    private int _currentGamesPlayed;

    private int _timeCounter;
    private int _repeatSeconds;

    private Dictionary<string, int> _votes = new Dictionary<string, int>();
    private string _votedMap;

    private bool _isSceneLoaded;

    public void OnSceneLoaded()
    {
        _isSceneLoaded = true;
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

    private IEnumerator HandleMapVoting()
    {
        SceneGameManager sceneGameManager = SceneGameManager.Singleton();

        sceneGameManager.RpcSetMapVoting(false);

        yield return new WaitForSeconds(_preVotingTime);

        sceneGameManager.RpcSetMapVoting(true);
        sceneGameManager.RpcPlayVotingSound(true);

        yield return new WaitForSeconds(_votingTime);

        sceneGameManager.RpcSetMapVoting(false);
        sceneGameManager.RpcPlayVotingSound(false);

        if (_votes.Count == 0)
        {
            Debug.LogWarning("Seems like nobody voted for map");

            yield break;
        }

        List<KeyValuePair<string, int>> _votesList = _votes.ToList();
        _votesList.Sort((current, next) => current.Value > next.Value ? -1 : 1);

        _votedMap = _votesList.First().Key;

        sceneGameManager.RpcOnVotingEnd(Path.GetFileNameWithoutExtension(_votedMap));
    }

    private IEnumerator Loop() // ебанутый цикл я в ахуе
    {
        WaitUntil _waitForSceneLoaded = new WaitUntil(() => _isSceneLoaded);

        while (NetworkServer.active)
        {
            yield return _waitForSceneLoaded;

            // ПЕРЕРЫВ
            if (_currentGamesPlayed == _roundsToLagreBreak)
            {
                SetGameState(GameState.LargeBreak, CanvasGameStates.Lobby, _largeBreakLength);
                _currentGamesPlayed = 0;
            }
            else
            {
                SetGameState(GameState.Break, CanvasGameStates.Lobby, _breakLength);
                _timeCounter = _breakLength;
            }

            SceneGameManager.Singleton().RpcSwitchUI(CurrentUIState);

            StartCoroutine(nameof(HandleMapVoting));

            for (int i = 0; i < _repeatSeconds; i++)
            {
                OnTimeCounterUpdate(_timeCounter, Color.white);

                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            LoadMatch();

            yield return _waitForSceneLoaded;

            SceneGameManager.Singleton().RpcSwitchUI(CurrentUIState);

            // ПОДГОТОВКА
            SetGameState(GameState.Prepare, CanvasGameStates.Lobby, _prepareLength);

            for (int i = 0; i < _repeatSeconds; i++)
            {
                OnTimeCounterUpdate(_timeCounter, Color.gray);

                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            OnTimeCounterUpdate(_timeCounter, Color.gray);

            yield return new WaitForSeconds(1f);

            // МАТЧ
            OnTimeCounterUpdate(_roundLength, Color.white);

            StartMatch();
            SetGameState(GameState.Match, CanvasGameStates.Game, _roundLength);

            SceneGameManager.Singleton().RpcFadeUI(CurrentUIState);

            for (int i = 0; i < _repeatSeconds; i++)
            {
                // я мистер читабельность
                Color targetColor = _timeCounter <= _attentionTimeRed ? Color.red : _timeCounter <= _attentionTimeYellow ? Color.yellow : Color.white;

                OnTimeCounterUpdate(_timeCounter, targetColor);

                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            OnTimeCounterUpdate(_timeCounter, Color.red);

            StopMatch();
            SetGameState(GameState.MatchEnded, CanvasGameStates.Game);

            yield return new WaitForSeconds(5f);

            _currentGamesPlayed++;

            TimeToBreak();
        }
    }

    private void SetGameState(GameState state, CanvasGameStates uiState, int time = 0)
    {
        _currentGameState = state;
        CurrentUIState = uiState;

        _timeCounter = time;
        _repeatSeconds = _timeCounter;
    }

    private void LoadMatch()
    {
        _isSceneLoaded = false;

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
        _isSceneLoaded = false;
        NetworkManager.singleton.ServerChangeScene("Lobby");
    }

    public void OnTimeCounterUpdate(int counter, Color color)
    {
        SceneGameManager sceneGameManager = SceneGameManager.Singleton();

        if (sceneGameManager == null) return;

        sceneGameManager.RpcOnTimeCounterUpdate(counter, color);
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
