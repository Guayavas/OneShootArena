using UnityEngine;
using UnityEngine.UI;

public class IndicadorPowerUps : MonoBehaviour
{
    private Image fillEscudo;
    private Image fillVelocidad;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;

    void Start()
    {
        Transform escudoT    = transform.Find("escudo_Image");
        Transform velocidadT = transform.Find("velocidad_Image");

        if (escudoT != null)
            fillEscudo = escudoT.GetComponent<Image>();

        if (velocidadT != null)
            fillVelocidad = velocidadT.GetComponent<Image>();

        SetFill(fillEscudo, 0f);
        SetFill(fillVelocidad, 0f);
    }

    void Update()
    {
        if (playerHealth == null || playerMovement == null)
            BuscarJugador();

        SetFill(fillEscudo,    playerHealth   != null ? playerHealth.ProgresoEscudo   : 0f);
        SetFill(fillVelocidad, playerMovement != null ? playerMovement.ProgresoBoost  : 0f);
    }

    void BuscarJugador()
    {
        PlayerHealth h = FindObjectOfType<PlayerHealth>();
        if (h != null)
        {
            playerHealth   = h;
            playerMovement = h.GetComponent<PlayerMovement>();
        }
    }

    void SetFill(Image img, float valor)
    {
        if (img == null) return;
        img.fillAmount = valor;
        img.gameObject.SetActive(valor > 0f);
    }
}