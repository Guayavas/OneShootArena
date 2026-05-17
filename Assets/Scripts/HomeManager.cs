using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeManager : MonoBehaviour
{
    [Header("Nickname")]
    public TMP_InputField inputNickname;

    [Header("Panel ONGs")]
    public GameObject panelONGs;

    void Start()
    {
        if (panelONGs != null)
            panelONGs.SetActive(false);
    }

    public void Jugar()
    {
        if (inputNickname != null)
        {
            PlayerPrefs.SetString("Nickname", inputNickname.text);
            PlayerPrefs.Save();
        }

        SceneManager.LoadScene("Game");
    }

    public void IrHistoria()
    {
        SceneManager.LoadScene("Historia");
    }

    public void AbrirOpciones()
    {
        Debug.Log("Botón Opciones presionado");
    }

    public void AlternarPanel()
    {
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
}