using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NicknameSobreNave : MonoBehaviour
{
    [Header("Referencias")]
    public Transform nave;
    public TMP_Text textoNickname;

    [Header("Ajuste sobre la nave")]
    public Vector3 offsetMundo = new Vector3(0, 1.2f, 0);
    public Vector2 offsetPantalla = new Vector2(0, 25);

    void Start()
    {
        string nickname = PlayerPrefs.GetString("Nickname", "Jugador");

        if (textoNickname != null)
        {
            textoNickname.text = nickname;
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