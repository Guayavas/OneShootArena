using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class ShipOption
    {
        public string nombre;
        public Sprite preview;
    }

    [Header("Opciones de naves")]
    public ShipOption[] naves;

    [Header("Referencias UI")]
    public Image imagenPreview;
    public TMP_Text textoNombre;

    private int indiceActual = 0;

    void Start()
    {
        indiceActual = PlayerPrefs.GetInt("SelectedShip", 0);
        indiceActual = Mathf.Clamp(indiceActual, 0, naves.Length - 1);
        ActualizarUI();
    }

    public void SiguienteNave()
    {
        indiceActual++;

        if (indiceActual >= naves.Length)
            indiceActual = 0;

        ActualizarUI();
    }

    public void AnteriorNave()
    {
        indiceActual--;

        if (indiceActual < 0)
            indiceActual = naves.Length - 1;

        ActualizarUI();
    }

    public int ObtenerIndiceSeleccionado()
    {
        return indiceActual;
    }

    public void GuardarSeleccion()
    {
        PlayerPrefs.SetInt("SelectedShip", indiceActual);
        PlayerPrefs.Save();
    }

    private void ActualizarUI()
    {
        if (naves == null || naves.Length == 0)
        {
            Debug.LogWarning("No hay naves configuradas en ShipSelectionManager.");
            return;
        }

        if (imagenPreview != null)
            imagenPreview.sprite = naves[indiceActual].preview;

        if (textoNombre != null)
            textoNombre.text = naves[indiceActual].nombre;
    }
}
