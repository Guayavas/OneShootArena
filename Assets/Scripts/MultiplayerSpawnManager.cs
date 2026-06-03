using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnZonesManager : NetworkBehaviour
{
    [Header("Zonas de spawn de jugadores")]
    public Transform[] zonasSpawn = new Transform[4];

    [Header("Configuración")]
    public float radioAleatorio = 2f;
    public float alturaSpawn = 0.1f;

    private List<int> zonasUsadas = new List<int>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // También posiciona a los jugadores que ya existan, por ejemplo el host
        StartCoroutine(PosicionarJugadoresExistentes());
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer)
            return;

        StartCoroutine(EsperarYPosicionarJugador(clientId));
    }

    private IEnumerator EsperarYPosicionarJugador(ulong clientId)
    {
        NetworkObject jugador = null;

        float tiempoMaximo = 5f;
        float tiempo = 0f;

        while (jugador == null && tiempo < tiempoMaximo)
        {
            jugador = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            tiempo += Time.deltaTime;
            yield return null;
        }

        if (jugador == null)
        {
            Debug.LogWarning("[PlayerSpawnZonesManager] No se encontró jugador para clientId: " + clientId);
            yield break;
        }

        MoverJugadorAZona(jugador, clientId);
    }

    private IEnumerator PosicionarJugadoresExistentes()
    {
        yield return new WaitForSeconds(0.5f);

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            NetworkObject jugador = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

            if (jugador != null)
            {
                MoverJugadorAZona(jugador, clientId);
            }
        }
    }

    private void MoverJugadorAZona(NetworkObject jugador, ulong clientId)
    {
        Vector3 posicion = ObtenerPosicionAleatoria();

        jugador.transform.position = posicion;

        Debug.Log("[PlayerSpawnZonesManager] Jugador " + clientId + " spawneado en " + posicion);
    }

    private Vector3 ObtenerPosicionAleatoria()
    {
        if (zonasSpawn == null || zonasSpawn.Length == 0)
        {
            Debug.LogWarning("[PlayerSpawnZonesManager] No hay zonas asignadas.");
            return Vector3.zero;
        }

        int indiceZona = ObtenerZonaDisponible();
        Transform zona = zonasSpawn[indiceZona];

        Vector2 randomCircle = Random.insideUnitCircle * radioAleatorio;

        Vector3 posicion = new Vector3(
            zona.position.x + randomCircle.x,
            alturaSpawn,
            zona.position.z + randomCircle.y
        );

        return posicion;
    }

    private int ObtenerZonaDisponible()
    {
        if (zonasUsadas.Count >= zonasSpawn.Length)
        {
            zonasUsadas.Clear();
        }

        int indice = 0;
        int intentos = 0;

        do
        {
            indice = Random.Range(0, zonasSpawn.Length);
            intentos++;
        }
        while (zonasUsadas.Contains(indice) && intentos < 20);

        zonasUsadas.Add(indice);

        return indice;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (zonasSpawn == null)
            return;

        Gizmos.color = Color.green;

        foreach (Transform zona in zonasSpawn)
        {
            if (zona == null)
                continue;

            Gizmos.DrawWireSphere(zona.position, radioAleatorio);
        }
    }
}
