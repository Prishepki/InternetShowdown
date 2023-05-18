using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

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
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        Cursor.lockState = CursorLockMode.None;
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        Cursor.lockState = CursorLockMode.Locked;
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
