using Unity.Netcode;
using UnityEngine;

public class PickupSpawner : NetworkBehaviour
{
    [Header("Prefab")]
    public GameObject prefabPickup;

    [Header("Configuración")]
    public float tiempoReaparicion = 20f;
    public float alturaSpawn = 0.1023054f;

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

        if (instanciaActual == null || !instanciaActual.GetComponent<NetworkObject>().IsSpawned)
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

        Vector3 posicionSpawn = transform.position + new Vector3(0, alturaSpawn, 0);
        instanciaActual = Instantiate(prefabPickup, posicionSpawn, Quaternion.identity);

        NetworkObject netObj = instanciaActual.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }

        cronometro = 0f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, alturaSpawn, 0), Vector3.one * 0.5f);
    }
}
