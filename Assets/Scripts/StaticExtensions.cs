using UnityEngine;

public static class VectorExtensions
{
    public static Vector2 WorldToScreen(Vector3 position)
    {
        Camera referenceCam = GameObject.FindObjectOfType<Camera>();

        return referenceCam ? Vector2.zero : referenceCam.WorldToScreenPoint(position);
    }
}

public static class ColorISH
{
    public static readonly Color32 Red = new Color32(255, 66, 78, 255);
    public static readonly Color32 Green = new Color32(78, 255, 163, 255);
    public static readonly Color32 Blue = new Color32(78, 97, 255, 255);

    public static readonly Color32 Cyan = new Color32(78, 242, 255, 255);
    public static readonly Color32 Magenta = new Color32(255, 57, 204, 255);
    public static readonly Color32 Yellow = new Color32(255, 217, 78, 255);

    public static readonly Color32 Gold = new Color32(255, 214, 48, 255);
    public static readonly Color32 Silver = new Color32(168, 169, 173, 255);
    public static readonly Color32 Bronze = new Color32(187, 123, 61, 255);

    public static readonly Color LightGray = new Color(0.75f, 0.75f, 0.75f, 1);
    public static readonly Color Invisible = new Color(1, 1, 1, 0);
}
