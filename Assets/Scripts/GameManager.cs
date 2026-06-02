using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay; // Necesario para RelayServerData nativo
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

    // Diccionario para recordar qué nave eligió cada cliente
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
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[LOG] Conectado con ID: " + AuthenticationService.Instance.PlayerId);
            }

            await BuscarOCrearLobby();
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR INIT] " + e.Message);
        }
    }

    void Update()
    {
        HandleHeartbeat();
    }

    private async void HandleHeartbeat()
    {
        if (lobbyActual != null && lobbyActual.HostId == AuthenticationService.Instance.PlayerId)
        {
            tiempoHeartbeat -= Time.deltaTime;
            if (tiempoHeartbeat <= 0f)
            {
                tiempoHeartbeat = 15f;
                try { await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id, new UpdateLobbyOptions()); } catch { }
            }
        }
    }

    private async Task BuscarOCrearLobby()
    {
        try
        {
            int intentos = 0;
            QueryResponse resultado = null;

            while (intentos < 3)
            {
                QueryLobbiesOptions opciones = new QueryLobbiesOptions
                {
                    Count = 10,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
                    }
                };

                resultado = await LobbyService.Instance.QueryLobbiesAsync(opciones);

                if (resultado.Results.Count > 0)
                {
                    foreach (var lobby in resultado.Results)
                    {
                        if (lobby.Name == "OneShotArena")
                        {
                            lobbyActual = lobby;
                            break;
                        }
                    }
                }

                if (lobbyActual != null) break;

                intentos++;
                await Task.Delay(1000);
            }

            if (lobbyActual != null)
            {
                Debug.Log("[LOG] Lobby encontrado: " + lobbyActual.Id);
                await UnirseALobby(lobbyActual.Id);
            }
            else
            {
                Debug.Log("[LOG] Creando nueva partida...");
                await CrearLobby();
                await IniciarRelay();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[WARN LOBBY] Falló búsqueda, forzando creación: " + e.Message);
            await CrearLobby();
            await IniciarRelay();
        }
    }

    private async Task IniciarRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxJugadores - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("[LOG] Relay Join Code: " + joinCode);

            await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "codigoRelay", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            });

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null) return;

            // 🔥 SOLUCIÓN DEFINITIVA: Forzamos WSS crudo, CERO UDP para evitar crasheos del componente
            var relayServerData = new RelayServerData(allocation, "wss");
            transport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Tu lógica original intacta: Registrar al Host (ID 0) antes de arrancar
            int indexNave = PlayerPrefs.GetInt("NaveSeleccionada", 0);
            seleccionNaves[0] = indexNave;
            Debug.Log($"[HOST] Registrando nave local index {indexNave} para ClientId 0");

            NetworkManager.Singleton.StartHost();
            Debug.Log("[LOG] Host iniciado con éxito");
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR RELAY HOST] " + e.Message);
        }
    }

    private async Task CrearLobby()
    {
        try
        {
            CreateLobbyOptions opcionesLobby = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "codigoRelay", new DataObject(DataObject.VisibilityOptions.Public, "0") }
                }
            };
            lobbyActual = await LobbyService.Instance.CreateLobbyAsync("OneShotArena", maxJugadores, opcionesLobby);
            Debug.Log("[LOG] Lobby creado: " + lobbyActual.Id);
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR CREAR LOBBY] " + e.Message);
        }
    }

    private async Task UnirseALobby(string lobbyId)
    {
        try
        {
            int indexNave = PlayerPrefs.GetInt("NaveSeleccionada", 0);
            JoinLobbyByIdOptions opciones = new JoinLobbyByIdOptions
            {
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "NaveIndex", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, indexNave.ToString()) }
                    }
                }
            };

            lobbyActual = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, opciones);
            string codigoRelay = lobbyActual.Data.ContainsKey("codigoRelay") ? lobbyActual.Data["codigoRelay"].Value : "0";

            int reintentos = 0;
            while (codigoRelay == "0" && reintentos < 5)
            {
                await Task.Delay(1000);
                lobbyActual = await LobbyService.Instance.GetLobbyAsync(lobbyActual.Id);
                codigoRelay = lobbyActual.Data["codigoRelay"].Value;
                reintentos++;
            }

            if (codigoRelay == "0") return;

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(codigoRelay);
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null) return;

            // 🔥 SOLUCIÓN DEFINITIVA: Forzamos WSS crudo para el cliente también
            var relayServerData = new RelayServerData(joinAllocation, "wss");
            transport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.NetworkConfig.ConnectionData = BitConverter.GetBytes(indexNave);

            Debug.Log("[LOG] Uniéndose con Nave Index: " + indexNave);
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR UNIRSE CLIENTE] " + e.Message);
        }
    }

    private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        int indexNave = 0;
        if (request.Payload != null && request.Payload.Length >= 4)
        {
            indexNave = BitConverter.ToInt32(request.Payload, 0);
        }
        else
        {
            if (seleccionNaves.ContainsKey(request.ClientNetworkId))
                indexNave = seleccionNaves[request.ClientNetworkId];
        }

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;

        if (NetworkManager.Singleton.IsServer)
        {
            seleccionNaves[request.ClientNetworkId] = indexNave;
            Debug.Log($"[SERVER] Conexión Aprobada. Cliente {request.ClientNetworkId} -> Nave {indexNave}");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        int indexNave = 0;
        if (seleccionNaves.TryGetValue(clientId, out int seleccion))
        {
            indexNave = seleccion;
        }

        NetorkPersistence persistence = FindObjectOfType<NetorkPersistence>();
        GameObject navePrefab = null;

        if (persistence != null)
        {
            navePrefab = persistence.ObtenerPrefabJugador(indexNave);
        }

        if (navePrefab != null)
        {
            GameObject jugadorInstancia = Instantiate(navePrefab);
            jugadorInstancia.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            Debug.Log($"[SERVER] Éxito. Nave spawneada para Cliente {clientId}");
        }
        else
        {
            Debug.LogError("[SERVER ERROR] Prefab de nave no encontrado.");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            seleccionNaves.Remove(clientId);
        }
    }
}