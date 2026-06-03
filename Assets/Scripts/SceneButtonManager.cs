using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonManager : MonoBehaviour
{
    public void IrACreditos()
    {
        SceneManager.LoadScene("Creditos");
    }

    public void VolverAHome()
    {
        SceneManager.LoadScene("Home");
    }
}
