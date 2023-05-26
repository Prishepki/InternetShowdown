using System.Collections;
using Mirror;
using UnityEngine;

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

    private GameState _currentGameState;
    public CanvasGameStates CurrentUIState { get; private set; }

    private int _currentGamesPlayed;

    private int _timeCounter;
    private int _repeatSeconds;

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

    private IEnumerator Loop() // ебанутый цикл я в ахуе
    {
        WaitForSeconds _sceneChangeDelay = new WaitForSeconds(0.15f);

        while (NetworkServer.active)
        {
            yield return _sceneChangeDelay;

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

            for (int i = 0; i < _repeatSeconds; i++)
            {
                OnTimeCounterUpdate(_timeCounter, Color.white);

                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            LoadMatch();

            yield return _sceneChangeDelay;

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
        NetworkManager.singleton.ServerChangeScene("TestMap");
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

    public void OnTimeCounterUpdate(int counter, Color color)
    {
        SceneGameManager.Singleton().RpcOnTimeCounterUpdate(counter, color);
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
