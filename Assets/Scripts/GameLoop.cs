using UnityEngine;
using Mirror;
using System.Collections;
using System;

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
    [SerializeField, Min(10), Tooltip("Время в секундах, когда счетчик времени станет красным")] private int _attentionTime = 60;

    private GameState _currentGameState;

    private int _currentGamesPlayed;

    private int _timeCounter;
    private int _repeatSeconds;

    private void Awake()
    {
        if (FindObjectsOfType<GameLoop>().Length > 1) // в случае если на сцене уже есть геймлуп он удалит себя нахуй чтоб не было приколов
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
        while (true)
        {
            // ПЕРЕРЫВ
            if (_currentGamesPlayed == _roundsToLagreBreak)
            {
                _currentGameState = GameState.LargeBreak;

                _timeCounter = _largeBreakLength;
                OnTimeCounterUpdate(_largeBreakLength, Color.white);
                
                _currentGamesPlayed = 0;
            }
            else
            {
                _currentGameState = GameState.Break;

                _timeCounter = _breakLength;
                OnTimeCounterUpdate(_breakLength, Color.white);
            }

            _repeatSeconds = _timeCounter;

            for (int i = 0; i < _repeatSeconds; i++)
            {
                OnTimeCounterUpdate(_timeCounter, Color.white);

                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            OnTimeCounterUpdate(_timeCounter, Color.white);

            StartMatch();

            yield return new WaitForSeconds(1f);

            // ПОДГОТОВКА
            _currentGameState = GameState.Prepare;

            _timeCounter = _prepareLength;
            OnTimeCounterUpdate(_prepareLength, Color.gray);

            _repeatSeconds = _timeCounter;

            for (int i = 0; i < _repeatSeconds; i++)
            {
                OnTimeCounterUpdate(_timeCounter, Color.gray);

                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            OnTimeCounterUpdate(_timeCounter, Color.gray);

            yield return new WaitForSeconds(1f);

            // МАТЧ
            _currentGameState = GameState.Match;

            _timeCounter = _roundLength;
            OnTimeCounterUpdate(_roundLength, Color.white);

            _repeatSeconds = _timeCounter;

            for (int i = 0; i < _repeatSeconds; i++)
            {
                Color targetColor = _timeCounter <= _attentionTime ? Color.red : Color.white;

                OnTimeCounterUpdate(_timeCounter, targetColor);

                _timeCounter--;

                yield return new WaitForSeconds(1f);
            }

            OnTimeCounterUpdate(_timeCounter, Color.red);

            yield return new WaitForSeconds(5f);

            _currentGamesPlayed++;

            TimeToBreak();
        }
    }

    private void StartMatch()
    {
        NetworkManager.singleton.ServerChangeScene("TestMap");
    }

    private void TimeToBreak()
    {
        NetworkManager.singleton.ServerChangeScene("Lobby");
    }

    public void OnTimeCounterUpdate(int counter, Color color)
    {
        FindObjectOfType<SceneGameManager>().RpcOnTimeCounterUpdate(counter, color);
    }
}

public enum GameState
{
    Break,
    LargeBreak,
    Prepare,
    Match
}
