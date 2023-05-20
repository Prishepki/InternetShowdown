using Mirror;
using System;
using UnityEngine;

public class SceneGameManager : NetworkBehaviour
{
    [ClientRpc] // методы с этим атрибутом будут вызываться на всех клиентах (работает только тогда если вызывается с класса который наследует NetworkBehaviour)
    public void RpcOnTimeCounterUpdate(int counter, Color color) // так надо было сделать ибо срало ошибками и нихуя не работало (мы с войдом решали это час)
    {
        EverywhereCanvas everywhereCanvas = EverywhereCanvas.Singleton();

        int minutes = TimeSpan.FromSeconds(counter).Minutes;
        int seconds = TimeSpan.FromSeconds(counter).Seconds;

        string exposedSeconds = seconds < 10 ? "0" + seconds.ToString() : seconds.ToString();

        string exposedTimeCounter = $"{minutes}:{exposedSeconds}";

        everywhereCanvas.Timer.text = exposedTimeCounter; // текст таймера
        everywhereCanvas.Timer.color = color; // цвет текста таймера
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
    public void RpcShakeAll(float duration, float time) // трясет экраны у всех игроков
    {
        NetworkClient.localPlayer.GetComponent<NetworkPlayer>().PlayerMoveCamera.Shake(duration, time);
    }

    public static SceneGameManager Singleton()
    {
        return FindObjectOfType<SceneGameManager>();
    }
}
