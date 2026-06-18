using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ControlHistoria : MonoBehaviour
{
    public Image imagenHistoria;

    public Sprite[] imagenes;

    private int indice = 0;

    void Start()
    {
        imagenHistoria.sprite = imagenes[indice];
    }

    public void Siguiente()
    {
        if (indice < imagenes.Length - 1)
        {
            indice++;
            imagenHistoria.sprite = imagenes[indice];
        }
    }

    public void Anterior()
    {
        if (indice > 0)
        {
            indice--;
            imagenHistoria.sprite = imagenes[indice];
        }
    }

    public void VolverMenu()
    {
        SceneManager.LoadScene("Home");
    }
}
