using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerShoot))]
public class NaveAudio : NetworkBehaviour
{
    [Header("Clips de la nave")]
    public AudioClip clipLaser;
    public AudioClip clipPropulsor;
    public AudioClip clipEscudo;
    public AudioClip clipDestruccion;

    [Range(0f, 1f)] public float volLaser       = 0.7f;
    [Range(0f, 1f)] public float volPropulsor   = 0.4f;
    [Range(0f, 1f)] public float volEscudo      = 0.8f;
    [Range(0f, 1f)] public float volDestruccion = 1f;

    private AudioSource fuenteEfectos;
    private AudioSource fuentePropulsor;

    private bool propulsorActivo = false;

    public override void OnNetworkSpawn()
    {
        fuenteEfectos   = CrearFuente("Audio_Efectos",  false, 1f);
        fuentePropulsor = CrearFuente("Audio_Propulsor", true,  volPropulsor);
    }

    void Update()
    {
        if (!IsOwner) return;
        ManejarPropulsor();
    }

    private void ManejarPropulsor()
    {
        if (clipPropulsor == null) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        bool moviendose = rb != null && rb.velocity.magnitude > 0.5f;

        if (moviendose && !propulsorActivo)
        {
            propulsorActivo = true;
            fuentePropulsor.clip = clipPropulsor;
            fuentePropulsor.Play();
        }
        else if (!moviendose && propulsorActivo)
        {
            propulsorActivo = false;
            fuentePropulsor.Stop();
        }
    }

    public void SonarLaser()
    {
        if (!IsOwner) return;
        fuenteEfectos.PlayOneShot(clipLaser, volLaser);
    }

    public void SonarEscudo()
    {
        if (!IsOwner) return;
        fuenteEfectos.PlayOneShot(clipEscudo, volEscudo);
    }

    [ClientRpc]
    public void SonarDestruccionClientRpc()
    {
        fuenteEfectos.PlayOneShot(clipDestruccion, volDestruccion);
    }

    private AudioSource CrearFuente(string nombre, bool loop, float volumen)
    {
        GameObject hijo = new GameObject(nombre);
        hijo.transform.SetParent(transform);
        AudioSource src = hijo.AddComponent<AudioSource>();
        src.loop         = loop;
        src.volume       = volumen;
        src.spatialBlend = 0f;
        src.playOnAwake  = false;
        return src;
    }
}
