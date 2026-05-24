using UnityEngine;
using System;
using Unity.Netcode;
using Unity.Collections;

public class PlayerStats : NetworkBehaviour
{
    public NetworkVariable<int> score = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> bonusRecarga = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> nickname = new NetworkVariable<FixedString32Bytes>("Jugador", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private const float BONUS_MIN = 1f;
    private const float BONUS_MAX = 1.3f;
    private const float BONUS_AUMENTO = 0.1f;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            nickname.Value = PlayerPrefs.GetString("Nickname", "Jugador");
            Debug.Log("Jugador local: " + nickname.Value);
        }
    }
    public void AumentarBonus()
    {
        if (!IsOwner) return;

        if (bonusRecarga.Value < BONUS_MAX)
        {
            bonusRecarga.Value += BONUS_AUMENTO;
            Debug.Log("Bonus recarga: " + bonusRecarga.Value);
        }
    }

    public void DisminuirBonus()
    {
        if (!IsOwner) return;

        bonusRecarga.Value = Mathf.Max(BONUS_MIN, (float)Math.Round(bonusRecarga.Value - BONUS_AUMENTO, 1));
        Debug.Log("Disminucion bonus recarga: " + bonusRecarga.Value);
    }

    public void SumarPunto()
    {
        if (!IsOwner) return;

        score.Value++;
        Debug.Log("Score: " + score.Value);
    }

    public void Reiniciar()
    {
        if (!IsOwner) return;

        score.Value = 0;
        bonusRecarga.Value = BONUS_MIN;
    }
}