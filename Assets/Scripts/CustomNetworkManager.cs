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
}
