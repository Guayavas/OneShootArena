using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private float velocidadBala = 20f;
    [SerializeField] private float tiempoRecarga = 3f;

    [Header("HU17 - Limite de reduccion por objeto recogible")]
    [Tooltip("Minimo de tiempo de recarga que puede quedar tras recoger objetos (en segundos)")]
    [SerializeField] private float minimoTiempoTrasReduccion = 0.3f;

    private Image iconoRecarga;
    private float tiempoTranscurrido;
    private bool puedoDisparar = true;
    private PlayerStats stats;

    void Start()
    {
        iconoRecarga = GameObject.Find("IndicadorRecarga").GetComponent<Image>();
        iconoRecarga.fillAmount = 1f;
        tiempoTranscurrido = tiempoRecarga;
        stats = GameObject.Find("GameManager").GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && puedoDisparar)
            Disparar();

        iconoRecarga.fillAmount = tiempoTranscurrido / (tiempoRecarga * stats.bonusRecarga);
    }

    void Disparar()
    {
        puedoDisparar = false;
        tiempoTranscurrido = 0f;
        GameObject bala = Instantiate(prefabBala, puntoDisparo.position, Quaternion.Euler(90f, 0f, 0f));
        bala.GetComponent<Rigidbody>().velocity = transform.forward * velocidadBala;
        bala.GetComponent<Bala>().duenio = this.gameObject;
        StartCoroutine(Recargar());
    }

    IEnumerator Recargar()
    {
        while (tiempoTranscurrido < tiempoRecarga * stats.bonusRecarga)
        {
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        puedoDisparar = true;
    }

    public void AplicarReduccionRecarga(float reduccion)
    {
        if (puedoDisparar) return;

        float tiempoTotalRecarga = tiempoRecarga * stats.bonusRecarga;
        float tiempoRestante = tiempoTotalRecarga - tiempoTranscurrido;
        float tiempoRestanteMinimo = tiempoTotalRecarga * minimoTiempoTrasReduccion / tiempoRecarga;

        if (tiempoRestante <= tiempoRestanteMinimo) return;

        float reduccionReal = Mathf.Min(reduccion, tiempoRestante - tiempoRestanteMinimo);
        tiempoTranscurrido += reduccionReal;
    }

    public void KillConfirmado()
    {
        stats.AumentarBonus();
        stats.SumarPunto();
    }
}