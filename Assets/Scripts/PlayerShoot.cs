using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode;

public class PlayerShoot : NetworkBehaviour
{
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private float velocidadBala = 20f;
    [SerializeField] private float tiempoRecarga = 3f;

    private Image iconoRecarga;
    private float tiempoTranscurrido;
    private bool puedoDisparar = true;
    private PlayerStats stats;

    void Start()
    {
        if (!IsOwner) return;

        GameObject canvas = GameObject.Find("IndicadorRecarga");
        if (canvas != null) iconoRecarga = canvas.GetComponent<Image>();

        tiempoTranscurrido = tiempoRecarga;
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Space) && puedoDisparar)
            DispararServerRpc();

        if (!puedoDisparar && tiempoTranscurrido < tiempoRecarga * stats.bonusRecarga.Value)
        {
            tiempoTranscurrido += Time.deltaTime;
        }

        if (iconoRecarga != null)
            iconoRecarga.fillAmount = tiempoTranscurrido / (tiempoRecarga * stats.bonusRecarga.Value);
    }

    [ServerRpc]
    void DispararServerRpc()
    {
        if (!puedoDisparar) return;

        puedoDisparar = false;
        IniciarRecargaLocalClientRpc();

        GameObject bala = Instantiate(prefabBala, puntoDisparo.position, Quaternion.Euler(90f, 0f, 0f));
        bala.GetComponent<NetworkObject>().Spawn();

        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        rbBala.velocity = transform.forward * velocidadBala;

        Bala scriptBala = bala.GetComponent<Bala>();
        scriptBala.duenioId.Value = OwnerClientId;

        StartCoroutine(Recargar());
    }

    [ClientRpc]
    void IniciarRecargaLocalClientRpc()
    {
        if (IsOwner)
        {
            puedoDisparar = false;
            tiempoTranscurrido = 0f;
        }
    }

    IEnumerator Recargar()
    {
        // En el servidor, usamos el valor del bonus para el tiempo de espera
        float duracion = tiempoRecarga * stats.bonusRecarga.Value;
        yield return new WaitForSeconds(duracion);
        puedoDisparar = true;
        ResetDisparoClientRpc();
    }

    [ClientRpc]
    void ResetDisparoClientRpc()
    {
        if (IsOwner)
        {
            puedoDisparar = true;
            tiempoTranscurrido = tiempoRecarga * stats.bonusRecarga.Value;
        }
    }

    public void KillConfirmado()
    {
        if (IsServer)
        {
             KillConfirmadoClientRpc();
        }
    }

    [ClientRpc]
    void KillConfirmadoClientRpc()
    {
        if (IsOwner)
        {
            stats.AumentarBonus();
            stats.SumarPunto();
        }
    }
}