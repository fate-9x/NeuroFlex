using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ExtractDataCollector : MonoBehaviour
{
    public APIManager apiManager;
    private List<float> captureTimes = new List<float>();
    private List<float> precisionValues = new List<float>();

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
                Debug.Log($"Tiempo promedio de captura: {average} segundos (de {captureTimes.Count} n\u00fameros)");
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
        Debug.Log($"Precisi\u00f3n #{precisionValues.Count}: {precision}%");
    }

    public void CalculateAveragePrecision()
    {
        if (precisionValues.Count > 0)
        {
            float average = precisionValues.Average();
            if (apiManager != null)
            {
                apiManager.data.Precision = average;
                Debug.Log($"Precisi\u00f3n promedio: {average}%");
            }
        }
        else
        {
            Debug.LogWarning("No hay valores de precisi\u00f3n registrados para calcular el promedio");
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

    public void SendDataToAPI(System.Action<bool> onComplete = null)
    {
        StartCoroutine(SendMetricsWithRetry(0, onComplete));
    }

    private IEnumerator SendMetricsWithRetry(int attempt, System.Action<bool> onComplete = null)
    {
        const int maxAttempts = 3;
        Debug.Log($"Intento {attempt + 1} de {maxAttempts} para enviar m\u00e9tricas a la API");

        bool requestCompleted = false;
        bool requestSuccessful = false;

        apiManager.SendMetrics((success) =>
        {
            requestCompleted = true;
            requestSuccessful = success;
        });

        while (!requestCompleted)
        {
            yield return null;
        }

        if (requestSuccessful)
        {
            Debug.Log($"M\u00e9tricas enviadas exitosamente en intento {attempt + 1}. Token consumido.");
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogWarning($"Error en intento {attempt + 1} de SendMetrics");
            if (attempt < maxAttempts - 1)
            {
                Debug.Log($"Reintentando en 2 segundos... ({attempt + 2}/{maxAttempts})");
                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(SendMetricsWithRetry(attempt + 1, onComplete));
            }
            else
            {
                apiManager.ClearMetricsToken();
                Debug.LogError($"Fall\u00f3 enviar m\u00e9tricas despu\u00e9s de {maxAttempts} intentos.");
                onComplete?.Invoke(false);
            }
        }
    }
}
