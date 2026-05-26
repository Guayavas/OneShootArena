using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float velocidad = 30f;
    [SerializeField] private float velocidadRotacion = 7f;

    private Rigidbody rb;
    private Vector3 inputMovimiento;

    private bool boostActivo = false;
    private float velocidadBase;
    private float duracionBoost;
    private float tiempoBoostRestante;
    private Coroutine rutinaBoost;

    public bool BoostActivo => boostActivo;
    public float ProgresoBoost => boostActivo ? (tiempoBoostRestante / duracionBoost) : 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        velocidadBase = velocidad;
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        inputMovimiento = new Vector3(x, 0f, z).normalized;

        if (boostActivo)
            tiempoBoostRestante -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        rb.AddForce(inputMovimiento * velocidad, ForceMode.Force);

        if (inputMovimiento != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(inputMovimiento);
            rb.rotation = Quaternion.Slerp(rb.rotation, rotacionObjetivo, velocidadRotacion * Time.fixedDeltaTime);
        }
    }

    public void ActivarBoostVelocidad(float multiplicador, float duracion)
    {
        if (rutinaBoost != null)
            StopCoroutine(rutinaBoost);

        velocidad = velocidadBase * multiplicador;
        boostActivo = true;
        duracionBoost = duracion;
        tiempoBoostRestante = duracion;
        rutinaBoost = StartCoroutine(DesactivarBoostTras(duracion));
    }

    private IEnumerator DesactivarBoostTras(float duracion)
    {
        yield return new WaitForSeconds(duracion);
        velocidad = velocidadBase;
        boostActivo = false;
        tiempoBoostRestante = 0f;
        rutinaBoost = null;
    }
}