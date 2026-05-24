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

    void Start()
    {
        AsignarStats();
        if (stats != null)
        {
            stats.nickname.OnValueChanged += (oldValue, newValue) =>
            {
                textoNickname.text = newValue.ToString();
            };

            // Valor inicial
            textoNickname.text = stats.nickname.Value.ToString();
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

    void Update()
    {
        if (nave == null || textoNickname == null || Camera.main == null)
            return;

        Vector3 posicionPantalla = Camera.main.WorldToScreenPoint(nave.position + offsetMundo);

        textoNickname.transform.position = posicionPantalla + new Vector3(offsetPantalla.x, offsetPantalla.y, 0);
    }
}