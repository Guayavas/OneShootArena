using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetorkPersistence : MonoBehaviour
{
    [SerializeField] public GameObject prefabJugador;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
