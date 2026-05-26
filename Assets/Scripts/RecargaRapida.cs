using UnityEngine;

public class RecargaRapida : MonoBehaviour
{
    [SerializeField] private float reduccionTiempo = 0.5f;
    [SerializeField] private float velocidadRotacion = 90f;

    void Update()
    {
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime);
    }

    void OnTriggerEnter(Collider otro)
    {
        PlayerShooter shooter = otro.GetComponent<PlayerShooter>();
        if (shooter == null)
            shooter = otro.GetComponentInChildren<PlayerShooter>();
        if (shooter == null) return;

        shooter.AplicarReduccionRecarga(reduccionTiempo);
        Destroy(gameObject);
    }
}