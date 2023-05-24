using Mirror;
using TMPro;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    private const string _nicknameSavePath = "PlayerNicknameValue";
    private const string _addressSavePath = "MenuAddressValue";

    [SerializeField] private TMP_InputField _nickname;
    [SerializeField] private TMP_InputField _address;

    private void Start() 
    {
        SetNickname(PlayerPrefs.GetString(_nicknameSavePath));
        SetIP(PlayerPrefs.GetString(_addressSavePath));
    }

    public void SetNickname(string value)
    {
        _nickname.text = value;

        PlayerPrefs.SetString(_nicknameSavePath, value);
    }

    public void SetIP(string value)
    {
        _address.text = value;

        PlayerPrefs.SetString(_addressSavePath, value);
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
