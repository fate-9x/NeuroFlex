using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class APIManager : MonoBehaviour
{
    [SerializeField] private ApiConfig apiConfig;
    public ExtractData data = new ExtractData();

    public string SessionId { get; private set; }
    public string MetricsToken { get; private set; }
    public string DisplayCode { get; private set; }
    public bool SessionActive => !string.IsNullOrEmpty(MetricsToken);

    public void CreateSession(Action<bool> onSuccess)
    {
        StartCoroutine(CreateSessionRequest(onSuccess));
    }

    private IEnumerator CreateSessionRequest(Action<bool> onSuccess)
    {
        if (!ValidateConfig()) { onSuccess?.Invoke(false); yield break; }

        string url = apiConfig.baseUrl.TrimEnd('/') + "/sessions";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"CreateSession error: {request.error}");
            onSuccess?.Invoke(false);
            request.Dispose();
            yield break;
        }

        SessionResponse response = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);
        if (response == null || string.IsNullOrEmpty(response.session_id) ||
            string.IsNullOrEmpty(response.metrics_token) || string.IsNullOrEmpty(response.display_code))
        {
            Debug.LogWarning($"CreateSession parse error: {request.downloadHandler.text}");
            onSuccess?.Invoke(false);
            request.Dispose();
            yield break;
        }

        SessionId = response.session_id;
        MetricsToken = response.metrics_token;
        DisplayCode = response.display_code;
        onSuccess?.Invoke(true);
        request.Dispose();
    }

    public void GetSessionStatus(Action<string> onStatus)
    {
        StartCoroutine(GetSessionStatusRequest(onStatus));
    }

    private IEnumerator GetSessionStatusRequest(Action<string> onStatus)
    {
        if (!ValidateConfig()) { onStatus?.Invoke(null); yield break; }
        if (string.IsNullOrEmpty(SessionId) || string.IsNullOrEmpty(MetricsToken))
        {
            Debug.LogWarning("GetSessionStatus: sin sesi\u00f3n activa");
            onStatus?.Invoke(null);
            yield break;
        }

        string url = apiConfig.baseUrl.TrimEnd('/') + "/sessions/" + UnityWebRequest.EscapeURL(SessionId);
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("X-Metrics-Token", MetricsToken);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"GetSessionStatus error: {request.error}");
            onStatus?.Invoke(null);
            request.Dispose();
            yield break;
        }

        SessionStatusResponse response = JsonUtility.FromJson<SessionStatusResponse>(request.downloadHandler.text);
        onStatus?.Invoke(response == null ? null : response.status);
        request.Dispose();
    }

    public void SendMetrics(Action<bool> onSuccess)
    {
        StartCoroutine(SendMetricsRequest(onSuccess));
    }

    private IEnumerator SendMetricsRequest(Action<bool> onSuccess)
    {
        if (!ValidateConfig()) { onSuccess?.Invoke(false); MetricsToken = null; yield break; }
        if (string.IsNullOrEmpty(SessionId) || string.IsNullOrEmpty(MetricsToken))
        {
            Debug.LogWarning("SendMetrics: sin sesi\u00f3n activa o token");
            onSuccess?.Invoke(false);
            yield break;
        }

        string url = apiConfig.baseUrl.TrimEnd('/') + "/sessions/" + UnityWebRequest.EscapeURL(SessionId) + "/metrics";
        string jsonData = GetJsonData();
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("X-Metrics-Token", MetricsToken);

        yield return request.SendWebRequest();

        MetricsToken = null;

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(true);
        }
        else
        {
            Debug.LogWarning($"SendMetrics error: {request.error}");
            onSuccess?.Invoke(false);
        }

        request.Dispose();
    }

    public string GetJsonData()
    {
        return JsonUtility.ToJson(data);
    }

    private bool ValidateConfig()
    {
        if (apiConfig == null || string.IsNullOrEmpty(apiConfig.baseUrl))
        {
            Debug.LogError("[APIManager] ApiConfig no asignado o baseUrl vac\u00edo en Utils.prefab");
            return false;
        }
        return true;
    }

    [System.Serializable]
    private class SessionResponse
    {
        public string session_id;
        public string display_code;
        public string metrics_token;
        public string status;
        public int ttl_seconds;
    }

    [System.Serializable]
    private class SessionStatusResponse
    {
        public string status;
    }

    [System.Serializable]
    public class ExtractData
    {
        public float TiempoRespuestaPararse;
        public float Precision;
        public float TiempoActivoTarea;
        public int CantAciertasTotales;
        public int ObjetosInteractuadosCorrectamente;
        public float TiempoRespuestaPregunta1;
        public float TiempoRespuestaPregunta2;
        public float TiempoRespuestaPregunta3;
        public float TiempoCapturarNumero;
        public float TiempoTutorial;
        public int TipoEscena;
    }
}
