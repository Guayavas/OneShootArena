using UnityEngine;

public class Bala : MonoBehaviour
{
    //No cambia nadaaaaa 
    [SerializeField] private float tiempoVida = 3f;

    void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    void OnTriggerEnter(Collider otro)
    {
        if (otro.CompareTag("Jugador"))
        {
            Destroy(gameObject);
        }
    }
}