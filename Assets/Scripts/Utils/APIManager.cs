using System.Collections;
using System.Security.Cryptography;
using System.Text;
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
    public int TtlSeconds { get; private set; }
    public string AcceptanceToken { get; set; }
    public bool SessionActive => !string.IsNullOrEmpty(MetricsToken);

    public void CreateSession(string acceptanceToken, Action<bool> onSuccess)
    {
        if (string.IsNullOrEmpty(acceptanceToken))
        {
            Debug.LogWarning("CreateSession: acceptanceToken vac\u00edo");
            onSuccess?.Invoke(false);
            return;
        }
        StartCoroutine(CreateSessionRequest(acceptanceToken, onSuccess));
    }

    private IEnumerator CreateSessionRequest(string acceptanceToken, Action<bool> onSuccess)
    {
        LastCreateSessionUnauthorized = false;

        if (!ValidateConfig()) { onSuccess?.Invoke(false); yield break; }

        SessionRequest body = new SessionRequest { acceptance_token = acceptanceToken };
        string jsonBody = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        string url = apiConfig.baseUrl.TrimEnd('/') + "/sessions";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode == 401 || request.responseCode == 403)
            {
                LastCreateSessionUnauthorized = true;
            }
            Debug.LogWarning($"CreateSession error: {request.error} (code {request.responseCode})");
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
        TtlSeconds = response.ttl_seconds > 0 ? response.ttl_seconds : 600;
        LastCreateSessionUnauthorized = false;
        onSuccess?.Invoke(true);
        request.Dispose();
    }

    public void GetCurrentTerms(Action<bool, TermsData> onResult)
    {
        StartCoroutine(GetCurrentTermsRequest(0, onResult));
    }

    private IEnumerator GetCurrentTermsRequest(int attempt, Action<bool, TermsData> onResult)
    {
        if (!ValidateConfig()) { onResult?.Invoke(false, null); yield break; }

        string url = apiConfig.baseUrl.TrimEnd('/') + "/terms/current";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Accept-Language", "es");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"GetCurrentTerms network error: {request.error}");
            if (attempt < 2)
            {
                request.Dispose();
                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(GetCurrentTermsRequest(attempt + 1, onResult));
                yield break;
            }
            onResult?.Invoke(false, null);
            request.Dispose();
            yield break;
        }

        TermsData data = JsonUtility.FromJson<TermsData>(request.downloadHandler.text);
        request.Dispose();

        if (data == null || string.IsNullOrEmpty(data.content) || string.IsNullOrEmpty(data.content_hash))
        {
            Debug.LogWarning("GetCurrentTerms: parse error o campos vac\u00edos");
            if (attempt < 2)
            {
                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(GetCurrentTermsRequest(attempt + 1, onResult));
                yield break;
            }
            onResult?.Invoke(false, null);
            yield break;
        }

        // Verificaci\u00f3n SHA-256
        string computedHash = ComputeSha256(data.content);
        if (!string.Equals(computedHash, data.content_hash, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"GetCurrentTerms hash mismatch. computed={computedHash} expected={data.content_hash}");
            if (attempt < 2)
            {
                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(GetCurrentTermsRequest(attempt + 1, onResult));
                yield break;
            }
            onResult?.Invoke(false, null);
            yield break;
        }

        onResult?.Invoke(true, data);
    }

    private static string ComputeSha256(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(bytes);
            System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    public void AcceptTerms(string versionId, string contentHash, Action<bool, string> onResult)
    {
        StartCoroutine(AcceptTermsRequest(versionId, contentHash, 0, onResult));
    }

    private IEnumerator AcceptTermsRequest(string versionId, string contentHash, int attempt, Action<bool, string> onResult)
    {
        if (!ValidateConfig()) { onResult?.Invoke(false, null); yield break; }

        AcceptRequest body = new AcceptRequest
        {
            version_id = versionId,
            content_hash = contentHash,
            application_version = Application.version
        };
        string jsonBody = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        string url = apiConfig.baseUrl.TrimEnd('/') + "/terms/accept";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept-Language", "es");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"AcceptTerms error: {request.error} (code {request.responseCode})");
            if (attempt < 2)
            {
                request.Dispose();
                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(AcceptTermsRequest(versionId, contentHash, attempt + 1, onResult));
                yield break;
            }
            onResult?.Invoke(false, null);
            request.Dispose();
            yield break;
        }

        string responseText = request.downloadHandler.text;
        request.Dispose();

        AcceptResponse response = JsonUtility.FromJson<AcceptResponse>(responseText);
        if (response == null || string.IsNullOrEmpty(response.acceptance_token))
        {
            Debug.LogWarning($"AcceptTerms parse error: {responseText}");
            if (attempt < 2)
            {
                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(AcceptTermsRequest(versionId, contentHash, attempt + 1, onResult));
                yield break;
            }
            onResult?.Invoke(false, null);
            yield break;
        }

        AcceptanceToken = response.acceptance_token;
        onResult?.Invoke(true, response.acceptance_token);
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
        Debug.Log($"[APIManager] GetSessionStatus URL: {url}");
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("X-Metrics-Token", MetricsToken);
        yield return request.SendWebRequest();

        Debug.Log($"[APIManager] GetSessionStatus responseCode={request.responseCode}, result={request.result}, error={request.error}");

        if (request.responseCode == 401 || request.responseCode == 403)
        {
            Debug.LogWarning($"GetSessionStatus auth error ({(int)request.responseCode})");
            onStatus?.Invoke("unauthorized");
            request.Dispose();
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"GetSessionStatus error: {request.error}");
            onStatus?.Invoke(null);
            request.Dispose();
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log($"[APIManager] GetSessionStatus raw response: {responseText}");
        SessionStatusResponse response = JsonUtility.FromJson<SessionStatusResponse>(responseText);
        Debug.Log($"[APIManager] GetSessionStatus parsed status: {(response == null ? "<parse null>" : response.status)}");
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

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(true);
            MetricsToken = null;
        }
        else
        {
            Debug.LogWarning($"SendMetrics error: {request.error}");
            onSuccess?.Invoke(false);
        }

        request.Dispose();
    }

    public void ClearMetricsToken()
    {
        MetricsToken = null;
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
    public class TermsData
    {
        public string version_id;
        public string language;
        public string title;
        public string content;
        public string content_hash;
        public string created_at;
    }

    [System.Serializable]
    private class AcceptRequest
    {
        public string version_id;
        public string content_hash;
        public string application_version;
    }

    [System.Serializable]
    private class AcceptResponse
    {
        public string acceptance_token;
        public string expires_at;
    }

    [System.Serializable]
    private class SessionRequest
    {
        public string acceptance_token;
    }

    [System.Serializable]
    private class SessionResponse
    {
        public string session_id;
        public string display_code;
        public string metrics_token;
        public int ttl_seconds;
    }

    [System.Serializable]
    private class SessionStatusResponse
    {
        public string status;
    }

    public bool LastCreateSessionUnauthorized { get; private set; }

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
        public float TiempoReaccionVisual;
    }
}
