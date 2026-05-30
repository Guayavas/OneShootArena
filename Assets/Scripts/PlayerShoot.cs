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
    [SerializeField] private float tiempoRecargaBase = 3f;
    [SerializeField] private GameObject indicadorBonus;

    private Image iconoRecarga;
    private float tiempoTranscurrido;
    private bool puedoDisparar = true;
    private PlayerStats stats;

    //[Header("Indicadores UI")]
    // Se eliminó indicadorPowerUpRecarga por feedback del usuario

    //[Header("Indicadores UI")]
    // Se eliminó indicadorPowerUpRecarga por feedback del usuario

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

            // Iniciamos listos para disparar
            float tiempoActualRecarga = (stats != null) ? (tiempoRecargaBase / stats.bonusRecarga.Value) : tiempoRecargaBase;
            tiempoTranscurrido = tiempoActualRecarga;
            puedoDisparar = true;
        }
    }

    private void AsignarStats()
    {
        if (stats != null) return;
        stats = GetComponent<PlayerStats>();
        if (stats == null) stats = GetComponentInParent<PlayerStats>();

        // Si aún es null, intentamos buscarlo en el mismo objeto que el NetworkObject
        if (stats == null && NetworkObject != null)
            stats = NetworkObject.GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (!IsOwner) return;
        if (stats == null)
        {
            AsignarStats();
            return;
        }

        // El bonus disminuye el tiempo de recarga: tiempoRecargaBase / bonus
        float tiempoActualRecarga = tiempoRecargaBase / stats.bonusRecarga.Value;

        if (!puedoDisparar)
        {
            tiempoTranscurrido += Time.deltaTime;
            if (tiempoTranscurrido >= tiempoActualRecarga)
            {
                puedoDisparar = true;
                tiempoTranscurrido = tiempoActualRecarga;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && puedoDisparar)
        {
            puedoDisparar = false;
            tiempoTranscurrido = 0f;
            DispararServerRpc();
        }

        if (iconoRecarga != null)
            iconoRecarga.fillAmount = Mathf.Clamp01(tiempoTranscurrido / tiempoActualRecarga);
    }


    [ServerRpc]
    void DispararServerRpc()
    {
        if (prefabBala == null) return;

        Vector3 spawnPos = (puntoDisparo != null) ? puntoDisparo.position : transform.position;

        // Notificamos a los clientes para que inicien su recarga visual
        IniciarRecargaLocalClientRpc();

        GameObject bala = Instantiate(prefabBala, spawnPos, Quaternion.Euler(90f, 0f, 0f));
        bala.GetComponent<NetworkObject>().Spawn();

        Vector3 direccion = transform.forward;
        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        if (rbBala != null) rbBala.velocity = direccion * velocidadBala;

        Bala scriptBala = bala.GetComponent<Bala>();
        if (scriptBala != null) scriptBala.duenioId.Value = OwnerClientId;

        SincronizarBalaClientRpc(bala.GetComponent<NetworkObject>().NetworkObjectId, direccion);
    }

    [ClientRpc]
    void SincronizarBalaClientRpc(ulong idBala, Vector3 dir)
    {
        if (IsServer) return;
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

    [ClientRpc]
    void ResetDisparoClientRpc()
    {
        if (IsOwner)
        {
            puedoDisparar = true;
            float bonus = (stats != null) ? stats.bonusRecarga.Value : 1f;
            tiempoTranscurrido = tiempoRecargaBase / bonus;
        }
    }

    public void ResetearCooldown()
    {
        if (!IsServer) return;
        ResetDisparoClientRpc();
    }

    public void ReducirTiempoRecargaTemporal(float cantidad)
    {
        if (!IsServer) return;
        ReducirTiempoRecargaTemporalClientRpc(cantidad);
    }

    [ClientRpc]
    private void ReducirTiempoRecargaTemporalClientRpc(float cantidad)
    {
        if (IsOwner)
        {
            tiempoTranscurrido += cantidad;
            Debug.Log($"Recarga reducida en {cantidad}s");
        }
    }

    public void AumentarBonusPermanente()
    {
        if (!IsServer) return;
        AsignarStats();
        if (stats != null) stats.AumentarBonus();
    }

    public void KillConfirmado()
    {
        if (IsServer)
        {
             AsignarStats();
             if (stats != null)
             {
                 stats.AumentarBonus();
                 stats.SumarPunto();
             }
             KillConfirmadoClientRpc();
        }
    }

    [ClientRpc]
    void KillConfirmadoClientRpc()
    {
        if (IsOwner) Debug.Log("¡Enemigo eliminado! Bonus aumentado.");
    }
}
