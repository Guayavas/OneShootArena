using UnityEngine;
using Unity.Netcode;

public class PlayerStatusUI : NetworkBehaviour
{
    private PlayerHealth health;
    private PlayerMovement movement;
    private PlayerShoot shoot;
    private PlayerStats stats;

    [Header("Indicadores")]
    private GameObject indicadorEscudo;
    private GameObject indicadorVelocidad;
    private GameObject indicadorBonus;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        health = GetComponent<PlayerHealth>();
        if (health == null) health = GetComponentInChildren<PlayerHealth>();

        movement = GetComponent<PlayerMovement>();
        if (movement == null) movement = GetComponentInChildren<PlayerMovement>();

        shoot = GetComponent<PlayerShoot>();
        if (shoot == null) shoot = GetComponentInChildren<PlayerShoot>();

        stats = GetComponent<PlayerStats>();
        if (stats == null) stats = GetComponentInChildren<PlayerStats>();
    }

    void Update()
    {
        if (!IsOwner) return;

        ActualizarEscudo();
        ActualizarVelocidad();
        ActualizarBonus();
    }

    private void ActualizarEscudo()
    {
        if (indicadorEscudo == null)
            indicadorEscudo = GameObject.Find("IndicadorEscudo");

        if (indicadorEscudo != null && health != null)
        {
            bool activo = health.TieneEscudoActivo();
            if (indicadorEscudo.activeSelf != activo)
                indicadorEscudo.SetActive(activo);
        }
    }

    private void ActualizarVelocidad()
    {
        if (indicadorVelocidad == null)
            indicadorVelocidad = GameObject.Find("IndicadorVelocidad");

        if (indicadorVelocidad != null && movement != null)
        {
            bool activo = movement.TieneBonoVelocidad();
            if (indicadorVelocidad.activeSelf != activo)
                indicadorVelocidad.SetActive(activo);
        }
    }

    private void ActualizarBonus()
    {
        if (indicadorBonus == null)
            indicadorBonus = GameObject.Find("IndicadorBonusRecarga");

        if (indicadorBonus != null && stats != null)
        {
            bool activo = stats.bonusRecarga.Value > 1.0f;
            if (indicadorBonus.activeSelf != activo)
                indicadorBonus.SetActive(activo);
        }
    }
}
