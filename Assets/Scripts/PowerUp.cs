using Unity.Netcode;
using UnityEngine;

public class PowerUp : NetworkBehaviour
{
    public enum TipoPowerUp { Escudo, Velocidad, Recarga }
    public TipoPowerUp tipo;

    [Header("Ajustes")]
    public float duracion = 5f;
    public float multiplicadorVelocidad = 1.5f;
    public float reduccionRecarga = 0.5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null)
        {
            AplicarEfecto(other.gameObject);
            // El objeto se destruye en el servidor y se sincroniza
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void AplicarEfecto(GameObject jugador)
    {
        // Buscamos componentes en el objeto o sus padres
        PlayerHealth health = jugador.GetComponentInParent<PlayerHealth>();
        PlayerMovement movement = jugador.GetComponentInParent<PlayerMovement>();
        PlayerShoot shoot = jugador.GetComponentInParent<PlayerShoot>();

        switch (tipo)
        {
            case TipoPowerUp.Escudo:
                if (health != null) health.ActivarEscudo(duracion);
                break;
            case TipoPowerUp.Velocidad:
                if (movement != null) movement.AplicarBonoVelocidad(multiplicadorVelocidad, duracion);
                break;
            case TipoPowerUp.Recarga:
                if (shoot != null) shoot.ReducirTiempoRecarga(reduccionRecarga);
                break;
        }

        Debug.Log($"PowerUp {tipo} aplicado a {jugador.name}");
    }
}
