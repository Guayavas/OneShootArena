using Unity.Netcode;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    public enum TipoPowerUp { Escudo, Velocidad, Recarga, Suministro }
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
        // El objeto 'jugador' suele ser el root o tener los componentes
        PlayerHealth health = jugador.GetComponent<PlayerHealth>();
        PlayerMovement movement = jugador.GetComponent<PlayerMovement>();
        PlayerShoot shoot = jugador.GetComponent<PlayerShoot>();
        PlayerStats stats = jugador.GetComponent<PlayerStats>();

        // Si no están en el objeto tocado, buscamos en el root
        if (health == null) health = jugador.GetComponentInParent<PlayerHealth>();
        if (movement == null) movement = jugador.GetComponentInParent<PlayerMovement>();
        if (shoot == null) shoot = jugador.GetComponentInParent<PlayerShoot>();
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
                if (shoot != null) shoot.ReducirTiempoRecarga();
                break;
            case TipoPowerUp.Suministro:
                if (stats != null) stats.SumarPuntos(puntosSuministro);
                break;
        }

        Debug.Log($"PowerUp {tipo} aplicado a {jugador.name}");
    }
}
