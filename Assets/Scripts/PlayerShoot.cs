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
    private float tiempoTranscurrido;
    private bool puedoDisparar = true;

    void Start()
    {
        iconoRecarga = GameObject.Find("IndicadorRecarga").GetComponent<Image>();
        iconoRecarga.fillAmount = 1f;
        tiempoTranscurrido = tiempoRecarga;

    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && puedoDisparar)
        {
            Disparar();
        }
        iconoRecarga.fillAmount = tiempoTranscurrido / tiempoRecarga;
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
        while (tiempoTranscurrido < tiempoRecarga)
        {
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        puedoDisparar = true;
    }
}