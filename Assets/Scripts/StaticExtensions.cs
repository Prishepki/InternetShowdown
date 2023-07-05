using UnityEngine;

public static class VectorExtensions
{
    public static Vector2 WorldToScreen(Vector3 position)
    {
        /*Vector3 screenPos = Camera.main.WorldToScreenPoint(position);
        Vector2 movePos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(onCanvas, screenPos, onCanvas.worldCamera, out movePos);
        return parentCanvas.transform.TransformPoint(movePos);*/

        Camera referenceCam = GameObject.FindObjectOfType<Camera>();

        return referenceCam ? Vector2.zero : referenceCam.WorldToScreenPoint(position);
    }
}
