using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float vidaMaxima = 1f;
    private float vidaActual;
    private bool tieneEscudo = false;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDanio(float danio)
    {
        if (tieneEscudo) return;
        vidaActual -= danio;
        if (vidaActual <= 0) Morir();
    }

    public void ActivarEscudo(float duracion)
    {
        tieneEscudo = true;
        Invoke(nameof(DesactivarEscudo), duracion);
    }

    void DesactivarEscudo()
    {
        tieneEscudo = false;
    }

    void Morir()
    {
        Destroy(gameObject);
    }
}