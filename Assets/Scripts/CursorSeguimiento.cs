using UnityEngine;

public class CursorSeguimiento : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas canvas;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        Cursor.visible = false;
    }

    void Update()
    {
        if (rectTransform == null || canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            Input.mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint
        );

        rectTransform.localPosition = localPoint;
    }

    void OnDisable()
    {
        Cursor.visible = true;
    }
}