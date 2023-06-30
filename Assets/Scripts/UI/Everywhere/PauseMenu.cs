using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Mirror;
using UnityEngine;

public class PauseMenu : MonoBehaviour, IGameCanvas
{
    [SerializeField] private CanvasGroup _pauseMenu;
    [SerializeField] private CanvasGroup _settingsWindow;

    private GroupsManager _groupsManager;
    private ResultsWindow _resultsWindow;
    private EverywhereCanvas _everywhereCanvas;

    private TweenerCore<float, float, FloatOptions> _pauseMenuTween;

    [HideInInspector] public bool PauseMenuOpened { get; private set; }

    private void Start()
    {
        _pauseMenu.alpha = 0;

        _groupsManager = GroupsManager.Singleton();
        _everywhereCanvas = EverywhereCanvas.Singleton();
        _resultsWindow = EverywhereCanvas.Results();
    }

    private void Update()
    {
        HandlePauseMenu();
    }

    private void HandlePauseMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseDynamic(!PauseMenuOpened);
        }
    }

    public void PauseDynamic(bool enable)
    {
        Pause(enable, !_everywhereCanvas.IsVotingActive && !_resultsWindow.IsActive);
    }

    public void Pause(bool enable, bool modifyCursor = true)
    {
        if (!NetworkManager.singleton.isNetworkActive) return;

        _pauseMenuTween.Kill(true);

        PauseMenuOpened = enable;

        _groupsManager.SetGroup(_pauseMenu, enable, false);

        if (enable)
        {
            if (modifyCursor) Cursor.lockState = CursorLockMode.None;

            _pauseMenuTween = _pauseMenu.DOFade(1, 0.3f).SetEase(Ease.InOutCubic);
        }
        else
        {
            if (modifyCursor) Cursor.lockState = CursorLockMode.Locked;

            _groupsManager.SetGroup(_settingsWindow, false);
            _pauseMenuTween = _pauseMenu.DOFade(0, 0.3f).SetEase(Ease.InOutCubic);
        }
    }

    public void OnDisconnect()
    {
        Pause(false, false);
    }
}
