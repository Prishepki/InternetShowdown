using System.Collections;
using DG.Tweening;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    private const string NICKNAME_SAVE_PATH = "PlayerNicknameValue";
    private const string ADDRESS_SAVE_PATH = "MenuAddressValue";

    [SerializeField] private TMP_InputField _nickname;
    [SerializeField] private TMP_InputField _address;

    [SerializeField] private UnityEvent _onStart;

    private Transition _transition;

    [SerializeField] private RectTransform[] _menuButtons;

    private void Start()
    {
        _transition = Transition.Singleton();

        SetNickname(PlayerPrefs.GetString(NICKNAME_SAVE_PATH));
        SetIP(PlayerPrefs.GetString(ADDRESS_SAVE_PATH));

        Button[] menuButtons = GetComponentsInChildren<Button>(true);
        foreach (var button in menuButtons) { button.onClick.AddListener(ClearSelections); }

        if (GameLoop.Singleton() != null) Destroy(GameLoop.Singleton().gameObject);

        _onStart.Invoke();

        for (int i = 0; i < _menuButtons.Length; i++)
        {
            StartCoroutine(ShiftButton(_menuButtons[i], i));
        }
    }

    private IEnumerator ShiftButton(RectTransform button, int delayMult)
    {
        Vector3 shiftAmount = Vector3.left * (button.sizeDelta.x - button.position.x);
        button.localPosition += shiftAmount;

        yield return new WaitForSeconds(0.15f * delayMult);

        button.DOLocalMove(button.localPosition - shiftAmount, 1f).SetEase(Ease.OutCirc);
    }

    public void SetNickname(string value)
    {
        _nickname.text = string.IsNullOrEmpty(value) ? "NoName " + UnityEngine.Random.Range(1000, 9999) : value;

        PlayerPrefs.SetString(NICKNAME_SAVE_PATH, _nickname.text);
    }

    public void SetIP(string value)
    {
        _address.text = value;

        PlayerPrefs.SetString(ADDRESS_SAVE_PATH, value);
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
