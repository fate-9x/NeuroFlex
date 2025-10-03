using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ExtractDataCollector : MonoBehaviour
{
    public APIManager apiManager;
    private List<float> captureTimes = new List<float>();
    private List<float> precisionValues = new List<float>();
    public static string lastSessionId = "";
    public static System.Action<string> OnSessionIdReceived;

    private void Awake()
    {
        if (apiManager == null)
        {
            GameObject utilsObj = GameObject.Find("Utils");
            if (utilsObj != null)
            {
                apiManager = utilsObj.GetComponent<APIManager>();
            }
        }
    }

    public void SetTiempoRespuestaPregunta(int index, float tiempo)
    {
        if (apiManager == null) return;
        var data = apiManager.data;

        switch (index)
        {
            case 1: data.TiempoRespuestaPregunta1 = tiempo; break;
            case 2: data.TiempoRespuestaPregunta2 = tiempo; break;
            case 3: data.TiempoRespuestaPregunta3 = tiempo; break;
        }
    }
    
    public void AddCaptureTime(float time)
    {
        captureTimes.Add(time);
        Debug.Log($"Tiempo de captura #{captureTimes.Count}: {time} segundos");
    }

    public void CalculateAverageCaptureTime()
    {
        if (captureTimes.Count > 0)
        {
            float average = captureTimes.Average();
            if (apiManager != null)
            {
                apiManager.data.TiempoCapturarNumero = average;
                Debug.Log($"Tiempo promedio de captura: {average} segundos (de {captureTimes.Count} números)");
            }
        }
        else
        {
            Debug.LogWarning("No hay tiempos de captura registrados para calcular el promedio");
        }
    }

    public void ExtractScores()
    {
        GameObject utilsObj = GameObject.Find("Utils");
        if (utilsObj != null && apiManager != null)
        {
            ScoreManager scoreManager = utilsObj.GetComponent<ScoreManager>();
            if (scoreManager != null)
            {
                apiManager.data.ObjetosInteractuadosCorrectamente = scoreManager.scoreNumbers;
                apiManager.data.CantAciertasTotales = scoreManager.scoreQuestions;
            }
        }
    }

    public void ExtractActiveTaskTime()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && apiManager != null)
        {
            Inputs inputs = playerObj.GetComponent<Inputs>();
            if (inputs != null)
            {
                apiManager.data.TiempoActivoTarea = inputs.tiempoActivoTarea;
            }
        }
    }
    public void ExtractTutorialTime()
    {
        GameObject subtitleManagerObj = GameObject.Find("SubtitleManager");
        if (subtitleManagerObj != null && apiManager != null)
        {
            SubtitleManager subtitleManager = subtitleManagerObj.GetComponent<SubtitleManager>();
            apiManager.data.TiempoTutorial = subtitleManager.GetTiempoTutorial();
        }
    }

    public void AddPrecision(float precision)
    {
        precisionValues.Add(precision);
        Debug.Log($"Precisión #{precisionValues.Count}: {precision}%");
    }

    public void CalculateAveragePrecision()
    {
        if (precisionValues.Count > 0)
        {
            float average = precisionValues.Average();
            if (apiManager != null)
            {
                apiManager.data.Precision = average;
                Debug.Log($"Precisión promedio: {average}%");
            }
        }
        else
        {
            Debug.LogWarning("No hay valores de precisión registrados para calcular el promedio");
        }
    }

    public void ExtractResponseTimePlayerUp()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && apiManager != null)
        {
            Inputs inputs = playerObj.GetComponent<Inputs>();
            if (inputs != null)
            {
                apiManager.data.TiempoRespuestaPararse = inputs.responseTimePlayerUp;
            }
        }
    }
    public void SendDataToAPI()
    {
        StartCoroutine(SendDataToAPIWithRetry(0));
    }

    private IEnumerator SendDataToAPIWithRetry(int attempt)
    {
        const int maxAttempts = 3;
        string apiData = apiManager.GetJsonData();
        Debug.Log($"Datos a enviar a la API: {apiData}");
        
        Debug.Log($"Intento {attempt + 1} de {maxAttempts} para enviar datos a la API");
        
        bool requestCompleted = false;
        bool requestSuccessful = false;
        string response = "";
        
        // Realizar la petición
        apiManager.SendJsonToAPI((result) => {
            requestCompleted = true;
            requestSuccessful = !result.StartsWith("Error:");
            response = result;
        });
        
        // Esperar a que la petición termine
        while (!requestCompleted)
        {
            yield return null;
        }
        
        if (requestSuccessful)
        {
            Debug.Log($"Datos enviados exitosamente a la API en el intento {attempt + 1}. Respuesta: {response}");
            
            // Intentar extraer el session_id de la respuesta
            string sessionId = ExtractSessionIdFromResponse(response);
            if (!string.IsNullOrEmpty(sessionId))
            {
                lastSessionId = sessionId;
                OnSessionIdReceived?.Invoke(sessionId);
                Debug.Log($"Session ID obtenido: {sessionId}");
            }
            else
            {
                OnSessionIdReceived?.Invoke("");
                Debug.LogWarning("No se pudo extraer el session_id de la respuesta");
            }
        }
        else
        {
            Debug.LogWarning($"Error en intento {attempt + 1}: {response}");
            
            if (attempt < maxAttempts - 1)
            {
                Debug.Log($"Reintentando en 2 segundos... ({attempt + 2}/{maxAttempts})");
                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(SendDataToAPIWithRetry(attempt + 1));
            }
            else
            {
                Debug.LogError($"Falló enviar datos a la API después de {maxAttempts} intentos. Último error: {response}");
                OnSessionIdReceived?.Invoke("");
            }
        }
    }

    private string ExtractSessionIdFromResponse(string response)
    {
        try
        {
            // Crear una clase simple para deserializar la respuesta JSON
            ApiResponse apiResponse = JsonUtility.FromJson<ApiResponse>(response);
            return apiResponse.session_id ?? "";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al parsear la respuesta de la API: {e.Message}");
            return "";
        }
    }

    [System.Serializable]
    public class ApiResponse
    {
        public string message;
        public string filename;
        public string date_folder;
        public string s3_key;
        public string session_id;
        public bool success;
    }
}
