using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float vidaMaxima = 1f;
    private float vidaActual;
    private bool tieneEscudo = false;
    public GameObject gameManagerObj;
    public GameManager gameManager;


    void Start()
    {
        vidaActual = vidaMaxima;
        gameManagerObj = GameObject.Find("GameManager");
        gameManager = gameManagerObj.GetComponent<GameManager>();

    }

    public void RecibirDanio(float danio)
    {
        if (tieneEscudo) return;
        vidaActual -= danio;
        if (vidaActual <= 0)
        {
            Morir();
            gameManager.BonusRecarga(1);
        } 
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