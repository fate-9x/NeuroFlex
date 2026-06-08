using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
public class Inputs : MonoBehaviour
{
    [SerializeField] Animator animatorPlayer;
    [SerializeField] public GameObject screen;
    [SerializeField] public GameObject headPlayer;
    private bool flagInitMovement = false;
    // Variables para el temporizador
    private bool isTimerRunning = false;
    private float timerStartTime;
    public float responseTimePlayerUp = 0f;
    public bool numberSpawned = false;
    
    public Animator animator;
    public float playerInitPositionY;
    private APIManager apiManager;
    [SerializeField] public TextMeshProUGUI debugDataText;
    public float activeTaskStartTime = 0f;
    public float tiempoActivoTarea = 0f;
    private bool isActiveTaskTimerRunning = false;
    private ExtractDataCollector extractDataCollector;
    private ScoreManager scoreManager;
    // Update is called once per frame
    void Start()
    {
        apiManager = FindAnyObjectByType<APIManager>();
        if (apiManager == null)
        {
            GameObject apiObj = new GameObject("APIManager");
            apiManager = apiObj.AddComponent<APIManager>();
        }

        // Buscar el ExtractDataCollector
        extractDataCollector = GameObject.Find("Utils").GetComponent<ExtractDataCollector>();
        scoreManager = GameObject.Find("Utils").GetComponent<ScoreManager>();
        if (extractDataCollector == null)
        {
            Debug.LogWarning("No se encontró ExtractDataCollector en la escena");
        }
        if (scoreManager == null)
        {
            Debug.LogWarning("No se encontró ScoreManager en la escena");
        }
        

    }
    void Update()
    {
        if (ConfirmInput.ConfirmWasPressedThisFrame())
        {
            if (screen.activeSelf)
            {
                StartMovement(screen);
            }
            // Enviar JSON a la API al presionar espacio

        }
        // Sentado = 1.35, de pie < 1.35
        if (flagInitMovement && numberSpawned)
        {
            // Iniciar el temporizador si no está corriendo
            if (!isTimerRunning)
            {
                isTimerRunning = true;
                timerStartTime = Time.time;
            }

            float currentTime = Time.time - timerStartTime;

            if (currentTime > 13f && isTimerRunning)
            {
                responseTimePlayerUp = 0f;
                isTimerRunning = false;
                numberSpawned = false;
                Debug.Log("Tiempo de respuesta para pararse agotado, asignado como 0");
            }
            else if (headPlayer.transform.position.y > 1.35f && isTimerRunning)
            {
                responseTimePlayerUp = Time.time - timerStartTime;
                isTimerRunning = false;
                numberSpawned = false;
            }
        }
        else
        {
            // Resetear el temporizador si las condiciones no se cumplen
            isTimerRunning = false;
        }

    }
    public void StartMovement(GameObject screen)
    {
        if (animatorPlayer != null && screen != null && !flagInitMovement)
        {
            playerInitPositionY = headPlayer.transform.position.y;
            scoreManager.scoreNumbers = 0;
            animatorPlayer.SetTrigger("Start");
            screen.SetActive(false);
            flagInitMovement = true;
            activeTaskStartTime = Time.time;
            isActiveTaskTimerRunning = true;
        }
        Debug.Log("StartMovement");
    }

    public void StopActiveTaskTimer()
    {
        if (isActiveTaskTimerRunning)
        {
            
            tiempoActivoTarea = Time.time - activeTaskStartTime;
            isActiveTaskTimerRunning = false;
            Debug.Log($"TiempoActivoTarea: {tiempoActivoTarea} segundos");
        }
    }

    public void ShowDebugData()
    {
        if (apiManager == null || debugDataText == null) return;
        var data = apiManager.data;
        debugDataText.text =
            //$"ManoPredominante: {data.ManoPredominante}\n";
            $"TiempoRespuestaPararse: {responseTimePlayerUp}\n" +
            $"Precision: {data.Precision}\n" +
            $"TiempoActivoTarea: {data.TiempoActivoTarea}\n" +
            //$"TasaAbandono: {data.TasaAbandono}\n" +
            $"CantAciertasTotales: {data.CantAciertasTotales}\n" +
            //$"CantAciertosAntesError: {data.CantAciertosAntesError}\n" +
            $"ObjetosInteractuadosCorrectamente: {data.ObjetosInteractuadosCorrectamente}\n" +
            $"TiempoRespuestaPregunta1: {data.TiempoRespuestaPregunta1}\n" +
            $"TiempoRespuestaPregunta2: {data.TiempoRespuestaPregunta2}\n" +
            $"TiempoRespuestaPregunta3: {data.TiempoRespuestaPregunta3}\n" +
            //$"TiempoReacciónVisual: {data.TiempoReacciónVisual}\n" +
            $"TiempoCapturarNumero: {data.TiempoCapturarNumero}\n" +
            $"TiempoTutorial: {data.TiempoTutorial}\n" +
            $"TipoEscena: {data.TipoEscena}\n";
            //$"Edad: {data.Edad}";
    }
}
