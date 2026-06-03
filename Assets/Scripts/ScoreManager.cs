using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Linq;

public class ScoreManagerUI : MonoBehaviour
{
    [Header("Panel Puntaje Local (Owner)")]
    [SerializeField] private TMP_Text textoScoreLocal;

    [Header("Panel Ranking (Top 3)")]
    [SerializeField] private RankingItemUI[] rankingItems; // Asignar Item#1, Item#2, Item#3 en el inspector

    [Header("Configuración")]
    [SerializeField] private float updateInterval = 0.5f;
    private float timer;

    [System.Serializable]
    public struct RankingItemUI
    {
        public GameObject rootObject;
        public TMP_Text nombreTexto;
        public TMP_Text scoreTexto; // Opcional, si quieres mostrar el número del puntaje también en el ranking
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            ActualizarInterfaz();
        }
    }

    private void ActualizarInterfaz()
    {
        // 1. Obtener todos los PlayerStats en la escena
        PlayerStats[] todosLosStats = FindObjectsOfType<PlayerStats>();
        if (todosLosStats == null || todosLosStats.Length == 0) return;

        // 2. Actualizar Puntaje Local (Owner)
        PlayerStats localStats = todosLosStats.FirstOrDefault(s => s.IsOwner);
        if (localStats != null && textoScoreLocal != null)
        {
            textoScoreLocal.text = localStats.score.Value.ToString();
        }

        // 3. Procesar Ranking Global (Top 3)
        // Ordenar por score descendente
        var listaOrdenada = todosLosStats
            .OrderByDescending(s => s.score.Value)
            .ToList();

        for (int i = 0; i < rankingItems.Length; i++)
        {
            if (i < listaOrdenada.Count)
            {
                // Hay un jugador para esta posición
                var stat = listaOrdenada[i];
                rankingItems[i].nombreTexto.text = stat.nickname.Value.ToString();

                // Si tienes un campo para el puntaje en el ranking, lo actualizamos
                if (rankingItems[i].scoreTexto != null)
                {
                    rankingItems[i].scoreTexto.text = stat.score.Value.ToString();
                }

                // Asegurarnos de que el item sea visible
                if (rankingItems[i].rootObject != null) rankingItems[i].rootObject.SetActive(true);
            }
            else
            {
                // No hay jugador para esta posición (ej. solo hay 2 jugadores y este es el puesto 3)
                rankingItems[i].nombreTexto.text = "Jugador " + (i + 1);

                if (rankingItems[i].scoreTexto != null)
                {
                    rankingItems[i].scoreTexto.text = "0";
                }

                // Opcional: Podrías ocultarlo si prefieres, pero el usuario pidió que diga "Jugador"
                // if (rankingItems[i].rootObject != null) rankingItems[i].rootObject.SetActive(false);
            }
        }
    }
}
