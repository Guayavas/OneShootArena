using Unity.Netcode;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    public enum TipoPowerUp { Escudo, Velocidad, Recarga, Suministro, PickupRecarga }
    public TipoPowerUp tipo;

    [Header("Ajustes")]
    public float duracion = 5f;
    public float multiplicadorVelocidad = 1.5f;
    public int puntosSuministro = 50;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Comprobamos si es un jugador buscando el componente PlayerMovement
        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            AplicarEfecto(movement.gameObject);
            // El objeto se destruye en el servidor y se sincroniza
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void AplicarEfecto(GameObject jugador)
    {
        // Búsqueda robusta: buscamos en el objeto tocado, en sus padres y en sus hijos.
        PlayerHealth health = jugador.GetComponentInChildren<PlayerHealth>();
        if (health == null) health = jugador.GetComponentInParent<PlayerHealth>();

        PlayerMovement movement = jugador.GetComponentInChildren<PlayerMovement>();
        if (movement == null) movement = jugador.GetComponentInParent<PlayerMovement>();

        PlayerShoot shoot = jugador.GetComponentInChildren<PlayerShoot>();
        if (shoot == null) shoot = jugador.GetComponentInParent<PlayerShoot>();

        PlayerStats stats = jugador.GetComponentInChildren<PlayerStats>();
        if (stats == null) stats = jugador.GetComponentInParent<PlayerStats>();

        switch (tipo)
        {
            case TipoPowerUp.Escudo:
                if (health != null) health.ActivarEscudo(duracion);
                break;
            case TipoPowerUp.Velocidad:
                if (movement != null) movement.AplicarBonoVelocidad(multiplicadorVelocidad, duracion);
                break;
            case TipoPowerUp.Recarga:
                // El power-up de recarga resetea el cooldown inmediatamente
                if (shoot != null) shoot.ResetearCooldown();
                break;
            case TipoPowerUp.Suministro:
                if (stats != null) stats.SumarPuntos(puntosSuministro);
                break;
            case TipoPowerUp.PickupRecarga:
                // El pickup pequeño reduce 0.1s el cooldown actual
                if (shoot != null) shoot.ReducirTiempoRecargaTemporal(0.1f);
                break;
        }

        Debug.Log($"PowerUp {tipo} aplicado a {jugador.name}");
    }
}
