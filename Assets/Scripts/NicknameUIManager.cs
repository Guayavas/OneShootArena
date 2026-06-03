using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NicknameUIManager : MonoBehaviour
{
    [Header("Ajuste sobre la nave")]
    public Vector3 offsetMundo = new Vector3(0, 1.2f, 0);
    public Vector2 offsetPantalla = new Vector2(0, 25);

    [Header("Estilo")]
    public int tamanoFuente = 26;
    public Color colorTexto = Color.white;

    private Camera camaraPrincipal;
    private Canvas canvasNicknames;
    private RectTransform canvasRect;

    private Dictionary<PlayerStats, TextMeshProUGUI> textos = new Dictionary<PlayerStats, TextMeshProUGUI>();

    void Start()
    {
        camaraPrincipal = Camera.main;
        CrearCanvasNicknames();
    }

    void Update()
    {
        if (camaraPrincipal == null)
            camaraPrincipal = Camera.main;

        if (camaraPrincipal == null)
            return;

        ActualizarListaJugadores();
        ActualizarPosiciones();
    }

    private void CrearCanvasNicknames()
    {
        GameObject existente = GameObject.Find("CanvasNicknames");

        if (existente != null)
        {
            canvasNicknames = existente.GetComponent<Canvas>();
            canvasRect = canvasNicknames.GetComponent<RectTransform>();
            return;
        }

        GameObject nuevoCanvas = new GameObject("CanvasNicknames");

        canvasNicknames = nuevoCanvas.AddComponent<Canvas>();
        canvasNicknames.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasNicknames.sortingOrder = 999;

        CanvasScaler scaler = nuevoCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        nuevoCanvas.AddComponent<GraphicRaycaster>();

        canvasRect = nuevoCanvas.GetComponent<RectTransform>();
    }

    private void ActualizarListaJugadores()
    {
        PlayerStats[] jugadores = FindObjectsOfType<PlayerStats>();

        foreach (PlayerStats jugador in jugadores)
        {
            if (jugador == null)
                continue;

            if (!textos.ContainsKey(jugador))
            {
                CrearTextoParaJugador(jugador);
            }
        }
    }

    private void CrearTextoParaJugador(PlayerStats jugador)
    {
        GameObject objTexto = new GameObject("Nickname_" + jugador.OwnerClientId);
        objTexto.transform.SetParent(canvasNicknames.transform, false);

        TextMeshProUGUI texto = objTexto.AddComponent<TextMeshProUGUI>();
        texto.text = "Jugador";
        texto.fontSize = tamanoFuente;
        texto.color = colorTexto;
        texto.alignment = TextAlignmentOptions.Center;
        texto.enableWordWrapping = false;
        texto.raycastTarget = false;

        RectTransform rect = texto.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 50);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        textos.Add(jugador, texto);

        Debug.Log("Nickname UI creado para: " + jugador.gameObject.name);
    }

    private void ActualizarPosiciones()
    {
        foreach (var par in textos)
        {
            PlayerStats jugador = par.Key;
            TextMeshProUGUI texto = par.Value;

            if (jugador == null || texto == null)
                continue;

            string nick = jugador.nickname.Value.ToString();

            if (string.IsNullOrWhiteSpace(nick))
                nick = "Jugador";

            texto.text = nick;

            Vector3 screenPos = camaraPrincipal.WorldToScreenPoint(jugador.transform.position + offsetMundo);

            if (screenPos.z < 0)
            {
                texto.gameObject.SetActive(false);
                continue;
            }

            texto.gameObject.SetActive(true);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                null,
                out Vector2 localPoint
            );

            RectTransform rect = texto.GetComponent<RectTransform>();
            rect.anchoredPosition = localPoint + offsetPantalla;
        }
    }
}