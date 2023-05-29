using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapVoting : MonoBehaviour
{
    [Scene] public string ConnectedMap;

    [Space(9)]

    public Image MapScreenshot;
    public TMP_Text MapName;
    public Button VoteButton;

    private void Start()
    {
        VoteButton.onClick.AddListener(HideMapVoting);
    }

    public void HideMapVoting()
    {
        EverywhereCanvas.Singleton().SetMapVoting(false, false);
        SceneGameManager.Singleton().CmdVoteMap(ConnectedMap);
    }
}
