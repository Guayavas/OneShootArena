using System;
using System.Threading.Tasks;
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

    private System.Collections.Generic.Dictionary<ulong, int> seleccionNaves =
        new System.Collections.Generic.Dictionary<ulong, int>();

    void Awake()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        }
        else
        {
            Debug.LogError("NetworkManager.Singleton es NULL");
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
            }

            await BuscarOCrearLobby();
        }
        catch (Exception e)
        {
            Debug.LogError("Error inicializando: " + e);
        }
    }

    void Update()
    {
        HandleHeartbeat();
    }

    private async void HandleHeartbeat()
    {
        if (lobbyActual != null &&
            lobbyActual.HostId == AuthenticationService.Instance.PlayerId)
        {
            tiempoHeartbeat -= Time.deltaTime;

            if (tiempoHeartbeat <= 0f)
            {
                tiempoHeartbeat = 15f;
                await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id, new UpdateLobbyOptions());
            }
        }
    }

    private async Task BuscarOCrearLobby()
    {
        try
        {
            QueryLobbiesOptions opciones = new QueryLobbiesOptions
            {
                Count = 20,
                Filters = new System.Collections.Generic.List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new QueryFilter(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
                }
            };

            var resultado = await LobbyService.Instance.QueryLobbiesAsync(opciones);

            foreach (var lobby in resultado.Results)
            {
                if (lobby.Name == "OneShotArena")
                {
                    lobbyActual = lobby;
                    break;
                }
            }

            if (lobbyActual != null)
            {
                await UnirseALobby(lobbyActual.Id);
            }
            else
            {
                await CrearLobby();
                await IniciarRelayComoHost();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error buscando lobby: " + e);
            await CrearLobby();
            await IniciarRelayComoHost();
        }
    }

    // =========================
    // HOST RELAY
    // =========================
    private async Task IniciarRelayComoHost()
    {
        try
        {
            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(maxJugadores - 1);

            string joinCode =
                await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id,
                new UpdateLobbyOptions
                {
                    Data = new System.Collections.Generic.Dictionary<string, DataObject>
                    {
                        { "codigoRelay", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                    }
                });

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError("No hay UnityTransport");
                return;
            }

            // 🔥 FIX CRÍTICO: SIEMPRE WSS EN WEBGL
            var relayServerData = new RelayServerData(allocation, "wss");
            transport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            NetworkManager.Singleton.StartHost();
        }
        catch (Exception e)
        {
            Debug.LogError("Error Relay Host: " + e);
        }
    }

    // =========================
    // CLIENT RELAY
    // =========================
    private async Task UnirseALobby(string lobbyId)
    {
        try
        {
            int indexNave = PlayerPrefs.GetInt("NaveSeleccionada", 0);

            lobbyActual = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            string codigoRelay = lobbyActual.Data["codigoRelay"].Value;

            while (codigoRelay == "0")
            {
                await Task.Delay(1000);
                lobbyActual = await LobbyService.Instance.GetLobbyAsync(lobbyActual.Id);
                codigoRelay = lobbyActual.Data["codigoRelay"].Value;
            }

            JoinAllocation joinAllocation =
                await RelayService.Instance.JoinAllocationAsync(codigoRelay);

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError("No hay UnityTransport");
                return;
            }

            // 🔥 FIX CRÍTICO: SIEMPRE WSS
            var relayServerData = new RelayServerData(joinAllocation, "wss");
            transport.SetRelayServerData(relayServerData);

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.NetworkConfig.ConnectionData =
                BitConverter.GetBytes(indexNave);

            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError("Error unirse lobby: " + e);
        }
    }

    // =========================
    // LOBBY CREATION
    // =========================
    private async Task CrearLobby()
    {
        lobbyActual = await LobbyService.Instance.CreateLobbyAsync(
            "OneShotArena",
            maxJugadores
        );
    }

    // =========================
    // CONNECTION APPROVAL
    // =========================
    private void ConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        int indexNave = 0;

        if (request.Payload.Length >= 4)
        {
            indexNave = BitConverter.ToInt32(request.Payload, 0);
        }

        seleccionNaves[request.ClientNetworkId] = indexNave;

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;
    }

    // =========================
    // SPAWN PLAYER
    // =========================
    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        int indexNave = 0;

        if (!seleccionNaves.TryGetValue(clientId, out indexNave))
            indexNave = 0;

        NetorkPersistence persistence = FindObjectOfType<NetorkPersistence>();

        if (persistence == null)
        {
            Debug.LogError("No NetorkPersistence");
            return;
        }

        GameObject prefab = persistence.ObtenerPrefabJugador(indexNave);

        if (prefab == null)
        {
            Debug.LogError("Prefab null");
            return;
        }

        GameObject obj = Instantiate(prefab);
        obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            seleccionNaves.Remove(clientId);
        }
    }

    private async void OnApplicationQuit()
    {
        if (lobbyActual == null) return;

        try
        {
            if (lobbyActual.HostId == AuthenticationService.Instance.PlayerId)
                await LobbyService.Instance.DeleteLobbyAsync(lobbyActual.Id);
            else
                await LobbyService.Instance.RemovePlayerAsync(
                    lobbyActual.Id,
                    AuthenticationService.Instance.PlayerId
                );
        }
        catch { }
    }
}