using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float vidaMaxima = 1f;
    private NetworkVariable<float> vidaActual = new NetworkVariable<float>();
    private bool tieneEscudo = false;
    private PlayerStats stats;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            vidaActual.Value = vidaMaxima;
        }
        AsignarStats();
    }

    private void AsignarStats()
    {
        if (stats != null) return;

        stats = GetComponentInParent<PlayerStats>();

        if (stats == null)
        {
            GameObject gm = GameObject.Find("GameManager");
            if (gm != null) stats = gm.GetComponent<PlayerStats>();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // testear muerte
        if (Input.GetKeyDown(KeyCode.K))
            MorirServerRpc();
    }

    public void RecibirDanio(float danio)
    {
        if (!IsServer) return;
        if (tieneEscudo) return;

        vidaActual.Value -= danio;
        if (vidaActual.Value <= 0) Morir();
    }

    [ServerRpc]
    void MorirServerRpc()
    {
        Morir();
    }

    void Morir()
    {
        NotificarMuerteClientRpc();

        // Desactivar visualmente o mover a posición de respawn
        ManejarRespawn();
    }

    [ClientRpc]
    void NotificarMuerteClientRpc()
    {
        if (IsOwner)
        {
            AsignarStats();
            if (stats != null)
            {
                stats.DisminuirBonus();
            }
            else
            {
                Debug.LogError("NotificarMuerteClientRpc: stats es NULO en el dueño");
            }
        }
    }

    private void ManejarRespawn()
    {
        // Posiciones aleatorias de respawn (ejemplo)
        Vector3[] posiciones = new Vector3[] {
            new Vector3(0,0,0), new Vector3(10,0,10), new Vector3(-10,0,10),
            new Vector3(10,0,-10), new Vector3(-10,0,-10)
        };

        transform.position = posiciones[UnityEngine.Random.Range(0, posiciones.Length)];

        // Resetear físicas para evitar bugs de velocidad al reaparecer
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (IsServer)
        {
            vidaActual.Value = vidaMaxima;
        }
    }

    public void ActivarEscudo(float duracion)
    {
        tieneEscudo = true;
        Invoke(nameof(DesactivarEscudo), duracion);
    }

    void DesactivarEscudo()
    {
        tieneEscudo = false;
    }
}