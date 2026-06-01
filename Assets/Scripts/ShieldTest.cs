using Unity.Netcode;
using UnityEngine;

public class ShieldTest : NetworkBehaviour
{
    public float intervaloDisparo = 2f;
    private float cronometro;

    void Update()
    {
        if (!IsServer) return;

        cronometro += Time.deltaTime;
        if (cronometro >= intervaloDisparo)
        {
            cronometro = 0f;
            DispararRayo();
        }
    }

    private void DispararRayo()
    {
        // Dispara un rayo hacia adelante para detectar jugadores
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 50f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                PlayerHealth health = hit.collider.GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    Debug.Log("ShieldTest: Impactando jugador para probar escudo.");
                    health.RecibirDanio(1f);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 50f);
    }
}
