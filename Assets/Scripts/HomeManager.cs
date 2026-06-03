using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeManager : MonoBehaviour
{
    [Header("Nickname")]
    public TMP_InputField inputNickname;

    [Header("Selecci\xf3n de nave")]
    public ShipSelectionManager shipSelectionManager;

    [Header("Panel ONGs")]
    public GameObject panelONGs;

    [Header("Validaci�n Nickname")]
    public GameObject panelErrorNickname;
    public float duracionError = 2f;

    private Coroutine rutinaError;

    void Start()
    {
        if (panelONGs != null)
            panelONGs.SetActive(false);

        if (panelErrorNickname != null)
            panelErrorNickname.SetActive(false);
    }

    public void Jugar()
    {
        AudioManager.Instance?.ReproducirClick();
        string nickname = "";

        if (inputNickname != null)
        {
            nickname = inputNickname.text.Trim();
        }

        if (string.IsNullOrEmpty(nickname))
        {
            MostrarErrorNickname();
            Debug.Log("Debe ingresar un nickname para jugar.");
            return;
        }

        if (panelErrorNickname != null)
            panelErrorNickname.SetActive(false);

        PlayerPrefs.SetString("Nickname", nickname);

        if (shipSelectionManager != null)
        {
            shipSelectionManager.GuardarSeleccion();
        }
        else
        {
            PlayerPrefs.SetInt("NaveSeleccionada", 0);
            Debug.LogWarning("No se asign\xf3 ShipSelectionManager. Se usar\xe1 la nave por defecto.");
        }

        Debug.Log("Nave seleccionada guardada: " + PlayerPrefs.GetInt("NaveSeleccionada", 0));

        PlayerPrefs.Save();

        SceneManager.LoadScene("Loading");
    }

    public void IrHistoria()
    {
        AudioManager.Instance?.ReproducirClick();
        SceneManager.LoadScene("Historia");
    }

    public void AbrirOpciones()
    {
        Debug.Log("Bot�n Opciones presionado");
    }

    public void AlternarPanel()
    {
        AudioManager.Instance?.ReproducirClick();
        if (panelONGs != null)
            panelONGs.SetActive(!panelONGs.activeSelf);
    }

    public void CerrarPanel()
    {
        if (panelONGs != null)
            panelONGs.SetActive(false);
    }

    public void AbrirNatura()
    {
        Application.OpenURL("https://natura.org.co/");
    }

    public void AbrirGreenpeace()
    {
        Application.OpenURL("https://www.greenpeace.org/international/");
    }

    public void AbrirWWF()
    {
        Application.OpenURL("https://www.wwf.org.co/");
    }

    public void MostrarErrorNickname()
    {
        if (panelErrorNickname == null)
            return;

        panelErrorNickname.SetActive(true);

        if (rutinaError != null)
        {
            StopCoroutine(rutinaError);
        }

        rutinaError = StartCoroutine(OcultarErrorNickname());
    }

    public void CerrarErrorNickname()
    {
        if (rutinaError != null)
        {
            StopCoroutine(rutinaError);
            rutinaError = null;
        }

        if (panelErrorNickname != null)
        {
            panelErrorNickname.SetActive(false);
        }
    }

    IEnumerator OcultarErrorNickname()
    {
        yield return new WaitForSeconds(duracionError);

        if (panelErrorNickname != null)
        {
            panelErrorNickname.SetActive(false);
        }

        rutinaError = null;
    }
}
