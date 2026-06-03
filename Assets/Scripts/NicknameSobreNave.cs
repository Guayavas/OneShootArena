using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NicknameSobreNave : NetworkBehaviour
{
    [Header("Referencias")]
    public TMP_Text textoNickname;

    [Header("Ajustes")]
    public Vector3 offsetLocal = new Vector3(0, 1.2f, 0);

    private PlayerStats stats;
    private Camera camaraPrincipal;

    public override void OnNetworkSpawn()
    {
        camaraPrincipal = Camera.main;

        // Si no se asignó manualmente, lo busca automáticamente en los hijos
        if (textoNickname == null)
        {
            textoNickname = GetComponentInChildren<TMP_Text>(true);
        }

        stats = GetComponent<PlayerStats>();

        if (stats == null)
        {
            stats = GetComponentInChildren<PlayerStats>();
        }

        if (stats == null)
        {
            Debug.LogError("No se encontró PlayerStats en " + gameObject.name);
            return;
        }

        if (textoNickname != null)
        {
            textoNickname.transform.localPosition = offsetLocal;
            textoNickname.text = stats.nickname.Value.ToString();
            textoNickname.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("No se encontró TextoNickname en " + gameObject.name);
        }

        stats.nickname.OnValueChanged += OnNicknameChanged;

        if (IsOwner)
        {
            // IMPORTANTE: esta clave debe coincidir con HomeManager
            string nombreGuardado = PlayerPrefs.GetString("Nickname", "Jugador");

            if (string.IsNullOrWhiteSpace(nombreGuardado))
            {
                nombreGuardado = "Jugador";
            }

            Debug.Log("Nickname leído en Game: " + nombreGuardado);

            CambiarNicknameServerRpc(nombreGuardado);
        }
    }

    private void OnNicknameChanged(FixedString32Bytes anterior, FixedString32Bytes nuevo)
    {
        if (textoNickname != null)
        {
            textoNickname.text = nuevo.ToString();
        }
    }

    [ServerRpc]
    private void CambiarNicknameServerRpc(string nuevoNombre)
    {
        if (stats == null)
        {
            stats = GetComponent<PlayerStats>();

            if (stats == null)
            {
                stats = GetComponentInChildren<PlayerStats>();
            }
        }

        if (stats == null)
        {
            Debug.LogError("No se pudo cambiar el nickname porque no se encontró PlayerStats en " + gameObject.name);
            return;
        }

        if (string.IsNullOrWhiteSpace(nuevoNombre))
        {
            nuevoNombre = "Jugador";
        }

        stats.nickname.Value = nuevoNombre;
    }

    private void LateUpdate()
    {
        if (textoNickname == null)
        {
            return;
        }

        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }

        if (camaraPrincipal == null)
        {
            return;
        }

        textoNickname.transform.rotation = Quaternion.LookRotation(
            textoNickname.transform.position - camaraPrincipal.transform.position
        );
    }

    public override void OnNetworkDespawn()
    {
        if (stats != null)
        {
            stats.nickname.OnValueChanged -= OnNicknameChanged;
        }
    }
}