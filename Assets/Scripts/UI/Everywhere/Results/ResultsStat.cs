using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultsStat : MonoBehaviour
{
    public TMP_Text Key;
    public TMP_Text Value;
    public CanvasGroup Group;
    public Image Panel;
}

public static class ResultsStatsJobs
{
    public static readonly List<(string key, int value, Color color)> Rankings = new List<(string, int, Color)>
    {
        ( "D", 0, new Color32(95, 19, 19, 255) ),
        ( "D+", 25, new Color32(126, 26, 26, 255) ),
        ( "C", 60, new Color32(144, 72, 31, 255) ),
        ( "C+", 87, new Color32(173, 87, 39, 255) ),
        ( "B", 120, new Color32(194, 143, 41, 255) ),
        ( "B+", 150, new Color32(210, 155, 46, 255) ),
        ( "A", 240, new Color32(204, 229, 50, 255) ),
        ( "A+", 310, new Color32(227, 255, 55, 255) ),
        ( "S", 420, new Color32(91, 255, 55, 255) ),
        ( "S+", 495, new Color32(99, 255, 111, 255) ),
        ( "P", 560, new Color32(55, 222, 255, 255) ),
        ( "P+", 615, new Color32(55, 128, 255, 255) )
    };

    public static Dictionary<string, (string value, Color32 color)> StatsToDisplay = new Dictionary<string, (string, Color32)>()
    {
        { "Place", ("Null", Color.white) },
        { "Score", ("Null", Color.white) },
        { "Activity", ("Null", Color.white) },
        { "Kills", ("Null", Color.white) },
        { "Hits", ("Null", Color.white) },
        { "Deaths", ("Null", Color.white) },
        { "Traumas", ("Null", Color.white) },
        { "Rank", ("Null", Color.white) }
    };
}
