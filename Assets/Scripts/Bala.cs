using UnityEngine;

public class Bala : MonoBehaviour
{
    [SerializeField] private float tiempoVida = 3f;
    public GameObject duenio;

    void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    void OnTriggerEnter(Collider otro)
    {
        if (otro.CompareTag("Player") && otro.gameObject != duenio)
        {
            otro.GetComponent<PlayerHealth>()?.RecibirDanio(1f);
            duenio?.GetComponent<PlayerShooter>()?.KillConfirmado();
            Destroy(gameObject);
        }
    }
}