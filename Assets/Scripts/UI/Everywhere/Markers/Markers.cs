using UnityEngine;

public class Markers : MonoBehaviour, IEverywhereCanvas
{
    public static Markers Singleton { get; private set; }

    [SerializeField] private CanvasGroup _hitMarker;
    [SerializeField] private CanvasGroup _damageMarker;

    public bool Active { get; set; }

    public void Reset()
    {
        Singleton = this;

        _hitMarker.alpha = 0;
        _damageMarker.alpha = 0;
    }

    public void OnDisconnect() { }
}
