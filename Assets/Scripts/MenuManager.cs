using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private const string _nicknameSavePath = "PlayerNicknameValue";
    private const string _addressSavePath = "MenuAddressValue";

    [SerializeField] private TMP_InputField _nickname;
    [SerializeField] private TMP_InputField _address;

    [SerializeField] private UnityEvent _onStart;

    private Transition _transition;

    private void Start()
    {
        _transition = Transition.Singleton();

        SetNickname(PlayerPrefs.GetString(_nicknameSavePath));
        SetIP(PlayerPrefs.GetString(_addressSavePath));

        Button[] menuButtons = GetComponentsInChildren<Button>(true);

        foreach (var button in menuButtons)
        {
            button.onClick.AddListener(ClearSelections);
        }

        if (GameLoop.Singleton() != null)
        {
            Destroy(GameLoop.Singleton().gameObject);
        }

        _onStart.Invoke();
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
        _transition.AwakeTransition(TransitionMode.In, DoConnect);
    }

    private void DoConnect()
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
        _transition.AwakeTransition(TransitionMode.In, Application.Quit);

        Debug.Log("Exiting");
    }

    private void ClearSelections()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
}
