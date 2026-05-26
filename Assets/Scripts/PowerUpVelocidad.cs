using UnityEngine;

public class PowerUpVelocidad : MonoBehaviour
{
    [SerializeField] private float multiplicador = 2f;
    [SerializeField] private float duracion = 5f;
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
            PlayerMovement mov = c.GetComponent<PlayerMovement>();
            if (mov == null)
                mov = c.GetComponentInParent<PlayerMovement>();
            if (mov == null) continue;

            recogido = true;
            mov.ActivarBoostVelocidad(multiplicador, duracion);
            Destroy(gameObject);
            break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, radioDeteccion);
    }
}
