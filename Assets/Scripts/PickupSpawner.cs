using Unity.Netcode;
using UnityEngine;

public class PickupSpawner : NetworkBehaviour
{
    [Header("Prefab")]
    public GameObject prefabPickup;

    [Header("Configuración")]
    public float tiempoEntreSpawns = 10f;
    public int maxPickups = 5;
    public float alturaSpawn = 0.1023054f;
    public float radioSpawn = 15f;

    private List<GameObject> pickupsActivos = new List<GameObject>();
    private float cronometro;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }
        cronometro = tiempoEntreSpawns;
    }

    void Update()
    {
        if (!IsServer) return;

        // Limpiar lista de objetos destruidos o despawneados
        pickupsActivos.RemoveAll(item => item == null || !item.GetComponent<NetworkObject>().IsSpawned);

        cronometro -= Time.deltaTime;
        if (cronometro <= 0f)
        {
            cronometro = tiempoEntreSpawns;

            if (pickupsActivos.Count < maxPickups)
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

        GameObject instancia = Instantiate(prefabPickup, posicionSpawn, Quaternion.identity);

        NetworkObject netObj = instancia.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            pickupsActivos.Add(instancia);
        }
        else
        {
            Debug.LogError($"El prefab de Pickup {prefabPickup.name} no tiene NetworkObject.");
            Destroy(instancia);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioSpawn);
        Gizmos.DrawWireCube(transform.position + new Vector3(0, alturaSpawn, 0), Vector3.one * 0.5f);
    }
}
