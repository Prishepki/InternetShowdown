using Mirror;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public void SetNickname(string value)
    {
        PlayerPrefs.SetString("PlayerNicknameValue", value);
    }

    public void SetIP(string value)
    {
        NetworkManager.singleton.networkAddress = value;
    }

    public void Connect()
    {
        if (NetworkManager.singleton.networkAddress == "host_server")
        {
            NetworkManager.singleton.StartServer();
            return;
        }

        if (NetworkManager.singleton.networkAddress == "host")
        {
            NetworkManager.singleton.StartHost();
            return;
        }

        NetworkManager.singleton.StartClient();
    }
}
