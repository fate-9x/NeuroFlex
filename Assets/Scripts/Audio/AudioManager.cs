using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource bgAudioSource;
    public AudioSource subtitleAudioSource;


    // Start is called before the first frame update
    void Start()
    {
        // Puedes iniciar la música de fondo aquí si es necesario
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void PlayBgMusic()
    {
        if (!bgAudioSource.isPlaying)
        {
            bgAudioSource.Play();
        }
    }

    public void PlayAudioClip(AudioClip clip) // Método para reproducir un AudioClip
    {
        if (clip != null)
        {
            subtitleAudioSource.clip = clip; // Cambiar el clip del AudioSource
            subtitleAudioSource.Play(); // Reproducir el clip
        }
    }
}
