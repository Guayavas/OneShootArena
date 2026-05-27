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

        // En un modelo autoritario de servidor, solo el SERVIDOR procesa la física real.
        // Los clientes solo envían su intención (input).
        if (!IsServer)
        {
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 nuevoInput = new Vector3(x, 0f, z).normalized;

        if (nuevoInput != inputMovimiento)
        {
            inputMovimiento = nuevoInput;
            EnviarInputServerRpc(inputMovimiento);
        }
    }

    [ServerRpc]
    void EnviarInputServerRpc(Vector3 input)
    {
        inputMovimiento = input;
    }

    void FixedUpdate()
    {
        // Solo el servidor aplica las fuerzas físicas
        if (!IsServer) return;

        rb.AddForce(inputMovimiento * velocidad, ForceMode.Force);

        if (inputMovimiento != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(inputMovimiento);
            rb.rotation = Quaternion.Slerp(rb.rotation, rotacionObjetivo, velocidadRotacion * Time.fixedDeltaTime);
        }
    }
}