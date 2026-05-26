using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public int score = 0;
    public float bonusRecarga = 1f;
    private const float BONUS_MIN = 1f;
    private const float BONUS_MAX = 1.3f;
    private const float BONUS_AUMENTO = 0.1f;
    public string nickname;

    void Start()
    {
        nickname = PlayerPrefs.GetString("Nickname", "Jugador");
    }

    public void AumentarBonus()
    {
        if (bonusRecarga <= BONUS_MAX)
            bonusRecarga += BONUS_AUMENTO;
    }

    public void DisminuirBonus()
    {
        bonusRecarga = Mathf.Max(BONUS_MIN, (float)Math.Round(bonusRecarga - BONUS_AUMENTO, 1));
    }

    public void SumarPunto()
    {
        score++;
    }

    public void Reiniciar()
    {
        score = 0;
        bonusRecarga = BONUS_MIN;
    }
}