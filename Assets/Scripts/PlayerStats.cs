using UnityEngine;
using System;
using Unity.Netcode;
using Unity.Collections;

public class PlayerStats : NetworkBehaviour
{
    // NetworkVariables con escritura exclusiva del servidor para mayor seguridad
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> bonusRecarga = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> nickname = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Jugador"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private const float BONUS_MIN = 1f;
    private const float BONUS_MAX = 2.0f; // Bonus máximo de recarga (doble velocidad)
    private const float BONUS_PASO = 0.1f;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string nickLocal = PlayerPrefs.GetString("Nickname", "Jugador");
            SetNicknameServerRpc(nickLocal);
        }
    }

    [ServerRpc]
    public void SetNicknameServerRpc(string nuevoNick)
    {
        nickname.Value = nuevoNick;
    }

    public void AumentarBonus()
    {
        if (!IsServer) return;
        bonusRecarga.Value = Mathf.Min(BONUS_MAX, bonusRecarga.Value + BONUS_PASO);
        Debug.Log($"Servidor: Bonus aumentado a {bonusRecarga.Value} para {OwnerClientId}");
    }

    public void DisminuirBonus()
    {
        if (!IsServer) return;
        bonusRecarga.Value = Mathf.Max(BONUS_MIN, bonusRecarga.Value - BONUS_PASO);
    }

    public void SumarPunto()
    {
        if (!IsServer) return;
        score.Value++;
    }

    public void SumarPuntos(int cantidad)
    {
        if (!IsServer) return;
        score.Value += cantidad;
    }

    public void Reiniciar()
    {
        if (!IsServer) return;
        score.Value = 0;
        bonusRecarga.Value = BONUS_MIN;
    }
}
