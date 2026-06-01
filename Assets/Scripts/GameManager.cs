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

    private bool serviciosListos = false;
    private bool iniciandoLobby = false;

    private float heartbeatTimer;

    private Dictionary<ulong, int> seleccionNaves = new Dictionary<ulong, int>();

    // =========================
    // INIT
    // =========================
    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            serviciosListos = true;

            Debug.Log("[LOG] Servicios listos");

            await Task.Delay(800); // 🔥 WebGL safety delay

            await BuscarOCrearLobby();
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR INIT] " + e);
        }
    }

    void Update()
    {
        HandleHeartbeat();
    }

    // =========================
    // HEARTBEAT LOBBY HOST
    // =========================
    private async void HandleHeartbeat()
    {
        if (lobbyActual == null) return;

        if (lobbyActual.HostId != AuthenticationService.Instance.PlayerId)
            return;

        heartbeatTimer -= Time.deltaTime;

        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = 15f;

            try
            {
                await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id, new UpdateLobbyOptions());
            }
            catch { }
        }
    }

    // =========================
    // LOBBY FLOW
    // =========================
    private async Task BuscarOCrearLobby()
    {
        if (!serviciosListos || iniciandoLobby)
            return;

        iniciandoLobby = true;

        try
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

            var result = await LobbyService.Instance.QueryLobbiesAsync(opciones);

            foreach (var lobby in result.Results)
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
                await IniciarRelayHost();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR LOBBY] " + e);
        }
        finally
        {
            iniciandoLobby = false;
        }
    }

    // =========================
    // CREATE LOBBY
    // =========================
    private async Task CrearLobby()
    {
        lobbyActual = await LobbyService.Instance.CreateLobbyAsync(
            "OneShotArena",
            maxJugadores
        );

        Debug.Log("[LOG] Lobby creado");
    }

    // =========================
    // HOST RELAY
    // =========================
    private async Task IniciarRelayHost()
    {
        try
        {
            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(maxJugadores - 1);

            string joinCode =
                await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            await LobbyService.Instance.UpdateLobbyAsync(
                lobbyActual.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "codigoRelay", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                    }
                }
            );

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError("No UnityTransport");
                return;
            }

            // 🔥 FIX WEBGL: SIEMPRE WSS
            var relayData = new RelayServerData(allocation, "wss");
            transport.SetRelayServerData(relayData);

            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            NetworkManager.Singleton.StartHost();
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR RELAY HOST] " + e);
        }
    }

    // =========================
    // CLIENT JOIN
    // =========================
    private async Task UnirseALobby(string lobbyId)
    {
        if (!serviciosListos)
            return;

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

            JoinAllocation joinAlloc =
                await RelayService.Instance.JoinAllocationAsync(codigoRelay);

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (transport == null)
            {
                Debug.LogError("No UnityTransport");
                return;
            }

            // 🔥 FIX WEBGL: SIEMPRE WSS
            var relayData = new RelayServerData(joinAlloc, "wss");
            transport.SetRelayServerData(relayData);

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.NetworkConfig.ConnectionData =
                BitConverter.GetBytes(indexNave);

            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError("[ERROR CLIENT] " + e);
        }
    }

    // =========================
    // CONNECTION APPROVAL
    // =========================
    private void ConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        int index = 0;

        if (request.Payload != null && request.Payload.Length >= 4)
        {
            index = BitConverter.ToInt32(request.Payload, 0);
        }

        seleccionNaves[request.ClientNetworkId] = index;

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

        int index = 0;

        if (!seleccionNaves.TryGetValue(clientId, out index))
            index = 0;

        NetorkPersistence persistence = FindObjectOfType<NetorkPersistence>();

        if (persistence == null)
        {
            Debug.LogError("No NetorkPersistence");
            return;
        }

        GameObject prefab = persistence.ObtenerPrefabJugador(index);

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

    // =========================
    // CLEAN EXIT
    // =========================
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