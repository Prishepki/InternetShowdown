using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EverywhereUIController : MonoBehaviour
{
    [SerializeField] private GroupsManager _groupsManager;

    public static EverywhereUIController Singleton { get; private set; }

    private void Update()
    {
        PauseMenuCheck();

        if (Input.GetKeyDown(KeyCode.Escape) && _groupsManager.EnabledGroups.Count > 0)
        {
            CanvasGroup target = _groupsManager.EnabledGroups.Last();

            _groupsManager.SetGroup(target, false, true);
            if (_groupsManager.WindowTweens.ContainsKey(target))
            {
                _groupsManager.WindowTweens[target].KillAll();
                _groupsManager.WindowTweens.Remove(target);
            }
        }
    }

    private void PauseMenuCheck()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && PauseMenu.Singleton.Active)
        {
            bool WillEnable = !PauseMenu.Singleton.PauseMenuOpened;

            if (!WillEnable && _groupsManager.EnabledGroups.Count > 0)
            {
                if (_groupsManager.EnabledGroups.Last() != PauseMenu.Singleton.PauseMenuGroup) return;
            }

            PauseMenu.Singleton.Pause(WillEnable, !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled);
            _groupsManager.SetGroup(PauseMenu.Singleton.PauseMenuGroup, WillEnable, false, false);
        }
    }

    public void Resume()
    {
        PauseMenu.Singleton.Pause(false, !EverywhereCanvas.Singleton.IsVotingActive && !ResultsWindow.Singleton.IsEnabled);
        _groupsManager.SetGroup(PauseMenu.Singleton.PauseMenuGroup, false, false, false);
    }

    public void RequestLobbySetup(float time) // тупое решение, потом буду плакать от того сколько же говна в моем коде :(
    {
        StartCoroutine(CO_RequestLobbySetup(time));
    }

    private IEnumerator CO_RequestLobbySetup(float time)
    {
        yield return new WaitUntil(() => SceneManager.GetActiveScene().buildIndex == 2);

        ExposeResults();
        EverywhereCanvas.Singleton.SwitchUIGameState(CanvasGameStates.Lobby);
    }

    public void ExposeResults()
    {
        ResultsWindow.Singleton.SetWindow(true, !PauseMenu.Singleton.PauseMenuOpened && !EverywhereCanvas.Singleton.IsVotingActive);
        _groupsManager.SetGroup(ResultsWindow.Singleton.Window, true, false, false);
    }

    public void CloseResults()
    {
        ResultsWindow.Singleton.SetWindow(false, !PauseMenu.Singleton.PauseMenuOpened && !EverywhereCanvas.Singleton.IsVotingActive);
        _groupsManager.SetGroup(ResultsWindow.Singleton.Window, false, false, false);
    }

    private void Awake()
    {
        Singleton = this;
    }
}
