using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerUpSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject[] prefabsPowerUps;

    [Header("Zona de Spawn (define los límites del mapa)")]
    public Vector3 centroMapa = Vector3.zero;
    public Vector3 tamanoMapa = new Vector3(50f, 0f, 50f); // X y Z del área jugable

    [Header("Configuración")]
    public float tiempoEntreSpawns = 15f;
    public int maxPowerUps = 5;
    public float alturaSpawn = 0.1023054f;

    [Header("Validación de Posición")]
    public float radioVerificacion = 0.8f;
    public LayerMask capasObstaculos;
    public int maxIntentos = 20;

    private List<GameObject> powerUpsActivos = new List<GameObject>();
    private float cronometro;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { enabled = false; return; }
        cronometro = tiempoEntreSpawns;
    }

    void Update()
    {
        if (!IsServer) return;

        powerUpsActivos.RemoveAll(item =>
            item == null || !item.GetComponent<NetworkObject>().IsSpawned);

        cronometro -= Time.deltaTime;
        if (cronometro <= 0f)
        {
            cronometro = tiempoEntreSpawns;
            if (powerUpsActivos.Count < maxPowerUps)
                SpawnPowerUp();
        }
    }

    private void SpawnPowerUp()
    {
        if (prefabsPowerUps == null || prefabsPowerUps.Length == 0) return;

        Vector3 posicionValida = Vector3.zero;
        bool encontrado = false;

        for (int i = 0; i < maxIntentos; i++)
        {
            // Posición aleatoria dentro del rectángulo del mapa
            Vector3 candidata = new Vector3(
                centroMapa.x + Random.Range(-tamanoMapa.x / 2f, tamanoMapa.x / 2f),
                alturaSpawn,
                centroMapa.z + Random.Range(-tamanoMapa.z / 2f, tamanoMapa.z / 2f)
            );

            // 1. ¿Hay obstáculo aquí?
            if (Physics.OverlapSphere(candidata, radioVerificacion, capasObstaculos).Length > 0)
                continue;

            // 2. ¿Hay otro powerup demasiado cerca?
            if (HayPowerUpCerca(candidata))
                continue;

            posicionValida = candidata;
            encontrado = true;
            break;
        }

        if (!encontrado)
        {
            Debug.LogWarning("[PowerUpSpawner] No se encontró posición libre tras " + maxIntentos + " intentos.");
            return;
        }

        int idx = Random.Range(0, prefabsPowerUps.Length);
        GameObject instancia = Instantiate(prefabsPowerUps[idx], posicionValida, Quaternion.identity);
        NetworkObject netObj = instancia.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            powerUpsActivos.Add(instancia);
        }
        else
        {
            Debug.LogError("[PowerUpSpawner] El prefab no tiene NetworkObject.");
            Destroy(instancia);
        }
    }

    private bool HayPowerUpCerca(Vector3 posicion)
    {
        foreach (var pu in powerUpsActivos)
            if (pu != null && Vector3.Distance(pu.transform.position, posicion) < radioVerificacion * 2f)
                return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // Dibuja el área de spawn en la escena
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawCube(new Vector3(centroMapa.x, alturaSpawn, centroMapa.z), tamanoMapa);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(centroMapa.x, alturaSpawn, centroMapa.z), tamanoMapa);
    }
}