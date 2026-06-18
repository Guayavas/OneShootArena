using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 15f, -10f);
    [SerializeField] private float suavizado = 5f;

    private Transform objetivo;
    private Quaternion rotacionInicial;

    void Start()
    {
        rotacionInicial = transform.rotation;
    }

    public void SetObjetivo(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        Vector3 posicionObjetivo = objetivo.position + offset;
        transform.position = Vector3.Lerp(transform.position, posicionObjetivo, suavizado * Time.deltaTime);
        transform.rotation = rotacionInicial;
    }
}
