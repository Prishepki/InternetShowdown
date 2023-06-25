using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    [Header("Custom")]
    [SerializeField] private EverywhereCanvas _everywhereCanvas;
    [SerializeField] private Transition _transition;

    public override void Awake()
    {
        base.Awake();

        List<GameObject> projectiles = Resources.LoadAll<GameObject>("Items/Projectiles").ToList();
        List<GameObject> netPrefs = Resources.LoadAll<GameObject>("NetworkedPrefabs").ToList();

        foreach (var proj in projectiles)
        {
            netPrefs.Add(proj);
        }

        spawnPrefabs = netPrefs;

        _everywhereCanvas = EverywhereCanvas.Singleton();
        _transition = Transition.Singleton();

        _everywhereCanvas.EnableCanvasElements(false);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        IGameCanvas[] gameCanvases = FindObjectsOfType<MonoBehaviour>(true).OfType<IGameCanvas>().ToArray();
        foreach (var canvas in gameCanvases)
        {
            canvas.OnDisconnect();
        }

        Cursor.lockState = CursorLockMode.None;

        _everywhereCanvas.EnableCanvasElements(false);
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
