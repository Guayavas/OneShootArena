using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float vidaMaxima = 1f;
    private float vidaActual;
    private bool tieneEscudo = false;
    private float duracionEscudo;
    private float tiempoEscudoRestante;
    private PlayerStats stats;

    public bool EscudoActivo => tieneEscudo;
    public float ProgresoEscudo => tieneEscudo ? (tiempoEscudoRestante / duracionEscudo) : 0f;

    void Start()
    {
        vidaActual = vidaMaxima;
        stats = GameObject.Find("GameManager").GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            Morir();

        if (tieneEscudo)
        {
            tiempoEscudoRestante -= Time.deltaTime;
            if (tiempoEscudoRestante <= 0f)
                DesactivarEscudo();
        }
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
        duracionEscudo = duracion;
        tiempoEscudoRestante = duracion;
    }

    void DesactivarEscudo()
    {
        tieneEscudo = false;
        tiempoEscudoRestante = 0f;
    }

    void Morir()
    {
        stats.DisminuirBonus();
        Destroy(gameObject);
    }
}