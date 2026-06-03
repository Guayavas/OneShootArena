using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ScoreManagerUI : MonoBehaviour
{
    [Header("Puntaje local")]
    [SerializeField] private TMP_Text textoScoreLocal;

    [Header("Ranking pequeño Top 3")]
    [SerializeField] private RankingItemUI[] rankingItems;

    [Header("Panel final")]
    [SerializeField] private GameObject panelFinalPartida;
    [SerializeField] private RankingItemUI[] rankingFinalItems;

    [Header("Configuración")]
    [SerializeField] private float updateInterval = 0.5f;

    private float timer;
    private bool partidaFinalizada;

    [System.Serializable]
    public class RankingItemUI
    {
        public GameObject rootObject;
        public TMP_Text nombreTexto;
        public TMP_Text scoreTexto;
    }

    private void Start()
    {
        if (panelFinalPartida != null)
            panelFinalPartida.SetActive(false);

        ActualizarInterfaz();
    }

    private void Update()
    {
        // Prueba temporal: presiona F para mostrar el panel final
        if (Input.GetKeyDown(KeyCode.F))
        {
            MostrarPanelFinal();
        }

        if (partidaFinalizada)
            return;

        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            timer = 0f;
            ActualizarInterfaz();
        }
    }

    public void ActualizarInterfaz()
    {
        List<PlayerStats> jugadores = ObtenerJugadoresOrdenados();

        ActualizarScoreLocal(jugadores);
        ActualizarRanking(rankingItems, jugadores);
    }

    private List<PlayerStats> ObtenerJugadoresOrdenados()
    {
        PlayerStats[] todos = FindObjectsOfType<PlayerStats>();

        return todos
            .Where(p => p != null)
            .OrderByDescending(p => p.score.Value)
            .ToList();
    }

    private void ActualizarScoreLocal(List<PlayerStats> jugadores)
    {
        if (textoScoreLocal == null)
            return;

        PlayerStats jugadorLocal = jugadores.FirstOrDefault(p => p.IsOwner);

        if (jugadorLocal != null)
            textoScoreLocal.text = jugadorLocal.score.Value.ToString();
        else
            textoScoreLocal.text = "0";
    }

    private void ActualizarRanking(RankingItemUI[] items, List<PlayerStats> jugadores)
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            if (i < jugadores.Count)
            {
                PlayerStats jugador = jugadores[i];

                string nickname = jugador.nickname.Value.ToString();

                if (string.IsNullOrWhiteSpace(nickname))
                    nickname = "Jugador " + (i + 1);

                if (items[i].nombreTexto != null)
                    items[i].nombreTexto.text = nickname;

                if (items[i].scoreTexto != null)
                    items[i].scoreTexto.text = jugador.score.Value + " pts";

                if (items[i].rootObject != null)
                    items[i].rootObject.SetActive(true);
            }
            else
            {
                if (items[i].nombreTexto != null)
                    items[i].nombreTexto.text = "Jugador " + (i + 1);

                if (items[i].scoreTexto != null)
                    items[i].scoreTexto.text = "0 pts";

                if (items[i].rootObject != null)
                    items[i].rootObject.SetActive(true);
            }
        }
    }

    public void MostrarPanelFinal()
    {
        partidaFinalizada = true;

        List<PlayerStats> jugadores = ObtenerJugadoresOrdenados();

        if (panelFinalPartida != null)
            panelFinalPartida.SetActive(true);

        ActualizarRanking(rankingFinalItems, jugadores);
    }

    public void OcultarPanelFinal()
    {
        partidaFinalizada = false;

        if (panelFinalPartida != null)
            panelFinalPartida.SetActive(false);
    }
}