using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class SubtitleManager : MonoBehaviour
{
    [SerializeField] private Texts textsManager;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Zone zone;
    [SerializeField] private SceneController sceneManager;
    [SerializeField] private int numberLinesWelcome = 9;
    [SerializeField] private int numberLinesTutorial = 11;

    [SerializeField] private List<AudioClip> tutorialAudioClips; // Lista de AudioClips para los subtítulos
    [SerializeField] private List<AudioClip> welcomeAudioClips; // Lista de AudioClips para los subtítulos
    [SerializeField] private GameObject metaBrand; // Variable para el GameObject metaBrand
    private Spawner spawner;
    private AudioManager audioManager; // Referencia al AudioManager
    private float tutorialStartTime;
    private float tutorialEndTime;
    public float tiempoTutorial => tutorialEndTime - tutorialStartTime;

    private void Start()
    {
        spawner = GameObject.Find("Utils").GetComponent<Spawner>();
        audioManager = FindObjectOfType<AudioManager>(); // Obtener la instancia del AudioManager
        StartCoroutine(ShowSubtitles());
    }

    private IEnumerator ShowSubtitles()
    {
        yield return new WaitForSeconds(3f);
        // Método para esperar la entrada del usuario
        IEnumerator WaitForInput()
        {
            yield return new WaitForSeconds(0.1f);
            yield return new WaitUntil(() => OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Space));
        }
        tutorialStartTime = Time.time;
        for (int i = 0; i < numberLinesWelcome; i++)
        {
            // Obtener y mostrar el subtítulo
            string welcomeText = textsManager.getWelcome(i.ToString());
            subtitleText.text = welcomeText;

            // Activar el GameObject metaBrand si se llega a la línea 2
            if (i == 2 && metaBrand != null)
            {
                metaBrand.SetActive(true); // Activar el GameObject
            }

            // Reproducir el audio correspondiente
            if (i < welcomeAudioClips.Count) // Verificar que haya un clip de audio disponible
            {
                audioManager.PlayAudioClip(welcomeAudioClips[i]);
            }

            // Desactivar el GameObject metaBrand si está activo
            if (metaBrand != null && metaBrand.activeSelf && i != 2)
            {
                metaBrand.SetActive(false);
            }
            // Esperar entrada del usuario
            yield return WaitForInput();
        }

        // Diccionario para manejar las fases
        var phaseActions = new Dictionary<int, System.Func<IEnumerator>>
        {
            { 1, () => spawner.Tutorial(zone.gameObject, 1) },
            { 2, () => spawner.Tutorial(zone.gameObject, 2) },
            { 3, HandleQuestion }
        };

        for (int i = 0; i < numberLinesTutorial; i++)
        {
            // Obtener y mostrar el subtítulo
            string tutorialText = textsManager.getTutorial(i.ToString());
            subtitleText.text = tutorialText;

            // Reproducir el audio correspondiente
            if (i < tutorialAudioClips.Count) // Verificar que haya un clip de audio disponible
            {
                audioManager.PlayAudioClip(tutorialAudioClips[i]);
            }

            // Manejar acciones específicas
            if (phaseActions.ContainsKey(i))
            {
                Debug.Log($"Entro a la fase {i / 2}.");
                yield return phaseActions[i]();
            }
            else if (i == 2)
            {
                yield return new WaitForSeconds(0.1f);
                yield return new WaitUntil(() => OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger) || OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger) || Input.GetKeyDown(KeyCode.Space));
            }
            else
            {
                yield return WaitForInput();
            }
            
        }
        tutorialEndTime = Time.time;
        ExtractDataCollector extractDataCollector = GameObject.Find("Utils").GetComponent<ExtractDataCollector>();
        extractDataCollector.ExtractTutorialTime();
        audioManager.subtitleAudioSource.Stop();
        sceneManager.LoadScene("SelectEnvironment");
    }

    // Manejo de la pregunta
    private IEnumerator HandleQuestion()
    {
        List<int> numbers = spawner.getNumbers(zone.gameObject);
        string question = textsManager.getQuestionAddition(numbers.Count);
        zone.questionManager.setQuestion(question, numbers.Sum());

        yield return new WaitForSeconds(4f);
        subtitleText.text = "";

        while (!zone.questionManager.flag)
        {
            yield return null;
        }
    }
    public float GetTiempoTutorial()
    {
        return tiempoTutorial;
    }
}
