using UnityEngine;

public class CursorSeguimiento : MonoBehaviour
{
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        // Oculta el puntero blanco normal de Windows
        Cursor.visible = false; 
    }

    void Update()
    {
        // Obliga al objeto a emparejar sus coordenadas con las del mouse
        if (rectTransform != null)
        {
            rectTransform.position = Input.mousePosition;
        }
    }

    void OnDisable()
    {
        // Si sales del juego, te devuelve tu mouse normal
        Cursor.visible = true; 
    }
}
