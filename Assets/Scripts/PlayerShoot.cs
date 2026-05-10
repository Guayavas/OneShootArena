using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private float velocidadBala = 20f;
    [SerializeField] private float tiempoRecarga = 3f;
    [SerializeField] private Image iconoRecarga;

    private bool puedoDisparar = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && puedoDisparar)
        {
            Disparar();
        }
        
    }
    
    void Disparar()
    {
        puedoDisparar = false;
        GameObject bala = Instantiate(prefabBala, puntoDisparo.position, Quaternion.Euler(90f, 0f, 0f));
        bala.GetComponent<Rigidbody>().velocity = transform.forward * velocidadBala;
        Invoke(nameof(ResetearDisparo), tiempoRecarga);
    }

    void ResetearDisparo()
    {
        puedoDisparar = true;
    }
}