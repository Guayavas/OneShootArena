using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float velocidad = 30f;
    [SerializeField] private float velocidadRotacion = 7f;

    private Rigidbody rb;
    private Vector3 inputMovimiento;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        // Solo el dueño debe tener el Rigidbody como NO cinemático para que las fuerzas funcionen
        // O bien, usar NetworkTransform para sincronizar.
        if (!IsOwner)
        {
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        inputMovimiento = new Vector3(x, 0f, z).normalized;
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        rb.AddForce(inputMovimiento * velocidad, ForceMode.Force);

        if (inputMovimiento != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(inputMovimiento);
            rb.rotation = Quaternion.Slerp(rb.rotation, rotacionObjetivo, velocidadRotacion * Time.fixedDeltaTime);
        }
    }
}