using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class Leaderboard : MonoBehaviour, IGameCanvas
{
    [SerializeField] private Place _placePrefab;
    [SerializeField] private Transform _placeContainer;

    private NetworkPlayer _player;

    private List<(string nickname, int score, int activity)> _leaderboard = new List<(string, int, int)>();

    public void Initialize(NetworkPlayer player)
    {
        _player = player;
        StartCoroutine(nameof(LeaderboardTickLoop));
    }

    private IEnumerator LeaderboardTickLoop()
    {
        while (NetworkClient.isConnected)
        {
            UpdateLeaderboard();

            yield return new WaitForSeconds(5f);
        }

        ClearLeaderboardUI();
    }

    public void UpdateLeaderboard()
    {
        List<(string nickname, int score, int activity)> newLeaderboardValue = new List<(string, int, int)>();

        List<NetworkPlayer> allPlayers = FindObjectsOfType<NetworkPlayer>().ToList();

        allPlayers.Sort((first, second) =>
        {
            if (first.Score == second.Score)
            {
                if (first.Activity < second.Activity) return 1;
                else return -1;
            }
            else if (first.Score < second.Score) return 1;
            else return -1;
        });

        int place = 1;

        foreach (NetworkPlayer connPlayer in allPlayers)
        {
            connPlayer.Place = place;

            newLeaderboardValue.Add((connPlayer.Nickname, connPlayer.Score, connPlayer.Activity));

            place++;
        }

        _leaderboard = newLeaderboardValue;

        UpdateLeaderboardUI();
    }

    private void UpdateLeaderboardUI()
    {
        ClearLeaderboardUI();

        int clampedLeaderboardSize = Mathf.Clamp(_leaderboard.Count, 0, 6);

        for (int place = 0; place < clampedLeaderboardSize; place++)
        {
            Place placeComp = Instantiate(_placePrefab.gameObject, _placeContainer).GetComponent<Place>();

            placeComp.Number.text = $"{place + 1})";

            switch (place + 1)
            {
                default:
                    placeComp.Number.color = Color.white;
                    break;

                case 1:
                    placeComp.Number.color = ColorISH.Gold;
                    break;

                case 2:
                    placeComp.Number.color = ColorISH.Silver;
                    break;

                case 3:
                    placeComp.Number.color = ColorISH.Bronze;
                    break;
            }

            placeComp.Nickname.text = _leaderboard[place].nickname;
            placeComp.Score.text = _leaderboard[place].score.ToString();
        }
    }

    private void ClearLeaderboardUI()
    {
        foreach (Transform place in _placeContainer)
        {
            Destroy(place.gameObject);
        }
    }

    public void OnDisconnect()
    {
        StopCoroutine(nameof(LeaderboardTickLoop));
    }
}
