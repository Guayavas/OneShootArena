using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private float velocidadBala = 20f;
    [SerializeField] private float tiempoRecarga = 3f;
    [SerializeField] private Image iconoRecarga;
    [SerializeField] private float bonusRecarga = 1;
    public GameObject gameManagerObj;
    public GameManager gameManager;

    private float tiempoTranscurrido;
    private bool puedoDisparar = true;

    void Start()
    {
        iconoRecarga = GameObject.Find("IndicadorRecarga").GetComponent<Image>();
        iconoRecarga.fillAmount = 1f;
        //Plantear la idea de update para el icono
        tiempoTranscurrido = tiempoRecarga;
        gameManagerObj = GameObject.Find("GameManager");
        gameManager = gameManagerObj.GetComponent<GameManager>();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && puedoDisparar)
        {
            Disparar();
        }
        iconoRecarga.fillAmount = tiempoTranscurrido / tiempoRecarga * gameManager.bonusRecargaActivo;
    }
    
    void Disparar()
    {
        puedoDisparar = false;
        tiempoTranscurrido = 0f;
        GameObject bala = Instantiate(prefabBala, puntoDisparo.position, Quaternion.Euler(90f, 0f, 0f));
        bala.GetComponent<Rigidbody>().velocity = transform.forward * velocidadBala;
        StartCoroutine(Recargar());
    }

    IEnumerator Recargar()
    {
        while (tiempoTranscurrido < tiempoRecarga * gameManager.bonusRecargaActivo)
        {
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        puedoDisparar = true;
    }
}