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
    [SerializeField] private GameObject prefabJugador;
    private Lobby lobbyActual;

    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Conectado con ID: " + AuthenticationService.Instance.PlayerId);
        }

        await BuscarOCrearLobby();
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
                new QueryFilter(
                    QueryFilter.FieldOptions.AvailableSlots,
                    "0",
                    QueryFilter.OpOptions.GT
                )
            }
            };

            QueryResponse resultado = await LobbyService.Instance.QueryLobbiesAsync(opciones);
            Debug.Log("Lobbies encontrados: " + resultado.Results.Count);
            if (resultado.Results.Count > 0)
            {
                lobbyActual = resultado.Results[0];
                Debug.Log("Lobby encontrado: " + lobbyActual.LobbyCode);
                await UnirseALobby(lobbyActual.LobbyCode);
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
            Debug.LogError(e);
        }
    }

    private async Task IniciarRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(9);
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

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClienteConectado;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClienteConectado;

            NetworkManager.Singleton.StartHost();
            Debug.Log("Host iniciado");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void OnClienteConectado(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log("Cliente conectado: " + clientId);

        NetorkPersistence persistence = NetworkManager.Singleton.GetComponent<NetorkPersistence>();

        if (persistence == null || persistence.prefabJugador == null)
        {
            Debug.LogError("prefabJugador es null");
            return;
        }

        GameObject jugador = Instantiate(persistence.prefabJugador);
        jugador.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private async Task CrearLobby()
    {
        try
        {
            CreateLobbyOptions opcionesLobby = new CreateLobbyOptions
            {
                IsPrivate = false,
            };
            lobbyActual = await LobbyService.Instance.CreateLobbyAsync("OneShotArena", 10, opcionesLobby);
            Debug.Log("Lobby creado: " + lobbyActual.LobbyCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    private async Task UnirseALobby(string codigo)
    {
        try
        {
            Debug.Log("Intentando unirse al lobby: " + codigo);
            lobbyActual = await LobbyService.Instance.JoinLobbyByCodeAsync(codigo);
            Debug.Log("Unido al lobby: " + lobbyActual.Id);

            string codigoRelay = lobbyActual.Data["codigoRelay"].Value;
            Debug.Log("Codigo relay obtenido: " + codigoRelay);

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(codigoRelay);
            Debug.Log("Relay joined");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            Debug.Log("Cliente conectado");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}