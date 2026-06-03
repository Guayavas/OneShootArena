using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Música de fondo")]
    public AudioClip musicaHome;
    public AudioClip musicaGameplay;
    public AudioClip musicaHistoria;
    [Range(0f, 1f)] public float volumenMusica = 0.4f;

    [Header("Efectos de UI")]
    public AudioClip sfxClick;
    public AudioClip sfxHover;
    [Range(0f, 1f)] public float volumenUI = 0.8f;

    private AudioSource fuenteMusica;
    private AudioSource fuenteUI;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        fuenteMusica = CrearFuente("Fuente_Musica", true, volumenMusica);
        fuenteUI     = CrearFuente("Fuente_UI",     false, volumenUI);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnScenaCargada;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnScenaCargada;
    }

    private void OnScenaCargada(Scene escena, LoadSceneMode modo)
    {
        switch (escena.name)
        {
            case "Home":
                ReproducirMusica(musicaHome);
                break;
            case "Game":
                ReproducirMusica(musicaGameplay);
                break;
            case "Historia":
                ReproducirMusica(musicaHistoria);
                break;
        }
    }

    public void ReproducirMusica(AudioClip clip)
    {
        if (clip == null || fuenteMusica.clip == clip) return;
        fuenteMusica.clip = clip;
        fuenteMusica.Play();
    }

    public void SetVolumenMusica(float valor)
    {
        volumenMusica = Mathf.Clamp01(valor);
        fuenteMusica.volume = volumenMusica;
    }

    public void PausarMusica()   => fuenteMusica.Pause();
    public void ReanudarMusica() => fuenteMusica.UnPause();

    public void ReproducirClick() => fuenteUI.PlayOneShot(sfxClick, volumenUI);
    public void ReproducirHover() => fuenteUI.PlayOneShot(sfxHover, volumenUI);

    private AudioSource CrearFuente(string nombre, bool loop, float volumen)
    {
        GameObject hijo = new GameObject(nombre);
        hijo.transform.SetParent(transform);
        AudioSource src = hijo.AddComponent<AudioSource>();
        src.loop        = loop;
        src.volume      = volumen;
        src.playOnAwake = false;
        return src;
    }
}
