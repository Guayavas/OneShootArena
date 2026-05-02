using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float velocidad = 30f;
    [SerializeField] private float velocidadRotacion = 7f;

    private Rigidbody rb;
    private Vector3 inputMovimiento;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        inputMovimiento = new Vector3(x, 0f, z).normalized;
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
}