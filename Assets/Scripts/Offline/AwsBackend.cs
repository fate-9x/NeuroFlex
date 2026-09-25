using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AwsBackend : IBackend
{
    private readonly ApiConfig _apiConfig;
    private readonly APIManager _apiManager;

    public AwsBackend(ApiConfig config, APIManager manager)
    {
        _apiConfig = config;
        _apiManager = manager;
    }

    public string GetTermsText() => "Términos y condiciones";

    public IEnumerator GetCurrentTerms(Action<bool, APIManager.TermsData> onResult)
    {
        yield return GetCurrentTermsRequest(0, onResult);
    }

    private IEnumerator GetCurrentTermsRequest(int attempt, Action<bool, APIManager.TermsData> onResult)
    {
        if (!ValidateConfig()) { onResult?.Invoke(false, null); yield break; }

        string url = _apiConfig.baseUrl.TrimEnd('/') + "/terms/current";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Accept-Language", "es");
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[AwsBackend] GetCurrentTerms error: {request.error}");
            if (attempt < 2)
            {
                request.Dispose();
                yield return new WaitForSeconds(2f);
                yield return GetCurrentTermsRequest(attempt + 1, onResult);
                yield break;
            }
            onResult?.Invoke(false, null);
            request.Dispose();
            yield break;
        }

        APIManager.TermsData data = JsonUtility.FromJson<APIManager.TermsData>(request.downloadHandler.text);
        request.Dispose();

        if (data == null || string.IsNullOrEmpty(data.content) || string.IsNullOrEmpty(data.content_hash))
        {
            if (attempt < 2)
            {
                yield return new WaitForSeconds(2f);
                yield return GetCurrentTermsRequest(attempt + 1, onResult);
                yield break;
            }
            onResult?.Invoke(false, null);
            yield break;
        }

        string computedHash = ComputeSha256(data.content);
        if (!string.Equals(computedHash, data.content_hash, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[AwsBackend] hash mismatch");
            if (attempt < 2)
            {
                yield return new WaitForSeconds(2f);
                yield return GetCurrentTermsRequest(attempt + 1, onResult);
                yield break;
            }
            onResult?.Invoke(false, null);
            yield break;
        }

        onResult?.Invoke(true, data);
    }

    public IEnumerator AcceptTerms(string versionId, string contentHash, Action<bool, string> onResult)
    {
        yield return AcceptTermsRequest(versionId, contentHash, 0, onResult);
    }

    private IEnumerator AcceptTermsRequest(string versionId, string contentHash, int attempt, Action<bool, string> onResult)
    {
        if (!ValidateConfig()) { onResult?.Invoke(false, null); yield break; }

        var body = new AwsAcceptRequest
        {
            version_id = versionId,
            content_hash = contentHash,
            application_version = Application.version
        };
        string jsonBody = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        string url = _apiConfig.baseUrl.TrimEnd('/') + "/terms/accept";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept-Language", "es");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (attempt < 2)
            {
                request.Dispose();
                yield return new WaitForSeconds(2f);
                yield return AcceptTermsRequest(versionId, contentHash, attempt + 1, onResult);
                yield break;
            }
            onResult?.Invoke(false, null);
            request.Dispose();
            yield break;
        }

        AwsAcceptResponse response = JsonUtility.FromJson<AwsAcceptResponse>(request.downloadHandler.text);
        request.Dispose();

        if (response == null || string.IsNullOrEmpty(response.acceptance_token))
        {
            if (attempt < 2)
            {
                yield return new WaitForSeconds(2f);
                yield return AcceptTermsRequest(versionId, contentHash, attempt + 1, onResult);
                yield break;
            }
            onResult?.Invoke(false, null);
            yield break;
        }

        _apiManager.AcceptanceToken = response.acceptance_token;
        onResult?.Invoke(true, response.acceptance_token);
    }

    public IEnumerator CreateSession(string acceptanceToken, Action<bool> onSuccess)
    {
        yield return CreateSessionRequest(acceptanceToken, onSuccess);
    }

    private IEnumerator CreateSessionRequest(string acceptanceToken, Action<bool> onSuccess)
    {
        _apiManager.LastCreateSessionUnauthorized = false;
        if (!ValidateConfig()) { onSuccess?.Invoke(false); yield break; }

        var body = new AwsSessionRequest { acceptance_token = acceptanceToken };
        string jsonBody = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        string url = _apiConfig.baseUrl.TrimEnd('/') + "/sessions";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode == 401 || request.responseCode == 403)
                _apiManager.LastCreateSessionUnauthorized = true;
            onSuccess?.Invoke(false);
            request.Dispose();
            yield break;
        }

        AwsSessionResponse response = JsonUtility.FromJson<AwsSessionResponse>(request.downloadHandler.text);
        if (response == null || string.IsNullOrEmpty(response.session_id) ||
            string.IsNullOrEmpty(response.metrics_token) || string.IsNullOrEmpty(response.display_code))
        {
            onSuccess?.Invoke(false);
            request.Dispose();
            yield break;
        }

        _apiManager.SessionId = response.session_id;
        _apiManager.MetricsToken = response.metrics_token;
        _apiManager.DisplayCode = response.display_code;
        _apiManager.TtlSeconds = response.ttl_seconds > 0 ? response.ttl_seconds : 600;
        _apiManager.LastCreateSessionUnauthorized = false;
        onSuccess?.Invoke(true);
        request.Dispose();
    }

    public IEnumerator GetSessionStatus(Action<string> onStatus)
    {
        if (!ValidateConfig()) { onStatus?.Invoke(null); yield break; }
        if (string.IsNullOrEmpty(_apiManager.SessionId) || string.IsNullOrEmpty(_apiManager.MetricsToken))
        {
            onStatus?.Invoke(null);
            yield break;
        }

        string url = _apiConfig.baseUrl.TrimEnd('/') + "/sessions/" + UnityWebRequest.EscapeURL(_apiManager.SessionId);
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("X-Metrics-Token", _apiManager.MetricsToken);
        yield return request.SendWebRequest();

        if (request.responseCode == 401 || request.responseCode == 403)
        {
            onStatus?.Invoke("unauthorized");
            request.Dispose();
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            onStatus?.Invoke(null);
            request.Dispose();
            yield break;
        }

        AwsSessionStatusResponse response = JsonUtility.FromJson<AwsSessionStatusResponse>(request.downloadHandler.text);
        onStatus?.Invoke(response?.status);
        request.Dispose();
    }

    public IEnumerator SendMetrics(string jsonData, Action<bool> onSuccess)
    {
        if (!ValidateConfig()) { onSuccess?.Invoke(false); _apiManager.MetricsToken = null; yield break; }
        if (string.IsNullOrEmpty(_apiManager.SessionId) || string.IsNullOrEmpty(_apiManager.MetricsToken))
        {
            onSuccess?.Invoke(false);
            yield break;
        }

        string url = _apiConfig.baseUrl.TrimEnd('/') + "/sessions/" + UnityWebRequest.EscapeURL(_apiManager.SessionId) + "/metrics";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("X-Metrics-Token", _apiManager.MetricsToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(true);
            _apiManager.MetricsToken = null;
        }
        else
        {
            onSuccess?.Invoke(false);
        }
        request.Dispose();
    }

    private bool ValidateConfig()
    {
        if (_apiConfig == null || string.IsNullOrEmpty(_apiConfig.baseUrl))
        {
            Debug.LogError("[AwsBackend] ApiConfig no asignado o baseUrl vacío");
            return false;
        }
        return true;
    }

    private static string ComputeSha256(string content)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder(64);
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }

    [System.Serializable]
    private class AwsAcceptRequest
    {
        public string version_id;
        public string content_hash;
        public string application_version;
    }

    [System.Serializable]
    private class AwsAcceptResponse
    {
        public string acceptance_token;
        public string expires_at;
    }

    [System.Serializable]
    private class AwsSessionRequest
    {
        public string acceptance_token;
    }

    [System.Serializable]
    private class AwsSessionResponse
    {
        public string session_id;
        public string display_code;
        public string metrics_token;
        public int ttl_seconds;
    }

    [System.Serializable]
    private class AwsSessionStatusResponse
    {
        public string status;
    }
}
