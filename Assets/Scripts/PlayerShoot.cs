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

    void Awake()
    {
        AsignarStats();
    }

    public override void OnNetworkSpawn()
    {
        AsignarStats();

        if (IsOwner)
        {
            GameObject canvas = GameObject.Find("IndicadorRecarga");
            if (canvas != null) iconoRecarga = canvas.GetComponent<Image>();

            float bonus = (stats != null) ? stats.bonusRecarga.Value : 1f;
            tiempoTranscurrido = tiempoRecarga * bonus;
        }
    }

    private void AsignarStats()
    {
        if (stats != null) return;

        // Intentar buscar en la propia nave (lo ideal)
        stats = GetComponentInParent<PlayerStats>();

        // Si no está ahí, buscar el global en el GameManager (como lo tienes ahora)
        if (stats == null)
        {
            GameObject gm = GameObject.Find("GameManager");
            if (gm != null) stats = gm.GetComponent<PlayerStats>();
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (stats == null) return;

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
        AsignarStats(); // Asegurar referencia en el servidor

        if (!puedoDisparar)
        {
            Debug.LogWarning("Servidor: Cooldown activo para el cliente: " + OwnerClientId);
            return;
        }

        if (prefabBala == null)
        {
            Debug.LogError("Servidor: PrefabBala es NULO");
            return;
        }

        if (puntoDisparo == null)
        {
            // Si puntoDisparo es nulo, intentar usar la posición de la nave como fallback
            Debug.LogWarning("Servidor: PuntoDisparo es NULO, usando posición de la nave");
        }

        Vector3 spawnPos = (puntoDisparo != null) ? puntoDisparo.position : transform.position;
        puedoDisparar = false;
        IniciarRecargaLocalClientRpc();

        GameObject bala = Instantiate(prefabBala, spawnPos, Quaternion.Euler(90f, 0f, 0f));
        bala.GetComponent<NetworkObject>().Spawn();

        Vector3 direccion = transform.forward;

        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        rbBala.velocity = direccion * velocidadBala;

        Bala scriptBala = bala.GetComponent<Bala>();
        scriptBala.duenioId.Value = OwnerClientId;

        // Sincronizar velocidad visual en clientes
        SincronizarBalaClientRpc(bala.GetComponent<NetworkObject>().NetworkObjectId, direccion);

        StartCoroutine(Recargar());
    }

    [ClientRpc]
    void SincronizarBalaClientRpc(ulong idBala, Vector3 dir)
    {
        // En el servidor ya se hizo, solo clientes
        if (IsServer) return;

        // Intentar encontrar la bala por su ID de red
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idBala, out NetworkObject objBala))
        {
            Rigidbody rb = objBala.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = dir * velocidadBala;
        }
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
        float bonus = (stats != null) ? stats.bonusRecarga.Value : 1f;
        float duracion = tiempoRecarga * bonus;
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
            float bonus = (stats != null) ? stats.bonusRecarga.Value : 1f;
            tiempoTranscurrido = tiempoRecarga * bonus;
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