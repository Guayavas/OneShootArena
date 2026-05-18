using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public int score = 0;
    public float bonusRecarga = 1f;

    private const float BONUS_MIN = 1f;
    private const float BONUS_MAX = 1.3f;
    private const float BONUS_AUMENTO = 0.1f;

    public void AumentarBonus()
    {
        if (bonusRecarga <= 1.3f)
        {
            bonusRecarga += 0.1f;
            Debug.Log("Bonus recarga: " + bonusRecarga);
        }
        else
        {
            Debug.Log("No entro");
        }
       
    }

    public void DisminuirBonus()
    {
        bonusRecarga = Mathf.Max(BONUS_MIN, (float)Math.Round(bonusRecarga - BONUS_AUMENTO, 1));
        Debug.Log("Disminucion bonus recarga: " + bonusRecarga);
    }

    public void SumarPunto()
    {
        score++;
        Debug.Log("Score: " + score);
    }

    public void Reiniciar()
    {
        score = 0;
        bonusRecarga = BONUS_MIN;
    }
}