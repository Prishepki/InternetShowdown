using System;
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

    [ClientRpc] // методы с этим атрибутом будут вызываться на всех клиентах (работает только тогда если вызывается с класса который наследует NetworkBehaviour)
    public void RpcOnTimeCounterUpdate(int counter, Color color, bool playSound) // так надо было сделать ибо срало ошибками и нихуя не работало (мы с войдом решали это час)
    {
        EverywhereCanvas everywhereCanvas = EverywhereCanvas.Singleton();

        int minutes = TimeSpan.FromSeconds(counter).Minutes;
        int seconds = TimeSpan.FromSeconds(counter).Seconds;

        string exposedSeconds = seconds < 10 ? "0" + seconds.ToString() : seconds.ToString();

        string exposedTimeCounter = $"{minutes}:{exposedSeconds}";

        everywhereCanvas.Timer.text = exposedTimeCounter; // текст таймера
        everywhereCanvas.Timer.color = color; // цвет текста таймера

        everywhereCanvas.Timer.transform.localScale = Vector2.one * 1.075f;
        everywhereCanvas.Timer.transform.DOScale(Vector2.one, 0.5f).SetEase(Ease.OutElastic);

        if (!playSound) return;
        SoundSystem.PlaySound(new SoundTransporter(_clockTicks), new SoundPositioner(Vector3.zero), volume: 0.225f, enableFade: false);
    }

    [ClientRpc]
    public void RpcPrepareText(int fromCount)
    {
        EverywhereCanvas.Singleton().PreMatchText(fromCount);
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
        EverywhereCanvas.Singleton().SwitchUIGameState(state);
    }

    [ClientRpc]
    public void RpcFadeUI(CanvasGameStates state) // меняет интерфейс у игроков с плавной анимацией
    {
        EverywhereCanvas.Singleton().FadeUIGameState(state);
    }

    [ClientRpc]
    public void RpcHideDeathScreen() // на всякий случай, прячет экраны смерти у игроков
    {
        EverywhereCanvas.Singleton().HideDeathScreen();
    }

    [ClientRpc]
    public void RpcSetMapVoting(bool enable, bool fade)
    {
        EverywhereCanvas.Singleton().SetMapVoting(enable, fade);
    }

    [ClientRpc]
    public void RpcOnVotingEnd(string message)
    {
        EverywhereCanvas.Singleton().OnVotingEnd(message);
    }

    [ClientRpc]
    public void RpcPlayVotingSound(bool start)
    {
        AudioClip targetSound = start ? _votingStart : _votingEnd;

        SoundSystem.PlaySound(new SoundTransporter(targetSound), new SoundPositioner(Vector3.zero), volume: 0.4f, enableFade: false);
    }

    [Command(requiresAuthority = false)]
    public void CmdVoteMap(string mapName)
    {
        Debug.Log($"Someone voted for {mapName}");

        GameLoop.Singleton().AddMapVote(mapName);
    }

    [Command(requiresAuthority = false)]
    public void CmdAskForMapVoting()
    {
        RpcSetMapVoting(EverywhereCanvas.Singleton().IsVotingActive, false);
    }

    [Command(requiresAuthority = false)]
    public void RecieveUIGameState()
    {
        RpcSwitchUI(FindObjectOfType<GameLoop>().CurrentUIState);
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
