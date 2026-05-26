using UnityEngine;

public class CajaSuministros : MonoBehaviour
{
    [SerializeField] private int puntosExtra = 3;
    [SerializeField] private float radioDeteccion = 1.5f;
    [SerializeField] private float velocidadRotacion = 90f;

    private bool recogida = false;

    void Update()
    {
        if (recogida) return;

        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime);

        Collider[] cercanos = Physics.OverlapSphere(transform.position, radioDeteccion);
        foreach (Collider c in cercanos)
        {
            PlayerStats stats = c.GetComponent<PlayerStats>();
            if (stats == null)
                stats = c.GetComponentInParent<PlayerStats>();
            if (stats == null) continue;

            recogida = true;
            for (int i = 0; i < puntosExtra; i++)
                stats.SumarPunto();
            Destroy(gameObject);
            break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, radioDeteccion);
    }
}
