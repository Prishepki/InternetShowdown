using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DG.Tweening;
using Mirror;
using UnityEngine;

public class SceneGameManager : NetworkBehaviour
{
    [SerializeField] private List<AudioClip> _clockTicks = new List<AudioClip>();

    [Space(9)]

    [SerializeField] private AudioClip _votingStart;
    [SerializeField] private AudioClip _votingEnd;

    private EverywhereCanvas _everywhereCanvas;
    private PauseMenu _pauseMenu;

    private void Awake()
    {
        _everywhereCanvas = EverywhereCanvas.Singleton();
        _pauseMenu = EverywhereCanvas.PauseMenu();
    }

    [ClientRpc] // методы с этим атрибутом будут вызываться на всех клиентах (работает только тогда если вызывается с класса который наследует NetworkBehaviour)
    public void RpcOnTimeCounterUpdate(int counter, Color color, bool playSound) // так надо было сделать ибо срало ошибками и нихуя не работало (мы с войдом решали это час)
    {
        int minutes = TimeSpan.FromSeconds(counter).Minutes;
        int seconds = TimeSpan.FromSeconds(counter).Seconds;

        string exposedSeconds = seconds < 10 ? "0" + seconds.ToString() : seconds.ToString();

        string exposedTimeCounter = $"{minutes}:{exposedSeconds}";

        _everywhereCanvas.Timer.text = exposedTimeCounter; // текст таймера
        _everywhereCanvas.Timer.color = color; // цвет текста таймера

        _everywhereCanvas.Timer.transform.localScale = Vector2.one * 1.075f;
        _everywhereCanvas.Timer.transform.DOScale(Vector2.one, 0.5f).SetEase(Ease.OutElastic);

        if (!playSound) return;
        SoundSystem.PlayInterfaceSound(new SoundTransporter(_clockTicks), volume: 0.225f);
    }

    [ClientRpc]
    public void RpcPrepareText(int fromCount)
    {
        _everywhereCanvas.PreMatchText(fromCount);
    }

    [ClientRpc]
    public void RpcAllowMovement(bool allow) // запрещает или разрешает всем игрокам двигаться
    {
        NetworkClient.localPlayer.GetComponent<NetworkPlayer>().AllowMovement = allow;
    }

    [ClientRpc]
    public void RpcRemoveMutations() // убирает все мутации с каждого игрока
    {
        NetworkClient.localPlayer.GetComponent<ItemsReader>().RemoveAllMutations();
    }

    [ClientRpc]
    public void RpcShakeAll(float duration, float time) // трясет экраны у игроков
    {
        NetworkClient.localPlayer.GetComponent<NetworkPlayer>().PlayerMoveCamera.Shake(duration, time);
    }

    [ClientRpc]
    public void RpcSwitchUI(CanvasGameStates state) // меняет интерфейс у игроков
    {
        _everywhereCanvas.SwitchUIGameState(state);
    }

    [ClientRpc]
    public void RpcFadeUI(CanvasGameStates state) // меняет интерфейс у игроков с плавной анимацией
    {
        _everywhereCanvas.FadeUIGameState(state);
    }

    [ClientRpc]
    public void RpcTransition(TransitionMode mode) // вызывает эффект перехода на всех клиентах
    {
        Transition.Singleton().AwakeTransition(mode);
    }

    [ClientRpc]
    public void RpcHideDeathScreen() // на всякий случай, прячет экраны смерти у игроков
    {
        _everywhereCanvas.HideDeathScreen();
    }

    [ClientRpc]
    public void RpcSetMapVoting(bool enable, bool fade)
    {
        _everywhereCanvas.SetMapVoting(enable, fade);
    }

    [TargetRpc]
    public void TRpcSetMapVoting(NetworkConnectionToClient target, bool enable, bool fade)
    {
        _everywhereCanvas.SetMapVoting(enable, fade);
    }

    [ClientRpc]
    public void RpcOnVotingEnd(string message)
    {
        _everywhereCanvas.OnVotingEnd(message);
    }

    [ClientRpc]
    public void RpcPlayVotingSound(bool start)
    {
        AudioClip targetSound = start ? _votingStart : _votingEnd;

        SoundSystem.PlayInterfaceSound(new SoundTransporter(targetSound), volume: 0.4f);
    }

    [ClientRpc]
    public void RpcTriggerResultsWindow()
    {
        EverywhereCanvas.Results().SetWindowDynamic(true);
    }

    [Command(requiresAuthority = false)]
    public void CmdVoteMap(string mapName)
    {
        Debug.Log($"Someone voted for {mapName}");

        GameLoop.Singleton().AddMapVote(mapName);
    }

    [Command(requiresAuthority = false)]
    public void CmdAskForMapVoting(NetworkIdentity asker)
    {
        TRpcSetMapVoting(asker.connectionToClient, GameLoop.Singleton().IsVotingTime, false);
    }

    [ClientRpc]
    public void RpcForceClientsForLeaderboardUpdate()
    {
        EverywhereCanvas.Leaderboard().UpdateLeaderboard();
    }

    [TargetRpc]
    public void TRpcStartMusic(NetworkConnectionToClient target, MusicGameStates state, float offset)
    {
        MusicSystem.StartMusic(state, offset);
    }

    [Command(requiresAuthority = false)]
    public void CmdAskForMusic(NetworkIdentity asker)
    {
        GameLoop gameLoop = GameLoop.Singleton();

        TRpcStartMusic(asker.connectionToClient, gameLoop.CurrentMusicState, gameLoop.CurrentMusicOffset);
    }

    [Command(requiresAuthority = false)]
    public void RecieveUIGameState()
    {
        RpcSwitchUI(GameLoop.Singleton().CurrentUIState);
    }

    [ClientRpc]
    public void RpcOnMatchEnd()
    {
        NetworkPlayer player = NetworkClient.localPlayer.GetComponent<NetworkPlayer>();

        ResultsStatsJobs.StatsToDisplay["Place"] = (player.Place.ToString(), Color.white);
        ResultsStatsJobs.StatsToDisplay["Score"] = (player.Score.ToString(), Color.white);
        ResultsStatsJobs.StatsToDisplay["Activity"] = (player.Activity.ToString(), Color.white);
        ResultsStatsJobs.StatsToDisplay["Kills"] = (player.Kills.ToString(), Color.white);
        ResultsStatsJobs.StatsToDisplay["Hits"] = (player.Hits.ToString(), Color.white);
        ResultsStatsJobs.StatsToDisplay["Deaths"] = (player.Deaths.ToString(), Color.white);
        ResultsStatsJobs.StatsToDisplay["Traumas"] = (player.Traumas.ToString(), Color.white);

        int total = Mathf.Clamp((player.Score * 3) + (player.Activity) + (player.Kills * 6) + (player.Hits * 3) - (player.Deaths * 2) - (player.Traumas) - (player.Place * 5), 0, 1000);

        for (int i = ResultsStatsJobs.Rankings.Count - 1; i >= 0; i--)
        {
            if (total >= ResultsStatsJobs.Rankings[i].value)
            {
                ResultsStatsJobs.StatsToDisplay["Rank"] = (ResultsStatsJobs.Rankings[i].key, ResultsStatsJobs.Rankings[i].color);
                break;
            }
        }
    }

    public static SceneGameManager Singleton()
    {
        return FindObjectOfType<SceneGameManager>();
    }
}

public static class Extensions
{
    public static string ToSentence(this string Input)
    {
        return new string(Input.SelectMany((c, i) => i > 0 && char.IsUpper(c) ? new[] { ' ', c } : new[] { c }).ToArray());
    }
}
