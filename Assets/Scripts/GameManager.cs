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

    // Diccionario para recordar qué nave eligió cada cliente
    private System.Collections.Generic.Dictionary<ulong, int> seleccionNaves = new System.Collections.Generic.Dictionary<ulong, int>();

    void Awake()
    {
        // Forzamos la configuración desde el primer milisegundo
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
                Debug.Log("Conectado con ID: " + AuthenticationService.Instance.PlayerId);
            }

            //await BuscarOCrearLobby();
            await UnirseALobby("HS89qTomBsMm9UamsP3shS");

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
            // Intentar buscar varias veces con un pequeño retraso por si el servidor de Unity tarda en propagar
            int intentos = 0;
            QueryResponse resultado = null;

            while (intentos < 3)
            {
                QueryLobbiesOptions opciones = new QueryLobbiesOptions
                {
                    Count = 1,
                    Filters = new System.Collections.Generic.List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.IsLocked, "0", QueryFilter.OpOptions.EQ),
                        // Filtrar por nombre para ser más específicos
                        new QueryFilter(QueryFilter.FieldOptions.Name, "OneShotArena", QueryFilter.OpOptions.EQ)
                    }
                };

                resultado = await LobbyService.Instance.QueryLobbiesAsync(opciones);
                if (resultado.Results.Count > 0) break;

                intentos++;
                if (intentos < 3) await Task.Delay(1500);
            }

            if (resultado != null && resultado.Results.Count > 0)
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

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;

            Debug.Log("Iniciando Host. ConnectionApproval: " + NetworkManager.Singleton.NetworkConfig.ConnectionApproval);

            // Limpiamos suscripciones anteriores para evitar duplicados
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Registrar la selección del host localmente
            int indexNave = PlayerPrefs.GetInt("NaveSeleccionada", 0);
            seleccionNaves[NetworkManager.Singleton.LocalClientId] = indexNave;

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

            // IMPORTANTE: El cliente también debe tener ConnectionApproval habilitado
            // en su configuración para que el mensaje de conexión coincida (mismo tamaño/formato)
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            Debug.Log("Iniciando Cliente. ConnectionApproval: " + NetworkManager.Singleton.NetworkConfig.ConnectionApproval);

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
        // IMPORTANTE: Ponemos en false para spawnear nosotros manualmente la nave correcta
        response.CreatePlayerObject = false;
        response.Pending = false;

        // Guardamos la selección para cuando OnClientConnected se dispare
        if (NetworkManager.Singleton.IsServer)
        {
            seleccionNaves[request.ClientNetworkId] = indexNave;
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

        if (prefabsNaves != null && indexNave >= 0 && indexNave < prefabsNaves.Length)
        {
            GameObject navePrefab = prefabsNaves[indexNave];
            GameObject jugadorInstancia = Instantiate(navePrefab);

            // Asignamos la nave como el Player Object oficial de este cliente
            jugadorInstancia.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }
        else
        {
            Debug.LogError("No se pudo spawnear la nave: índice inválido o prefabs no asignados.");
        }
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