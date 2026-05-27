using UnityEngine;
using System;
using Unity.Netcode;
using Unity.Collections;

public class PlayerStats : NetworkBehaviour
{
    // Cambiamos a Server-Write Only para evitar errores de NGO en WebGL/Red
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> bonusRecarga = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> nickname = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Jugador"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private const float BONUS_MIN = 1f;
    private const float BONUS_MAX = 1.3f;
    private const float BONUS_AUMENTO = 0.1f;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string nickLocal = PlayerPrefs.GetString("Nickname", "Jugador");
            SetNicknameServerRpc(nickLocal);
            Debug.Log("Jugador local: " + nickLocal);
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

        if (bonusRecarga.Value < BONUS_MAX)
        {
            bonusRecarga.Value += BONUS_AUMENTO;
            Debug.Log("Bonus recarga: " + bonusRecarga.Value);
        }
    }

    public void DisminuirBonus()
    {
        if (!IsServer) return;

        bonusRecarga.Value = Mathf.Max(BONUS_MIN, (float)Math.Round(bonusRecarga.Value - BONUS_AUMENTO, 1));
        Debug.Log("Disminucion bonus recarga: " + bonusRecarga.Value);
    }

    public void SumarPunto()
    {
        if (!IsServer) return;

        score.Value++;
        Debug.Log("Score: " + score.Value);
    }

    public void Reiniciar()
    {
        if (!IsServer) return;

        score.Value = 0;
        bonusRecarga.Value = BONUS_MIN;
    }
}