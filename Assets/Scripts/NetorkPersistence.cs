using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetorkPersistence : MonoBehaviour
{
    [Header("Prefab jugador por defecto")]
    [SerializeField] public GameObject prefabJugador;

    [Header("Prefabs de naves seleccionables")]
    public GameObject[] prefabsJugadores;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public GameObject ObtenerPrefabJugador(int indiceNave)
    {
        if (prefabsJugadores != null && prefabsJugadores.Length > 0)
        {
            indiceNave = Mathf.Clamp(indiceNave, 0, prefabsJugadores.Length - 1);
            return prefabsJugadores[indiceNave];
        }

        if (prefabJugador != null)
        {
            Debug.LogWarning("No hay prefabs seleccionables asignados. Se usará prefabJugador por defecto.");
            return prefabJugador;
        }

        Debug.LogError("No hay ningún prefab de jugador asignado en NetorkPersistence.");
        return null;
    }
}
