using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        Cursor.lockState = CursorLockMode.None;
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Invoke(nameof(StartGameLoop), 1f); // ибал в рот
    }

    private void StartGameLoop()
    {
        FindObjectOfType<GameLoop>().StartLoop();
    }
}
