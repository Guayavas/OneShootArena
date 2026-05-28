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
        // Unificamos la clave a "NaveSeleccionada"
        indiceActual = PlayerPrefs.GetInt("NaveSeleccionada", 0);
        if (naves != null && naves.Length > 0)
        {
            indiceActual = Mathf.Clamp(indiceActual, 0, naves.Length - 1);
        }
        ActualizarUI();
    }

    public void SiguienteNave()
    {
        if (naves == null || naves.Length == 0) return;
        indiceActual = (indiceActual + 1) % naves.Length;
        ActualizarUI();
        GuardarSeleccion();
    }

    public void AnteriorNave()
    {
        if (naves == null || naves.Length == 0) return;
        indiceActual = (indiceActual - 1 + naves.Length) % naves.Length;

        ActualizarUI();
        GuardarSeleccion();
    }

    public int ObtenerIndiceSeleccionado()
    {
        return indiceActual;
    }

    public void GuardarSeleccion()
    {
        PlayerPrefs.SetInt("NaveSeleccionada", indiceActual);
        PlayerPrefs.Save();
        Debug.Log("Selección guardada: " + indiceActual);
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
