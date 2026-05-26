using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject prefabEscudo;
    [SerializeField] private GameObject prefabVelocidad;
    [SerializeField] private GameObject prefabCajaSuministros;

    [Header("Configuracion de spawn")]
    [SerializeField] private float intervaloSpawn = 8f;
    [SerializeField] private int maxPowerUpsEnMapa = 3;
    [SerializeField] private int maxCajasEnMapa = 2;

    [Header("Area de spawn")]
    [SerializeField] private Vector3 centroMapa = new Vector3(95f, 0f, 100f);
    [SerializeField] private float areaSpawnX = 55f;
    [SerializeField] private float areaSpawnZ = 65f;
    [SerializeField] private float alturaSpawn = 0.5f;
    [SerializeField] private float margenBorde = 10f;

    [Header("Auto-deteccion del Suelo")]
    [SerializeField] private bool autoDetectarSuelo = true;
    [Range(0.1f, 0.9f)]
    [SerializeField] private float fraccionAreaJugable = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool mostrarGizmosArea = true;

    private List<GameObject> powerUpsActivos = new List<GameObject>();
    private List<GameObject> cajasActivas = new List<GameObject>();
    private Vector3 centroFinal;
    private float halfX;
    private float halfZ;

    void Start()
    {
        CalcularAreaSpawn();
        StartCoroutine(RutinaSpawn());
    }

    void CalcularAreaSpawn()
    {
        if (autoDetectarSuelo)
        {
            GameObject suelo = GameObject.FindWithTag("Suelo") ?? GameObject.Find("Suelo");
            if (suelo != null)
            {
                Vector3 escala = suelo.transform.localScale;
                centroFinal = suelo.transform.position;
                halfX = (escala.x * 10f * fraccionAreaJugable) - margenBorde;
                halfZ = (escala.z * 10f * fraccionAreaJugable) - margenBorde;
                alturaSpawn = suelo.transform.position.y + 0.5f;
                return;
            }
        }
        centroFinal = centroMapa;
        halfX = areaSpawnX;
        halfZ = areaSpawnZ;
    }

    IEnumerator RutinaSpawn()
    {
        yield return new WaitForSeconds(2f);
        while (true)
        {
            yield return new WaitForSeconds(intervaloSpawn);
            LimpiarLista(powerUpsActivos);
            LimpiarLista(cajasActivas);

            if (powerUpsActivos.Count < maxPowerUpsEnMapa)
                SpawnearPowerUp();

            if (cajasActivas.Count < maxCajasEnMapa)
                SpawnearCaja();
        }
    }

    void SpawnearPowerUp()
    {
        List<GameObject> disponibles = new List<GameObject>();
        if (prefabEscudo != null)    disponibles.Add(prefabEscudo);
        if (prefabVelocidad != null) disponibles.Add(prefabVelocidad);
        if (disponibles.Count == 0) return;

        GameObject instancia = Instantiate(
            disponibles[Random.Range(0, disponibles.Count)],
            ObtenerPosicionAleatoria(), Quaternion.identity);
        powerUpsActivos.Add(instancia);
    }

    void SpawnearCaja()
    {
        if (prefabCajaSuministros == null) return;
        GameObject instancia = Instantiate(
            prefabCajaSuministros,
            ObtenerPosicionAleatoria(), Quaternion.identity);
        cajasActivas.Add(instancia);
    }

    Vector3 ObtenerPosicionAleatoria()
    {
        float x = centroFinal.x + Random.Range(-halfX, halfX);
        float z = centroFinal.z + Random.Range(-halfZ, halfZ);
        return new Vector3(x, alturaSpawn, z);
    }

    void LimpiarLista(List<GameObject> lista)
    {
        lista.RemoveAll(p => p == null);
    }

    void OnDrawGizmosSelected()
    {
        if (!mostrarGizmosArea) return;
        Vector3 centro = Application.isPlaying ? centroFinal : centroMapa;
        float hx = Application.isPlaying ? halfX : areaSpawnX;
        float hz = Application.isPlaying ? halfZ : areaSpawnZ;
        Vector3 size = new Vector3(hx * 2f, 0.2f, hz * 2f);
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.15f);
        Gizmos.DrawCube(new Vector3(centro.x, alturaSpawn, centro.z), size);
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(centro.x, alturaSpawn, centro.z), size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(centro.x, alturaSpawn, centro.z), 1f);
    }
}