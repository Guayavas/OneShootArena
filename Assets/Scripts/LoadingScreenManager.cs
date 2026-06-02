using System.Collections;
using TMPro;
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

    [Header("Tiempos")]
    [SerializeField] private float minimumLoadingTime = 6f;
    [SerializeField] private float extraWaitAfterGameLoaded = 3f;

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
        SetProgress("Cargando partida...", 0f);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Additive);

        if (loadOperation == null)
        {
            Debug.LogError("No se pudo cargar la escena: " + gameSceneName);
            yield break;
        }

        loadOperation.allowSceneActivation = true;

        float timer = 0f;

        while (!loadOperation.isDone || timer < minimumLoadingTime)
        {
            timer += Time.deltaTime;

            float sceneProgress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(timer / minimumLoadingTime);

            float progress = Mathf.Min(sceneProgress, timeProgress) * 0.85f;

            SetProgress("Cargando partida...", progress);

            yield return null;
        }

        SetProgress("Preparando jugador...", 0.90f);

        yield return new WaitForSeconds(extraWaitAfterGameLoaded);

        SetProgress("Listo", 1f);

        yield return new WaitForSeconds(0.4f);

        Scene gameScene = SceneManager.GetSceneByName(gameSceneName);

        if (gameScene.IsValid())
        {
            SceneManager.SetActiveScene(gameScene);
        }
        else
        {
            Debug.LogWarning("No se encontró la escena del juego: " + gameSceneName);
        }

        Scene loadingScene = SceneManager.GetSceneByName(loadingSceneName);

        if (loadingScene.IsValid())
        {
            SceneManager.UnloadSceneAsync(loadingScene);
        }
        else
        {
            Debug.LogWarning("No se encontró la escena de loading: " + loadingSceneName);
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