using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
    [Header("Custom")]
    [SerializeField] private EverywhereCanvas _everywhereCanvas;
    [SerializeField] private Transition _transition;

    private bool _sceneChanged;

    public override void Awake()
    {
        base.Awake();

        List<GameObject> projectiles = Resources.LoadAll<GameObject>("Items/Projectiles").ToList();
        List<GameObject> netPrefs = Resources.LoadAll<GameObject>("NetworkedPrefabs").ToList();

        netPrefs.AddRange(projectiles);

        spawnPrefabs = netPrefs;

        _everywhereCanvas = EverywhereCanvas.Singleton();
        _transition = Transition.Singleton();

        _everywhereCanvas.EnableCanvasElements(false);

        SceneManager.activeSceneChanged += (Scene a, Scene b) => _sceneChanged = true;
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        StartCoroutine(OnDisconnect());
    }

    private IEnumerator OnDisconnect()
    {
        IGameCanvas[] gameCanvases = FindObjectsOfType<MonoBehaviour>(true).OfType<IGameCanvas>().ToArray();
        foreach (var canvas in gameCanvases)
        {
            canvas.OnDisconnect();
        }

        _everywhereCanvas.EnableCanvasElements(false);

        yield return new WaitUntil(() => _sceneChanged);
        _sceneChanged = false;

        OnEnterMenu();
    }

    private void OnEnterMenu()
    {
        _transition.AwakeTransition(TransitionMode.Out);
        Cursor.lockState = CursorLockMode.None;
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        Cursor.lockState = CursorLockMode.Locked;

        _everywhereCanvas.EnableCanvasElements(true);
        _transition.AwakeTransition(TransitionMode.Out);

        StartCoroutine(WaitForSceneGameManagerSingleton());
    }

    private IEnumerator WaitForSceneGameManagerSingleton()
    {
        yield return new WaitUntil(() => SceneGameManager.Singleton() != null);

        SceneGameManager sceneGameManager = SceneGameManager.Singleton();

        sceneGameManager.RecieveUIGameState();
        sceneGameManager.CmdAskForMapVoting(NetworkClient.localPlayer);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        StartCoroutine(nameof(WaitForGameLoop));
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        GameLoop.Singleton()?.SetSceneLoaded(true);
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        base.OnServerChangeScene(newSceneName);

        GameLoop.Singleton()?.SetSceneLoaded(false);
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();

        _transition.AwakeTransition(TransitionMode.Out);

        StartCoroutine(nameof(WaitForMusic));
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        EverywhereCanvas.Results().SetWindow(false);
    }

    private IEnumerator WaitForMusic()
    {
        yield return new WaitUntil(() => SceneGameManager.Singleton());

        SceneGameManager.Singleton().CmdAskForMusic(NetworkClient.localPlayer);
    }

    private IEnumerator WaitForGameLoop()
    {
        yield return new WaitUntil(() => GameLoop.Singleton());

        GameLoop.Singleton().StartLoop();
    }
}
