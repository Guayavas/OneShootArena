using UnityEngine;

public class CursorSeguimiento : MonoBehaviour
{
    public Texture2D cursorTexture;

    void Awake()
    {
        // Esta es la parte que hace que el objeto NO se destruya
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Vector2 hotspot = new Vector2(10, 10);
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }
}