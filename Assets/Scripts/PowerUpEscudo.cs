using UnityEngine;

public class PowerUpEscudo : MonoBehaviour
{
    [SerializeField] private float duracionEscudo = 5f;
    [SerializeField] private float velocidadRotacion = 90f;
    [SerializeField] private float radioDeteccion = 1.5f;

    private bool recogido = false;

    void Update()
    {
        if (recogido) return;

        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime);

        Collider[] cercanos = Physics.OverlapSphere(transform.position, radioDeteccion);
        foreach (Collider c in cercanos)
        {
            PlayerHealth health = c.GetComponent<PlayerHealth>();
            if (health == null)
                health = c.GetComponentInParent<PlayerHealth>();
            if (health == null) continue;

            recogido = true;
            health.ActivarEscudo(duracionEscudo);
            Destroy(gameObject);
            break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, radioDeteccion);
    }
}
