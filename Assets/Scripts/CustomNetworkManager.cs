using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

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

        SceneGameManager.Singleton().RecieveUIGameState();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Invoke(nameof(StartGameLoop), 1f); // ибал в рот
    }

    private void StartGameLoop()
    {
        FindObjectOfType<GameLoop>().StartLoop();
    }
}
