using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefabs de power-ups")]
    [SerializeField] private GameObject prefabEscudo;
    [SerializeField] private GameObject prefabVelocidad;

    [Header("Configuracion de spawn")]
    [SerializeField] private float intervaloSpawn = 8f;
    [SerializeField] private int maxPowerUpsEnMapa = 3;

    [Header("Area de spawn")]
    [SerializeField] private Vector3 centroMapa = new Vector3(95f, 0f, 100f);
    [SerializeField] private float areaSpawnX = 55f;
    [SerializeField] private float areaSpawnZ = 65f;
    [SerializeField] private float alturaSpawn = 0.5f;
    [SerializeField] private float margenBorde = 10f;

    [Header("Cupula central - zona excluida")]
    [Tooltip("Centro de la cupula en coordenadas del mundo")]
    [SerializeField] private Vector3 centroCupula = new Vector3(95f, 0f, 100f);
    [Tooltip("Radio de la cupula. Los power-ups no spawnearan dentro de este radio")]
    [SerializeField] private float radioCupula = 20f;
    [Tooltip("Intentos maximos para encontrar posicion valida fuera de la cupula")]
    [SerializeField] private int maxIntentos = 20;

    [Header("Auto-deteccion del Suelo")]
    [SerializeField] private bool autoDetectarSuelo = true;
    [Range(0.1f, 0.9f)]
    [SerializeField] private float fraccionAreaJugable = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool mostrarGizmosArea = true;

    private List<GameObject> powerUpsActivos = new List<GameObject>();
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
            GameObject suelo = GameObject.FindWithTag("Suelo");
            if (suelo == null) suelo = GameObject.Find("Suelo");

            if (suelo != null)
            {
                Vector3 escala = suelo.transform.localScale;
                float anchoX = escala.x * 10f;
                float anchoZ = escala.z * 10f;
                centroFinal = suelo.transform.position;
                halfX = (anchoX * fraccionAreaJugable) - margenBorde;
                halfZ = (anchoZ * fraccionAreaJugable) - margenBorde;
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
            LimpiarListaDestruidos();
            if (powerUpsActivos.Count < maxPowerUpsEnMapa)
                SpawnearPowerUpAleatorio();
        }
    }

    void SpawnearPowerUpAleatorio()
    {
        List<GameObject> disponibles = new List<GameObject>();
        if (prefabEscudo != null)    disponibles.Add(prefabEscudo);
        if (prefabVelocidad != null) disponibles.Add(prefabVelocidad);
        if (disponibles.Count == 0) return;

        Vector3 posicion;
        if (!ObtenerPosicionValida(out posicion))
        {
            Debug.LogWarning("[PowerUpSpawner] No se encontro posicion valida fuera de la cupula.");
            return;
        }

        GameObject prefabElegido = disponibles[Random.Range(0, disponibles.Count)];
        GameObject instancia = Instantiate(prefabElegido, posicion, Quaternion.identity);
        powerUpsActivos.Add(instancia);
    }

    bool ObtenerPosicionValida(out Vector3 posicion)
    {
        for (int i = 0; i < maxIntentos; i++)
        {
            float x = centroFinal.x + Random.Range(-halfX, halfX);
            float z = centroFinal.z + Random.Range(-halfZ, halfZ);
            Vector3 candidata = new Vector3(x, alturaSpawn, z);

            float distCupula = Vector2.Distance(
                new Vector2(candidata.x, candidata.z),
                new Vector2(centroCupula.x, centroCupula.z)
            );

            if (distCupula > radioCupula)
            {
                posicion = candidata;
                return true;
            }
        }
        posicion = Vector3.zero;
        return false;
    }

    void LimpiarListaDestruidos()
    {
        powerUpsActivos.RemoveAll(p => p == null);
    }

    void OnDrawGizmosSelected()
    {
        if (!mostrarGizmosArea) return;

        Vector3 centro = Application.isPlaying ? centroFinal : centroMapa;
        float hx = Application.isPlaying ? halfX : areaSpawnX;
        float hz = Application.isPlaying ? halfZ : areaSpawnZ;

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.15f);
        Gizmos.DrawCube(new Vector3(centro.x, alturaSpawn, centro.z), new Vector3(hx * 2f, 0.2f, hz * 2f));

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(centro.x, alturaSpawn, centro.z), new Vector3(hx * 2f, 0.2f, hz * 2f));

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawSphere(new Vector3(centroCupula.x, alturaSpawn, centroCupula.z), radioCupula);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(new Vector3(centroCupula.x, alturaSpawn, centroCupula.z), radioCupula);
    }
}