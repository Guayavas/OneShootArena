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

    // Diccionario para recordar qué nave eligió cada cliente
    private System.Collections.Generic.Dictionary<ulong, int> seleccionNaves = new System.Collections.Generic.Dictionary<ulong, int>();

    void Awake()
    {
        Debug.Log("[LOG] GameManager Awake iniciado");
        // Forzamos la configuración desde el primer milisegundo
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            Debug.Log("[LOG] ConnectionApproval activado en NetworkManager");
        }
        else
        {
            Debug.LogError("[LOG] NetworkManager.Singleton es NULL en Awake");
        }
    }

    async void Start()
    {
        Debug.Log("[LOG] GameManager Start iniciado");
        try
        {
            Debug.Log("[LOG] Inicializando Unity Services...");
            await UnityServices.InitializeAsync();
            Debug.Log("[LOG] Unity Services inicializados correctamente");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[LOG] Iniciando sesión anónima...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[LOG] Sesión iniciada. PlayerId: " + AuthenticationService.Instance.PlayerId);
            }
            else
            {
                Debug.Log("[LOG] Ya había una sesión iniciada. PlayerId: " + AuthenticationService.Instance.PlayerId);
            }

            Debug.Log("[LOG] Iniciando búsqueda o creación de lobby...");
            await BuscarOCrearLobby();
        }
        catch (Exception e)
        {
            Debug.LogError("[LOG] Error crítico en Inicialización: " + e.Message);
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
        Debug.Log("[LOG] BuscarOCrearLobby: Iniciando búsqueda...");
        try
        {
            int intentos = 0;
            QueryResponse resultado = null;

            while (intentos < 5)
            {
                Debug.Log($"[LOG] BuscarOCrearLobby: Intento {intentos + 1} de 5...");
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
                Debug.Log($"[LOG] BuscarOCrearLobby: Se encontraron {resultado.Results.Count} lobbies");

                if (resultado.Results.Count > 0)
                {
                    foreach (var lobby in resultado.Results)
                    {
                        if (lobby.Name == "OneShotArena")
                        {
                            lobbyActual = lobby;
                            Debug.Log("[LOG] BuscarOCrearLobby: Lobby 'OneShotArena' identificado");
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
                Debug.Log("[LOG] BuscarOCrearLobby: Lobby encontrado. Uniendo...");
                await UnirseALobby(lobbyActual.Id);
            }
            else
            {
                Debug.Log("[LOG] BuscarOCrearLobby: No se encontró lobby. Creando uno nuevo...");
                await CrearLobby();
                await IniciarRelay();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[LOG] Error en BuscarOCrearLobby: " + e.Message);
            await CrearLobby();
            await IniciarRelay();
        }
    }

    private async Task IniciarRelay()
    {
        Debug.Log("[LOG] IniciarRelay: Empezando flujo de Host...");
        try
        {
            Debug.Log("[LOG] IniciarRelay: Creando Allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxJugadores - 1);
            Debug.Log("[LOG] IniciarRelay: Obteniendo Join Code...");
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("[LOG] IniciarRelay: Join Code obtenido: " + joinCode);

            Debug.Log("[LOG] IniciarRelay: Actualizando Lobby con el Join Code...");
            await LobbyService.Instance.UpdateLobbyAsync(lobbyActual.Id, new UpdateLobbyOptions
            {
                Data = new System.Collections.Generic.Dictionary<string, DataObject>
                {
                    { "codigoRelay", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            });

            Debug.Log("[LOG] IniciarRelay: Configurando UnityTransport...");
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[LOG] IniciarRelay: No se encontró componente UnityTransport en NetworkManager");
                return;
            }

            // Para WebGL en HTTPS (GitHub Pages), forzamos el uso de WebSockets (WSS)
            string connectionType = Application.platform == RuntimePlatform.WebGLPlayer ? "wss" : "udp";
            Debug.Log("[LOG] IniciarRelay: Plataforma detectada: " + Application.platform + ". Usando conexión: " + connectionType);

            var relayServerData = new RelayServerData(allocation, connectionType);
            transport.SetRelayServerData(relayServerData);

            Debug.Log("[LOG] IniciarRelay: SetRelayServerData (RelayServerData) ejecutado");

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            int indexNave = PlayerPrefs.GetInt("NaveSeleccionada", 0);
            seleccionNaves[0] = indexNave;
            Debug.Log($"[LOG] IniciarRelay: Registrando nave local index {indexNave} para ClientId 0");

            Debug.Log("[LOG] IniciarRelay: Llamando a StartHost()...");
            bool hostStarted = NetworkManager.Singleton.StartHost();
            Debug.Log("[LOG] IniciarRelay: StartHost() retornó: " + hostStarted);
        }
        catch (Exception e)
        {
            Debug.LogError("[LOG] Error en IniciarRelay: " + e.Message);
        }
    }

    private async Task CrearLobby()
    {
        Debug.Log("[LOG] CrearLobby: Iniciando...");
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
            Debug.Log("[LOG] CrearLobby: Éxito. Lobby ID: " + lobbyActual.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("[LOG] Error al crear lobby: " + e.Message);
        }
    }

    private async Task UnirseALobby(string lobbyId)
    {
        Debug.Log("[LOG] UnirseALobby: Iniciando flujo de Cliente...");
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

            Debug.Log("[LOG] UnirseALobby: Uniéndose a Lobby ID: " + lobbyId);
            lobbyActual = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, opciones);
            string codigoRelay = lobbyActual.Data.ContainsKey("codigoRelay") ? lobbyActual.Data["codigoRelay"].Value : "0";

            int reintentos = 0;
            while (codigoRelay == "0" && reintentos < 10)
            {
                Debug.Log("[LOG] UnirseALobby: Esperando código de Relay... Intento " + (reintentos + 1));
                await Task.Delay(1000);
                lobbyActual = await LobbyService.Instance.GetLobbyAsync(lobbyActual.Id);
                codigoRelay = lobbyActual.Data["codigoRelay"].Value;
                reintentos++;
            }

            if (codigoRelay == "0")
            {
                Debug.LogError("[LOG] UnirseALobby: No se obtuvo código de Relay tras reintentos");
                return;
            }

            Debug.Log("[LOG] UnirseALobby: JoinAllocation con código: " + codigoRelay);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(codigoRelay);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("[LOG] UnirseALobby: No se encontró componente UnityTransport");
                return;
            }

            // Para WebGL en HTTPS (GitHub Pages), forzamos el uso de WebSockets (WSS)
            string connectionType = Application.platform == RuntimePlatform.WebGLPlayer ? "wss" : "udp";
            Debug.Log("[LOG] UnirseALobby: Plataforma detectada: " + Application.platform + ". Usando conexión: " + connectionType);

            var relayServerData = new RelayServerData(joinAllocation, connectionType);
            transport.SetRelayServerData(relayServerData);

            Debug.Log("[LOG] UnirseALobby: SetRelayServerData (RelayServerData) ejecutado");

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.BitConverter.GetBytes(indexNave);

            Debug.Log("[LOG] UnirseALobby: Llamando a StartClient()...");
            bool clientStarted = NetworkManager.Singleton.StartClient();
            Debug.Log("[LOG] UnirseALobby: StartClient() retornó: " + clientStarted);
        }
        catch (Exception e)
        {
            Debug.LogError("[LOG] Error al unirse: " + e.Message);
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
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[LOG] OnClientConnected: Cliente local {clientId} conectado al servidor");
            return;
        }

        Debug.Log($"[LOG] OnClientConnected: Servidor detectó conexión de Cliente {clientId}");

        int indexNave = 0;
        if (seleccionNaves.TryGetValue(clientId, out int seleccion))
        {
            indexNave = seleccion;
            Debug.Log($"[LOG] OnClientConnected: Nave recuperada para Cliente {clientId}: {indexNave}");
        }
        else
        {
            Debug.LogWarning($"[LOG] OnClientConnected: [SERVER] No se encontró selección para Cliente {clientId}, usando nave 0 por defecto.");
        }

        NetorkPersistence persistence = FindObjectOfType<NetorkPersistence>();
        GameObject navePrefab = null;

        if (persistence != null)
        {
            navePrefab = persistence.ObtenerPrefabJugador(indexNave);
            Debug.Log($"[LOG] OnClientConnected: [SERVER] Cliente {clientId} conectado. Prefab a spawnear: {(navePrefab != null ? navePrefab.name : "NULL")}");
        }
        else
        {
            Debug.LogError("[LOG] OnClientConnected: NetorkPersistence no encontrado en la escena");
        }

        if (navePrefab != null)
        {
            Debug.Log($"[LOG] OnClientConnected: Instanciando nave para Cliente {clientId}...");
            GameObject jugadorInstancia = Instantiate(navePrefab);
            Debug.Log($"[LOG] OnClientConnected: Llamando a SpawnAsPlayerObject para Cliente {clientId}...");
            jugadorInstancia.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            Debug.Log($"[LOG] OnClientConnected: Spawn finalizado para Cliente {clientId}");
        }
        else
        {
            Debug.LogError("[LOG] OnClientConnected: Error al spawnear nave: Prefab no encontrado.");
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
