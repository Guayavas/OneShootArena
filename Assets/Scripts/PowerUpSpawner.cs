using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerUpSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject[] prefabsPowerUps;

    [Header("Configuración")]
    public float tiempoEntreSpawns = 15f;
    public int maxPowerUps = 5;
    public float radioSpawn = 25f;
    public float alturaSpawn = 0.1023054f;

    private List<GameObject> powerUpsActivos = new List<GameObject>();
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
        powerUpsActivos.RemoveAll(item => item == null || !item.GetComponent<NetworkObject>().IsSpawned);

        cronometro -= Time.deltaTime;
        if (cronometro <= 0f)
        {
            cronometro = tiempoEntreSpawns;

            if (powerUpsActivos.Count < maxPowerUps)
            {
                SpawnPowerUp();
            }
        }
    }

    private void SpawnPowerUp()
    {
        if (prefabsPowerUps == null || prefabsPowerUps.Length == 0) return;

        Vector2 circuloAleatorio = Random.insideUnitCircle * radioSpawn;
        Vector3 posicionSpawn = transform.position + new Vector3(circuloAleatorio.x, alturaSpawn, circuloAleatorio.y);

        int indiceAleatorio = Random.Range(0, prefabsPowerUps.Length);
        GameObject prefab = prefabsPowerUps[indiceAleatorio];

        GameObject instancia = Instantiate(prefab, posicionSpawn, Quaternion.identity);
        NetworkObject netObj = instancia.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            powerUpsActivos.Add(instancia);
        }
        else
        {
            Debug.LogError("El prefab de PowerUp no tiene NetworkObject");
            Destroy(instancia);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioSpawn);
    }
}
