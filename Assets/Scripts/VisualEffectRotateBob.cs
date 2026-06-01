using UnityEngine;

public class VisualEffectRotateBob : MonoBehaviour
{
    [Header("Rotación")]
    public float velocidadRotacion = 50f;
    public Vector3 ejeRotacion = Vector3.up;

    [Header("Balanceo (Bobbing)")]
    public float amplitudBob = 0.15f;
    public float frecuenciaBob = 1.5f;

    private Vector3 posicionInicial;

    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        // Rotación constante
        transform.Rotate(ejeRotacion * velocidadRotacion * Time.deltaTime);

        // Movimiento de subir y bajar (Bobbing)
        float nuevoY = posicionInicial.y + Mathf.Sin(Time.time * frecuenciaBob) * amplitudBob;
        transform.position = new Vector3(transform.position.x, nuevoY, transform.position.z);
    }
}
