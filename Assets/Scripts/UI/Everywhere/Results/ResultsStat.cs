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

    public void Set(string key, object value, Color valueColor)
    {
        Group.alpha = 0;

        Key.text = key;

        Value.text = value.ToString();
        Value.color = valueColor;
    }

    public void ChangeValueColor(Color color)
    {
        Value.color = color;
    }
}

public static class ResultsStatsJobs
{
    public static Dictionary<string, BaseStat> StatsToDisplay = new Dictionary<string, BaseStat>()
    {
        { "Place", new PlaceStat("Null") },
        { "Score", new MoreBetterStat("Null") },
        { "Activity", new MoreBetterStat("Null") },
        { "Kills", new MoreBetterStat("Null") },
        { "Hits", new MoreBetterStat("Null") },
        { "Deaths", new LessBetterStat("Null") },
        { "Traumas", new LessBetterStat("Null") },
        { "Rank", new RankStat("Null") }
    };
}

public class BaseStat
{
    public object Value { get; set; }

    public virtual Color AskForColor() { return Color.white; }

    public BaseStat(object value)
    {
        Value = value;
    }
}

public class PlaceStat : BaseStat
{
    public PlaceStat(object value) : base(value) { }

    public override Color AskForColor()
    {
        switch (Value)
        {
            default:
                return Color.white;

            case 1:
                return ColorISH.Gold;

            case 2:
                return ColorISH.Silver;

            case 3:
                return ColorISH.Bronze;

        }
    }
}

public class MoreBetterStat : BaseStat
{
    public MoreBetterStat(object value) : base(value) { }

    public override Color AskForColor()
    {
        if (((int)Value) > 0)
        {
            return Color.white;
        }

        return ColorISH.LightGray;
    }
}

public class LessBetterStat : BaseStat
{
    public LessBetterStat(object value) : base(value) { }

    public override Color AskForColor()
    {
        if (((int)Value) == 0)
        {
            return ColorISH.Green;
        }

        return Color.white;
    }
}

public class RankStat : BaseStat
{
    public RankStat(object value) : base(value) { }

    public override Color AskForColor()
    {
        for (int i = 0; i < Rankings.Count; i++)
        {
            var rank = Rankings[i];

            if (Value.ToString() == rank.key)
            {
                return rank.color;
            }
        }

        return base.AskForColor();
    }

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
}
