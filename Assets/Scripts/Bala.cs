using Unity.Netcode;
using UnityEngine;

public class Bala : NetworkBehaviour
{
    [SerializeField] private float tiempoVida = 3f;
    public NetworkVariable<ulong> duenioId = new NetworkVariable<ulong>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Invoke(nameof(DespawnBala), tiempoVida);
        }
    }

    private void DespawnBala()
    {
        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }

    void OnTriggerEnter(Collider otro)
    {
        if (!IsServer) return;

        if (otro.CompareTag("Player"))
        {
            // Intentar obtener NetworkObject del objeto tocado o sus padres
            NetworkObject no = otro.GetComponentInParent<NetworkObject>();
            if (no != null && no.OwnerClientId != duenioId.Value)
            {
                // El daño se aplica al PlayerHealth, que puede estar en el mismo objeto, padre o hijo
                PlayerHealth health = no.GetComponentInChildren<PlayerHealth>();
                if (health == null) health = no.GetComponentInParent<PlayerHealth>();

                if (health != null) health.RecibirDanio(1f);

                // Buscar al dueño para confirmar la kill
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(duenioId.Value, out var client))
                {
                    if (client.PlayerObject != null)
                    {
                        PlayerShoot shooter = client.PlayerObject.GetComponentInChildren<PlayerShoot>();
                        if (shooter != null) shooter.KillConfirmado();
                    }
                }

                DespawnBala();
            }
        }
    }
}