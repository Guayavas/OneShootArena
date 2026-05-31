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
                await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id, new UpdateLobbyOptions());
            }
        }
    }

    private async Task BuscarOCrearLobby()
    {
        try
        {
            int intentos = 0;
            QueryResponse resultado = null;

            while (intentos < 5)
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
                await Task.Delay(2000);
            }

            if (lobbyActual != null)
            {
                Debug.Log("Lobby encontrado y uniéndose: " + lobbyActual.Id);
                await UnirseALobby(lobbyActual.Id);
            }
            else
            {
                Debug.Log("No se encontraron partidas activas de OneShotArena, creando nueva...");
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
            // En WebGL QoS falla, así que intentamos forzar una región (us-east-1 suele ser estable)
            Allocation allocation;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                allocation = await RelayService.Instance.CreateAllocationAsync(maxJugadores - 1, "us-east-1");
            }
            else
            {
                allocation = await RelayService.Instance.CreateAllocationAsync(maxJugadores - 1);
            }
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
            // Forzamos el protocolo a RelayUnityTransport para evitar el error de IPC
            transport.Protocol = UnityTransport.ProtocolType.RelayUnityTransport;

            // Para WebGL en HTTPS (GitHub Pages), forzamos el uso de WebSockets (WSS)
            bool useWSS = Application.platform == RuntimePlatform.WebGLPlayer;

            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                null,
                useWSS
            );

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            int indexNave = PlayerPrefs.GetInt("NaveSeleccionada", 0);
            // El Host siempre tiene ClientId 0 al inicio.
            // Registramos la selección ANTES de StartHost para asegurar que esté lista.
            seleccionNaves[0] = indexNave;
            Debug.Log($"[HOST] Registrando nave local index {indexNave} para ClientId 0");

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
                await Task.Delay(1000);
                lobbyActual = await LobbyService.Instance.GetLobbyAsync(lobbyActual.Id);
                codigoRelay = lobbyActual.Data["codigoRelay"].Value;
                reintentos++;
            }

            if (codigoRelay == "0") return;

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(codigoRelay);
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            // Forzamos el protocolo a RelayUnityTransport para evitar el error de IPC
            transport.Protocol = UnityTransport.ProtocolType.RelayUnityTransport;

            // Para WebGL en HTTPS (GitHub Pages), forzamos el uso de WebSockets (WSS)
            bool useWSS = Application.platform == RuntimePlatform.WebGLPlayer;

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                useWSS
            );

            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(indexNave);
            Debug.Log("Uniéndose con Nave Index: " + indexNave);
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
        else
        {
            // Si no hay payload (ej. el Host), usamos lo que ya registramos o 0
            if (seleccionNaves.ContainsKey(request.ClientNetworkId))
                indexNave = seleccionNaves[request.ClientNetworkId];
        }

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;

        if (NetworkManager.Singleton.IsServer)
        {
            seleccionNaves[request.ClientNetworkId] = indexNave;
            Debug.Log($"[SERVER] Aprobando conexión. Cliente {request.ClientNetworkId} solicitó nave index {indexNave}");
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
        else
        {
            Debug.LogWarning($"[SERVER] No se encontró selección para Cliente {clientId}, usando nave 0 por defecto.");
        }

        NetorkPersistence persistence = FindObjectOfType<NetorkPersistence>();
        GameObject navePrefab = null;

        if (persistence != null)
        {
            navePrefab = persistence.ObtenerPrefabJugador(indexNave);
            Debug.Log($"[SERVER] Cliente {clientId} conectado. Spawneando nave index {indexNave} (Prefab: {(navePrefab != null ? navePrefab.name : "NULL")})");
        }

        if (navePrefab != null)
        {
            GameObject jugadorInstancia = Instantiate(navePrefab);
            jugadorInstancia.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }
        else
        {
            Debug.LogError("Error al spawnear nave: Prefab no encontrado en NetorkPersistence.");
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
                Debug.LogWarning("Error al cerrar lobby: " + e.Message);
            }
        }
    }
}
