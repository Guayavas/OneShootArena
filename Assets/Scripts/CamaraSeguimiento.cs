using UnityEngine;
using Unity.Netcode;

public class CamaraSeguimiento : MonoBehaviour
{
    /* Cuando este configurado netcode se usara este script
    [SerializeField] private Vector3 offset = new Vector3(0f, 20f, -15f);
    [SerializeField] private float suavizado = 5f;

    private Transform objetivo;

    void LateUpdate()
    {
        if (objetivo == null)
            BuscarJugadorLocal();

        if (objetivo == null) return;

        Vector3 posicionObjetivo = objetivo.position + offset;
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, suavizado * Time.deltaTime);
    }

    void BuscarJugadorLocal()
    {
        foreach (var jugador in FindObjectsOfType<NetworkObject>())
        {
            if (jugador.IsOwner)
            {
                objetivo = jugador.transform;
                break;
            }
        }
    }
    */

    //Script para prueba
    [SerializeField] private Transform objetivo;
    [SerializeField] private Vector3 offset = new Vector3(0f, 15f, -10f);
    [SerializeField] private float suavizado = 5f;

    private Quaternion rotacionInicial;

    void Start()
    {
        rotacionInicial = transform.rotation;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        Vector3 posicionObjetivo = objetivo.position + offset;
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, suavizado * Time.deltaTime);
        transform.rotation = rotacionInicial;
    }

}
