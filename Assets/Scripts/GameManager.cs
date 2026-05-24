using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
    [SerializeField] private GameObject[] prefabsNaves;
    private Lobby lobbyActual;
    private float tiempoHeartbeat;

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Conectado con ID: " + AuthenticationService.Instance.PlayerId);
            }

            await BuscarOCrearLobby();
        }
        catch (Exception e)
        {
            Debug.LogError("Error en Inicialización: " + e.Message);
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
                // Si SendHeartbeatAsync no está disponible, se suele usar un UpdateLobby vacío
                // pero lo más probable es que sea un problema de versión o referencia.
                // Intentaremos con una actualización para mantenerlo vivo.
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
                Count = 1,
                Filters = new System.Collections.Generic.List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new QueryFilter(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ)
                }
            };

            QueryResponse resultado = await LobbyService.Instance.QueryLobbiesAsync(opciones);

            if (resultado.Results.Count > 0)
            {
                lobbyActual = resultado.Results[0];
                Debug.Log("Lobby encontrado: " + lobbyActual.Id);
                await UnirseALobby(lobbyActual.Id);
            }
            else
            {
                Debug.Log("No hay lobbies disponibles, creando uno...");
                await CrearLobby();
                await IniciarRelay();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error al buscar lobby: " + e.Message);
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
            Debug.Log("Relay Join Code: " + joinCode);

            await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id, new UpdateLobbyOptions
            {
                Data = new System.Collections.Generic.Dictionary<string, DataObject>
                {
                    { "codigoRelay", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            });

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;
            NetworkManager.Singleton.StartHost();
            Debug.Log("Host iniciado");
        }
        catch (Exception e)
        {
            Debug.LogError("Error en IniciarRelay: " + e.Message);
        }
    }

    private async Task CrearLobby()
    {
        try
        {
            CreateLobbyOptions opcionesLobby = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new System.Collections.Generic.Dictionary<string, DataObject>
                {
                    { "codigoRelay", new DataObject(DataObject.VisibilityOptions.Public, "0") }
                }
            };
            lobbyActual = await LobbyService.Instance.CreateLobbyAsync("OneShotArena", maxJugadores, opcionesLobby);
            Debug.Log("Lobby creado: " + lobbyActual.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Error al crear lobby: " + e.Message);
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
                    Data = new System.Collections.Generic.Dictionary<string, PlayerDataObject>
                    {
                        { "NaveIndex", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, indexNave.ToString()) }
                    }
                }
            };

            lobbyActual = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, opciones);

            string codigoRelay = lobbyActual.Data.ContainsKey("codigoRelay") ? lobbyActual.Data["codigoRelay"].Value : "0";

            int reintentos = 0;
            while (codigoRelay == "0" && reintentos < 10)
            {
                Debug.Log("Esperando código de Relay...");
                await Task.Delay(1000);
                lobbyActual = await LobbyService.Instance.GetLobbyAsync(lobbyActual.Id);
                codigoRelay = lobbyActual.Data["codigoRelay"].Value;
                reintentos++;
            }

            if (codigoRelay == "0")
            {
                Debug.LogError("No se pudo obtener el código de Relay");
                return;
            }

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(codigoRelay);
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(indexNave);
            NetworkManager.Singleton.StartClient();
            Debug.Log("Cliente unido al Relay");
        }
        catch (Exception e)
        {
            Debug.LogError("Error al unirse: " + e.Message);
        }
    }

    private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        int indexNave = 0;
        if (request.Payload.Length >= 4)
        {
            indexNave = System.BitConverter.ToInt32(request.Payload, 0);
        }

        response.Approved = true;
        response.CreatePlayerObject = true;

        // Asignar el hash del prefab correspondiente según la selección
        if (prefabsNaves != null && indexNave >= 0 && indexNave < prefabsNaves.Length)
        {
            // Obtenemos el prefab de la lista de prefabs del NetworkManager
            GameObject navePrefab = prefabsNaves[indexNave];
            foreach (var networkPrefab in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
            {
                if (networkPrefab.Prefab == navePrefab)
                {
                    response.PlayerPrefabHash = networkPrefab.Hash;
                    break;
                }
            }
        }

        response.Pending = false;
    }

    private async void OnApplicationQuit()
    {
        if (lobbyActual != null)
        {
            try
            {
                if (lobbyActual.HostId == AuthenticationService.Instance.PlayerId)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(lobbyActual.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(lobbyActual.Id, AuthenticationService.Instance.PlayerId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error al cerrar lobby al salir: " + e.Message);
            }
        }
    }
}