using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text tipText;

    [Header("Escenas")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string loadingSceneName = "Loading";

    [Header("Tiempos y Seguridad")]
    [SerializeField] private float minimumLoadingTime = 2f;
    [SerializeField] private float timeoutSeconds = 20f;

    [Header("Consejos")]
    [TextArea]
    [SerializeField] private string[] tips;

    private void Start()
    {
        SetRandomTip();
        StartCoroutine(LoadGameAdditive());
    }

    private IEnumerator LoadGameAdditive()
    {
        SetProgress("Cargando mapa...", 0.1f);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);

        if (loadOperation == null)
        {
            Debug.LogError("No se pudo cargar la escena: " + gameSceneName);
            yield break;
        }

        // 1. Esperar a que la escena se cargue físicamente
        while (!loadOperation.isDone)
        {
            float progress = Mathf.Clamp01(loadOperation.progress / 0.9f) * 0.5f;
            SetProgress("Cargando recursos...", progress);
            yield return null;
        }

        // 2. IMPORTANTE: Activar la escena inmediatamente para que el spawn ocurra en ella
        Scene gameScene = SceneManager.GetSceneByName(gameSceneName);
        if (gameScene.IsValid())
        {
            SceneManager.SetActiveScene(gameScene);
            Debug.Log("[LOADING] Escena de juego activada. Esperando spawn de nave...");
        }

        // 3. Esperar dinámicamente a que la nave aparezca o se cumpla el tiempo mínimo
        float timer = 0f;
        bool spawned = false;

        while (timer < timeoutSeconds)
        {
            timer += Time.deltaTime;

            // Verificar si el jugador ya tiene su objeto asignado en red
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsClient &&
                NetworkManager.Singleton.LocalClient != null &&
                NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                spawned = true;
                if (timer >= minimumLoadingTime) break;
            }

            float progress = 0.5f + Mathf.Clamp01(timer / minimumLoadingTime) * 0.4f;
            SetProgress("Esperando a otros jugadores...", progress);

            yield return null;
        }

        if (!spawned)
        {
            Debug.LogWarning("[LOADING] Tiempo de espera agotado o nave no detectada, procediendo de todos modos.");
        }

        SetProgress("¡Listo!", 1f);
        yield return new WaitForSeconds(0.5f);

        // 4. Descargar la escena de carga
        Scene loadingScene = SceneManager.GetSceneByName(loadingSceneName);
        if (loadingScene.IsValid())
        {
            SceneManager.UnloadSceneAsync(loadingScene);
        }
    }

    private void SetProgress(string message, float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (loadingText != null)
            loadingText.text = message;

        if (loadingSlider != null)
            loadingSlider.value = progress;

        if (percentText != null)
            percentText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }

    private void SetRandomTip()
    {
        if (tipText == null || tips == null || tips.Length == 0)
            return;

        int randomIndex = Random.Range(0, tips.Length);
        tipText.text = $"<color=#00E7FF>Consejo:</color> {tips[randomIndex]}";
    }
}
