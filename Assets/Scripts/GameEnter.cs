using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEnter : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(LoadLobby), 0);
    }

    private void LoadLobby()
    {
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }
}
