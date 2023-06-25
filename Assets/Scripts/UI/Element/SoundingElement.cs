using UnityEngine;
using UnityEngine.EventSystems;

public class SoundingElement : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [Header("Sounds")]
    [SerializeField] private AudioClip _hoverSound;
    [SerializeField] private AudioClip _clickSound;

    public void OnPointerClick(PointerEventData eventData)
    {
        SoundSystem.PlayInterfaceSound(new SoundTransporter(_clickSound), volume: 0.55f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundSystem.PlayInterfaceSound(new SoundTransporter(_hoverSound), volume: 0.55f);
    }
}
