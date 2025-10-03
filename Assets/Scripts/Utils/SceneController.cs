using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [SerializeField] private AudioClip selectEnvironmentAudioClip; // Nuevo AudioClip para la escena SelectEnvironment
    private FadeOVR[] screenFades;

    void Start()
    {
        GetScreenFades();
    }

    private void GetScreenFades()
    {
        // Encuentra todos los GameObjects con el tag "MainCamera"
        GameObject[] cameras = GameObject.FindGameObjectsWithTag("MainCamera");

        // Inicializa el array de FadeOVR con el mismo tamaño que el número de cámaras
        screenFades = new FadeOVR[cameras.Length];

        // Obtiene la instancia de FadeOVR de cada cámara
        for (int i = 0; i < cameras.Length; i++)
        {
            screenFades[i] = cameras[i].GetComponent<FadeOVR>();
        }

        // Obtiene el nombre de la escena actual
        string sceneName = GetCurrentSceneName();

        if(sceneName == "NatureScene" || sceneName == "City")
        {
            StartCoroutine(LoadFadeIn(10.0f));
        }
        else
        {
            StartCoroutine(LoadFadeIn(1.0f));
        }
    }

    IEnumerator LoadFadeIn(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        foreach (FadeOVR screenFade in screenFades)
        {
            screenFade.FadeIn();
        }
        // Reproducir el audio si la escena cargada es "SelectEnvironment"
        if (GetCurrentSceneName() == "SelectEnvironment")
        {
            Debug.Log("SIiii");
            AudioManager audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
            
            if (audioManager != null && selectEnvironmentAudioClip != null)
            {
                yield return new WaitForSeconds(2f);
                audioManager.PlayAudioClip(selectEnvironmentAudioClip); // Reproducir el clip de audio
            }
        }
    }

    // Función para cargar la siguiente escena
    public void LoadScene(string sceneName = "")
    {
        // Guardar el tipo de escena antes de cargarla
        APIManager apiManager = GameObject.Find("Utils").GetComponent<APIManager>();
        if (apiManager != null)
        {
            // Puedes traducir el nombre si quieres mostrarlo en inglés
            int tipoEscena = 0;
            if (sceneName == "NatureScene") tipoEscena = 1;
            else if (sceneName == "City") tipoEscena = 2;
            apiManager.data.TipoEscena = tipoEscena;
        }

        StartCoroutine(LoadSceneWithLoadingScreen(sceneName));
    }

    IEnumerator LoadSceneWithLoadingScreen(string sceneName)
    {
        foreach (FadeOVR screenFade in screenFades)
        {
            screenFade.FadeOut();
        }

        yield return new WaitForSeconds(screenFades[0].fadeTime);

        // Si sceneName está vacío, carga la siguiente escena
        if (string.IsNullOrEmpty(sceneName))
        {
            // Obtiene el índice de la escena actual
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

            // Calcula el índice de la siguiente escena
            int nextSceneIndex = currentSceneIndex + 1;

            // Carga la siguiente escena
            sceneName = SceneManager.GetSceneByBuildIndex(nextSceneIndex).name;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.M))
        {
            LoadScene("NatureScene");
        }
    }

    // Funcion para obtener el nombre de la escena actual
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
}