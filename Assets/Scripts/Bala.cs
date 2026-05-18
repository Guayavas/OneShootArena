using Unity.VisualScripting;
using UnityEngine;

public class Bala : MonoBehaviour
{
    [SerializeField] private float tiempoVida = 3f;
    [SerializeField] private float darBonusRecarga;

    public GameObject gameManagerObj;
    public GameManager gameManager;
    void Start()
    {
        Destroy(gameObject, tiempoVida);
        gameManagerObj = GameObject.Find("GameManager");
        gameManager = gameManagerObj.GetComponent<GameManager>();   
    }

    void OnTriggerEnter(Collider otro)
    {
        if (otro.CompareTag("Player"))
        {
            gameManager.BonusRecarga(0);
            otro.GetComponent<PlayerHealth>()?.RecibirDanio(1f);
        }
    }
    
}