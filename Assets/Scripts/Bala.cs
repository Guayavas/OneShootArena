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
            NetworkObject no = otro.GetComponent<NetworkObject>();
            if (no != null && no.OwnerClientId != duenioId.Value)
            {
                otro.GetComponent<PlayerHealth>()?.RecibirDanio(1f);

                // Buscar al dueño para confirmar la kill
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(duenioId.Value, out var client))
                {
                    client.PlayerObject.GetComponent<PlayerShoot>()?.KillConfirmado();
                }

                DespawnBala();
            }
        }
    }
}