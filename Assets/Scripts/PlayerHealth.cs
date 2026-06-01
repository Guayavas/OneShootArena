using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float vidaMaxima = 1f;
    private NetworkVariable<float> vidaActual = new NetworkVariable<float>();
    private bool tieneEscudo = false;
    public GameObject visualEscudo; // Asignar esfera visual en Inspector
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
        stats = GetComponent<PlayerStats>();
        if (stats == null) stats = GetComponentInParent<PlayerStats>();
        if (stats == null && NetworkObject != null)
            stats = NetworkObject.GetComponent<PlayerStats>();
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

        if (tieneEscudo)
        {
            DesactivarEscudo();
            Debug.Log("Escudo absorbio el danio.");
            return;
        }

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
        if (!IsServer) return;

        AsignarStats();
        if (stats != null) stats.DisminuirBonus();

        NotificarMuerteClientRpc();

        // Desactivar visualmente o mover a posición de respawn
        ManejarRespawn();
    }

    [ClientRpc]
    void NotificarMuerteClientRpc()
    {
        if (IsOwner)
        {
            Debug.Log("Has muerto. Reseteando bonus.");
        }
    }

    private void ManejarRespawn()
    {
        float correctY = 0.1023054f;
        // Posiciones aleatorias de respawn (ejemplo)
        Vector3[] posiciones = new Vector3[] {
            new Vector3(0,correctY,0), new Vector3(10,correctY,10), new Vector3(-10,correctY,10),
            new Vector3(10,correctY,-10), new Vector3(-10,correctY,-10)
        };

        Vector3 nuevaPos = posiciones[UnityEngine.Random.Range(0, posiciones.Length)];

        // En Netcode, el servidor debe mover el objeto
        if (IsServer)
        {
            transform.position = nuevaPos;
            vidaActual.Value = vidaMaxima;

            // Resetear físicas en el servidor
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
                rb.WakeUp();
            }

            // Forzar actualización de posición a los clientes
            RpcMoverNaveClientRpc(nuevaPos);
        }
    }

    [ClientRpc]
    private void RpcMoverNaveClientRpc(Vector3 pos)
    {
        transform.position = pos;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ActivarEscudo(float duracion)
    {
        if (!IsServer) return;

        tieneEscudo = true;
        ActivarVisualEscudoClientRpc(true);
        CancelInvoke(nameof(DesactivarEscudo));
        Invoke(nameof(DesactivarEscudo), duracion);
    }

    void DesactivarEscudo()
    {
        if (!IsServer) return;

        tieneEscudo = false;
        ActivarVisualEscudoClientRpc(false);
        CancelInvoke(nameof(DesactivarEscudo));
    }

    [ClientRpc]
    private void ActivarVisualEscudoClientRpc(bool activado)
    {
        if (visualEscudo != null)
            visualEscudo.SetActive(activado);
    }

    public bool TieneEscudoActivo()
    {
        return tieneEscudo;
    }
}
