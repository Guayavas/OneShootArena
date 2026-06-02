using UnityEngine;
using UnityEngine.EventSystems;

public class UIAudioButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance?.ReproducirHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance?.ReproducirClick();
    }
}
