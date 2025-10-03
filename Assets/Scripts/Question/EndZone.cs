using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EndZone : MonoBehaviour
{
    [SerializeField] private GameObject dialogScreen;
    [SerializeField] private Animator animatorPlayer;
    public TextMeshProUGUI SessionText;
    private ExtractDataCollector extractDataCollector;

    private void Start()
    {
        extractDataCollector = GameObject.Find("Utils").GetComponent<ExtractDataCollector>();
        
        // Suscribirse al evento para recibir el session_id
        ExtractDataCollector.OnSessionIdReceived += OnSessionIdReceived;
        
        // Configurar texto inicial
        if (SessionText != null)
        {
            SessionText.text = "Obteniendo número de sesión...";
        }
    }
    
    private void OnDestroy()
    {
        // Desuscribirse del evento para evitar memory leaks
        ExtractDataCollector.OnSessionIdReceived -= OnSessionIdReceived;
    }
    
    private void OnSessionIdReceived(string sessionId)
    {
        if (SessionText != null)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                string shortSessionId = sessionId.Length >= 8 ? sessionId.Substring(0, 8) : sessionId;
                SessionText.text = $"Número de sesión: {shortSessionId}";
            }
            else
            {
                SessionText.text = "No se pudo obtener el número de sesión";
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            Debug.Log("Player ha entrado en la zona final");
            // Buscar el componente Animator utilizando Find
            animatorPlayer.speed = 0;
            dialogScreen.SetActive(true);
            extractDataCollector.CalculateAverageCaptureTime();
            extractDataCollector.CalculateAveragePrecision();
            extractDataCollector.ExtractScores();
            extractDataCollector.ExtractResponseTimePlayerUp();

            // Detener temporizador de tarea activa y extraer el tiempo
            Inputs inputs = other.GetComponent<Inputs>();
            if (inputs != null)
            {
                inputs.StopActiveTaskTimer();
                extractDataCollector.ExtractActiveTaskTime();
                inputs.ShowDebugData();
                
                
            }
            extractDataCollector.SendDataToAPI();
        }
    }
}
