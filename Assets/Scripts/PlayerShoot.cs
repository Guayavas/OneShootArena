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

    [Header("UI")]
    [SerializeField] private Image iconoRecarga;
    [SerializeField] private GameObject indicadorBonus;

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
            if (iconoRecarga == null)
            {
                GameObject obj = GameObject.Find("IndicadorRecarga");
                if (obj != null) iconoRecarga = obj.GetComponent<Image>();
            }

            if (indicadorBonus == null)
            {
                indicadorBonus = GameObject.Find("IndicadorBonusRecarga");
            }

            // Iniciamos con el tiempo de recarga actual
            float bonus = (stats != null) ? stats.bonusRecarga.Value : 1f;
            tiempoTranscurrido = tiempoRecargaBase / bonus;
        }
    }

    private void AsignarStats()
    {
        if (stats != null) return;
        stats = GetComponent<PlayerStats>();
        if (stats == null) stats = GetComponentInParent<PlayerStats>();
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

        // El bonus disminuye el tiempo de recarga: tiempoRecargaBase / bonus
        float tiempoActualRecarga = tiempoRecargaBase / stats.bonusRecarga.Value;

        if (!puedoDisparar && tiempoTranscurrido < tiempoActualRecarga)
        {
            tiempoTranscurrido += Time.deltaTime;
        }

        if (iconoRecarga != null)
            iconoRecarga.fillAmount = Mathf.Clamp01(tiempoTranscurrido / tiempoActualRecarga);

        if (indicadorBonus != null)
            indicadorBonus.SetActive(stats.bonusRecarga.Value > 1.01f);
    }

    [ServerRpc]
    void DispararServerRpc()
    {
        if (!puedoDisparar) return;

        if (prefabBala == null) return;

        Vector3 spawnPos = (puntoDisparo != null) ? puntoDisparo.position : transform.position;
        puedoDisparar = false;
        IniciarRecargaLocalClientRpc();

        GameObject bala = Instantiate(prefabBala, spawnPos, Quaternion.Euler(90f, 0f, 0f));
        bala.GetComponent<NetworkObject>().Spawn();

        Vector3 direccion = transform.forward;
        Rigidbody rbBala = bala.GetComponent<Rigidbody>();
        if (rbBala != null) rbBala.velocity = direccion * velocidadBala;

        Bala scriptBala = bala.GetComponent<Bala>();
        if (scriptBala != null) scriptBala.duenioId.Value = OwnerClientId;

        SincronizarBalaClientRpc(bala.GetComponent<NetworkObject>().NetworkObjectId, direccion);

        StartCoroutine(Recargar());
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

    IEnumerator Recargar()
    {
        AsignarStats();
        float bonus = (stats != null) ? stats.bonusRecarga.Value : 1f;
        float duracion = tiempoRecargaBase / bonus;
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
            tiempoTranscurrido = tiempoRecargaBase / bonus;
        }
    }

    public void ReducirTiempoRecarga()
    {
        if (!IsServer) return;

        // Resetear recarga inmediatamente
        puedoDisparar = true;
        StopCoroutine(nameof(Recargar));

        ResetDisparoClientRpc();

        ReducirTiempoRecargaClientRpc();
    }

    [ClientRpc]
    private void ReducirTiempoRecargaClientRpc()
    {
        if (IsOwner) Debug.Log("¡Recarga reseteada!");
    }

    public void ReducirPequenoTiempoRecarga()
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
