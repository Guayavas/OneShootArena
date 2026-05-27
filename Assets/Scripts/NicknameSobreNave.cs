using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using Unity.Netcode;

public class NicknameSobreNave : NetworkBehaviour
{
    [Header("Referencias")]
    public Transform nave;
    public TMP_Text textoNickname;

    [Header("Ajuste sobre la nave")]
    public Vector3 offsetMundo = new Vector3(0, 1.2f, 0);
    public Vector2 offsetPantalla = new Vector2(0, 25);

    private PlayerStats stats;

    public override void OnNetworkSpawn()
    {
        // Solo queremos ver nuestro propio nombre si somos el dueño
        // O si quieres que todos vean todos, quitamos esta restricción.
        // El usuario dijo: "me basta con que el jugador lo pueda ver. O sea, el jugador... Personalmente pueda ver su nombre personalmente."

        // Sin embargo, para que funcione en red, buscaremos los stats del objeto padre (la nave)
        AsignarStats();

        if (stats != null)
        {
            // Si es el dueño de la nave, configuramos el nombre
            if (stats.IsOwner)
            {
                stats.nickname.OnValueChanged += (oldValue, newValue) =>
                {
                    textoNickname.text = newValue.ToString();
                };
                textoNickname.text = stats.nickname.Value.ToString();
            }
            else
            {
                // Si no somos el dueño, tal vez queremos ocultarlo o mostrarlo si se sincroniza
                textoNickname.text = stats.nickname.Value.ToString();
                stats.nickname.OnValueChanged += (oldValue, newValue) =>
                {
                    textoNickname.text = newValue.ToString();
                };
            }
        }
    }

    void Start()
    {
        // Intentar asignar la nave al inicio
        ActualizarReferenciaNave();
    }

    private void ActualizarReferenciaNave()
    {
        if (nave == null)
        {
            // Intentamos buscar en los ancestros si estamos dentro de la jerarquía de la nave
            PlayerMovement mov = GetComponentInParent<PlayerMovement>();
            if (mov != null) nave = mov.transform;
            else nave = transform.root;
        }
    }

    private void AsignarStats()
    {
        if (stats != null) return;

        stats = GetComponentInParent<PlayerStats>();

        if (stats == null)
        {
            GameObject gm = GameObject.Find("GameManager");
            if (gm != null) stats = gm.GetComponent<PlayerStats>();
        }
    }

    void LateUpdate()
    {
        if (textoNickname == null)
            return;

        if (nave == null)
        {
            ActualizarReferenciaNave();
            if (nave == null)
            {
                textoNickname.enabled = false;
                return;
            }
        }

        if (Camera.main == null)
            return;

        Vector3 posicionPantalla = Camera.main.WorldToScreenPoint(nave.position + offsetMundo);

        // Si la nave está detrás de la cámara, no mostrar
        if (posicionPantalla.z < 0)
        {
            textoNickname.enabled = false;
        }
        else
        {
            textoNickname.enabled = true;
            textoNickname.transform.position = posicionPantalla + new Vector3(offsetPantalla.x, offsetPantalla.y, 0);
        }
    }
}