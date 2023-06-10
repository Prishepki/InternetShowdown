using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
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

        EverywhereCanvas.Singleton().EnableCanvasElements(false);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        Cursor.lockState = CursorLockMode.None;

        EverywhereCanvas.Singleton().EnableCanvasElements(false);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        Cursor.lockState = CursorLockMode.Locked;

        EverywhereCanvas.Singleton().EnableCanvasElements(true);

        StartCoroutine(WaitForSceneGameManagerSingleton());
    }

    private IEnumerator WaitForSceneGameManagerSingleton()
    {
        yield return new WaitUntil(() => SceneGameManager.Singleton() != null);

        SceneGameManager sceneGameManager = SceneGameManager.Singleton();

        sceneGameManager.RecieveUIGameState();
        sceneGameManager.CmdAskForMapVoting();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Invoke(nameof(StartGameLoop), 1f); // ибал в рот
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        GameLoop gameLoop = GameLoop.Singleton();
        if (gameLoop == null) return;

        gameLoop.OnSceneLoaded();
    }

    public override void OnServerChangeScene(string newSceneName)
    {
        base.OnServerChangeScene(newSceneName);

        GameLoop gameLoop = GameLoop.Singleton();
        if (gameLoop == null) return;

        gameLoop.OnSceneUnloaded();
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();

        StartCoroutine(nameof(WaitForMusic));
    }

    private IEnumerator WaitForMusic()
    {
        yield return new WaitUntil(() => SceneGameManager.Singleton() != null);

        SceneGameManager.Singleton().CmdAskForMusic(NetworkClient.localPlayer);
    }

    private void StartGameLoop()
    {
        GameLoop.Singleton().StartLoop();
    }
}
