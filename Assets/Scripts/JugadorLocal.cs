using Unity.Netcode;
using UnityEngine;

public class JugadorLocal : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        Camera.main.GetComponent<CamaraSeguimiento>().SetObjetivo(transform);
    }
}