using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int maxJugadores = 10;
    private Lobby lobbyActual;
    private float tiempoHeartbeat;
    private Dictionary<ulong, int> seleccionNaves = new Dictionary<ulong, int>();

    void Awake()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        }
    }

    async void Start()
    {
        try
        {
            Debug.Log("[LOG] Iniciando Servicios de Unity...");
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[LOG] Autenticando...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[LOG] Conectado con ID: " + AuthenticationService.Instance.PlayerId);
            }
            await BuscarOCrearLobby();
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR CRÍTICO] Inicialización: " + e.Message);
        }
    }

    void Update() => HandleHeartbeat();

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

    private async void HandleHeartbeat()
    {
        if (lobbyActual != null && lobbyActual.HostId == AuthenticationService.Instance.PlayerId)
        {
            tiempoHeartbeat -= Time.deltaTime;
            if (tiempoHeartbeat <= 0f)
            {
                tiempoHeartbeat = 15f;
                try { await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id, new UpdateLobbyOptions()); }
                catch (Exception e) { Debug.LogWarning("[LOG] Heartbeat falló: " + e.Message); }
            }
        }
    }

    private async Task BuscarOCrearLobby()
    {
        try
        {
            Debug.Log("[LOG] Buscando lobbies disponibles...");
            QueryLobbiesOptions opciones = new QueryLobbiesOptions { Count = 10 };
            QueryResponse resultado = await LobbyService.Instance.QueryLobbiesAsync(opciones);

            Debug.Log($"[LOG] Lobbies encontrados: {resultado.Results.Count}");
            foreach (var lobby in resultado.Results)
            {
                if (lobby.Name == "OneShotArena") { lobbyActual = lobby; break; }
            }

            if (lobbyActual != null)
            {
                Debug.Log("[LOG] Lobby encontrado: " + lobbyActual.Id);
                await UnirseALobby(lobbyActual.Id);
            }
            else
            {
                Debug.Log("[LOG] No hay lobby. Creando nuevo...");
                await CrearLobby();
                await IniciarRelay();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR] Búsqueda de lobby: " + e.Message);
            await CrearLobby();
            await IniciarRelay();
        }
    }

    private async Task IniciarRelay()
    {
        try
        {
            Debug.Log("[LOG] Solicitando asignación Relay...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxJugadores - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("[LOG] Relay Code: " + joinCode);

            await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> { { "codigoRelay", new DataObject(DataObject.VisibilityOptions.Public, joinCode) } }
            });

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServerData = new RelayServerData(allocation, "wss");
            transport.SetRelayServerData(relayServerData);

            // Limpiamos suscripciones previas por si acaso
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            int indexNave = PlayerPrefs.GetInt("NaveSeleccionada", 0);
            seleccionNaves.Clear(); // Limpiamos el diccionario al iniciar nuevo host
            seleccionNaves[0] = indexNave;

            // IMPORTANTE: El Host también debe enviar su selección en el ConnectionData
            NetworkManager.Singleton.NetworkConfig.ConnectionData = BitConverter.GetBytes(indexNave);

            Debug.Log("[LOG] Iniciando Host con nave: " + indexNave);
            NetworkManager.Singleton.StartHost();
            Debug.Log("[LOG] Host iniciado correctamente.");
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR] IniciarRelay: " + e.Message);
        }
    }

    private async Task CrearLobby()
    {
        try
        {
            lobbyActual = await LobbyService.Instance.CreateLobbyAsync("OneShotArena", maxJugadores);
            Debug.Log("[LOG] Lobby creado en nube con ID: " + lobbyActual.Id);
        }
        catch (Exception e) { Debug.LogError("[ERROR] CrearLobby: " + e.Message); }
    }

    private async Task UnirseALobby(string lobbyId)
    {
        try
        {
            Debug.Log("[LOG] Uniéndose a lobby existente...");
            lobbyActual = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            string codigoRelay = lobbyActual.Data["codigoRelay"].Value;

            while (codigoRelay == "0")
            {
                Debug.Log("[LOG] Esperando código Relay del host...");
                await Task.Delay(1000);
                lobbyActual = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                codigoRelay = lobbyActual.Data["codigoRelay"].Value;
            }

            Debug.Log("[LOG] Código Relay obtenido: " + codigoRelay);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(codigoRelay);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            var relayServerData = new RelayServerData(joinAllocation, "wss");
            transport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.NetworkConfig.ConnectionData = BitConverter.GetBytes(PlayerPrefs.GetInt("NaveSeleccionada", 0));
            NetworkManager.Singleton.StartClient();
            Debug.Log("[LOG] Cliente iniciado.");
        }
        catch (Exception e) { Debug.LogError("[ERROR] UnirseALobby: " + e.Message); }
    }

    private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // El Host también pasa por aquí al inicio
        int indexNave = 0;
        if (request.Payload != null && request.Payload.Length >= 4)
        {
            indexNave = BitConverter.ToInt32(request.Payload, 0);
        }
        else
        {
            // Fallback para el host por si el payload llega vacío en alguna versión de NGO
            if (request.ClientNetworkId == 0)
            {
                indexNave = PlayerPrefs.GetInt("NaveSeleccionada", 0);
            }
        }

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;

        seleccionNaves[request.ClientNetworkId] = indexNave;
        Debug.Log($"[LOG] ConnectionApproval: Cliente {request.ClientNetworkId} - Nave index: {indexNave} - Payload length: {(request.Payload?.Length ?? 0)}");
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log($"[LOG] Cliente {clientId} conectado.");
        int index = seleccionNaves.ContainsKey(clientId) ? seleccionNaves[clientId] : 0;
        NetorkPersistence persistence = FindObjectOfType<NetorkPersistence>();
        GameObject prefab = persistence?.ObtenerPrefabJugador(index);
        if (prefab != null)
        {
            Instantiate(prefab).GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            Debug.Log($"[LOG] Nave spawneada para {clientId}");
        }
        else Debug.LogError("[ERROR] Prefab no encontrado para spawn.");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.Log($"[LOG] Cliente {clientId} desconectado.");
        seleccionNaves.Remove(clientId);
    }
}