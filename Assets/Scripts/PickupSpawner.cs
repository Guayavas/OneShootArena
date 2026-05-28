using Unity.Netcode;
using UnityEngine;

public class PickupSpawner : NetworkBehaviour
{
    [Header("Prefab")]
    public GameObject prefabPickup;

    [Header("Configuración")]
    public float tiempoReaparicion = 20f;
    public float alturaSpawn = 0.1023054f;
    public float radioSpawn = 5f;

    private GameObject instanciaActual;
    private float cronometro;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }
        SpawnPickup();
    }

    void Update()
    {
        if (!IsServer) return;

        bool necesitaSpawn = false;
        if (instanciaActual == null)
        {
            necesitaSpawn = true;
        }
        else
        {
            NetworkObject netObj = instanciaActual.GetComponent<NetworkObject>();
            if (netObj == null || !netObj.IsSpawned)
            {
                necesitaSpawn = true;
            }
        }

        if (necesitaSpawn)
        {
            cronometro += Time.deltaTime;
            if (cronometro >= tiempoReaparicion)
            {
                SpawnPickup();
            }
        }
    }

    private void SpawnPickup()
    {
        if (prefabPickup == null) return;

        Vector2 circuloAleatorio = Random.insideUnitCircle * radioSpawn;
        Vector3 posicionSpawn = transform.position + new Vector3(circuloAleatorio.x, alturaSpawn, circuloAleatorio.y);

        instanciaActual = Instantiate(prefabPickup, posicionSpawn, Quaternion.identity);

        NetworkObject netObj = instanciaActual.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError($"El prefab de Pickup {prefabPickup.name} no tiene NetworkObject.");
            Destroy(instanciaActual);
        }

        cronometro = 0f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioSpawn);
        Gizmos.DrawWireCube(transform.position + new Vector3(0, alturaSpawn, 0), Vector3.one * 0.5f);
    }
}
