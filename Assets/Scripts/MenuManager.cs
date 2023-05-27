using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private const string _nicknameSavePath = "PlayerNicknameValue";
    private const string _addressSavePath = "MenuAddressValue";

    [SerializeField] private TMP_InputField _nickname;
    [SerializeField] private TMP_InputField _address;

    private Button[] _menuButtons;

    private void Start()
    {
        SetNickname(PlayerPrefs.GetString(_nicknameSavePath));
        SetIP(PlayerPrefs.GetString(_addressSavePath));

        _menuButtons = GetComponentsInChildren<Button>(true);

        foreach (var button in _menuButtons)
        {
            button.onClick.AddListener(ClearSelections);
        }
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

    public void ExitGame()
    {
        Debug.Log("Exiting");
        Application.Quit();
    }

    private void ClearSelections()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
}
