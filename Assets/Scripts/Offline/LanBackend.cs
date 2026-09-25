using System;
using System.Collections;
using UnityEngine;

public class LanBackend : IBackend
{
    private readonly LanConfig _config;
    private readonly APIManager _apiManager;
    private string _baseUrl;
    private string _sessionId;
    private string _metricsToken;
    private string _acceptanceToken;
    private APIManager.TermsData _currentTerms;

    public string CurrentPin { get; private set; }
    public string BaseUrl => _baseUrl;

    public LanBackend(LanConfig config, APIManager manager)
    {
        _config = config;
        _apiManager = manager;
        if (_config == null)
        {
            Debug.LogError("[LanBackend] LanConfig is null; LAN backend cannot discover or reach the dashboard");
        }
    }

    public string GetTermsText() => _currentTerms?.content ?? "Cargando términos...";

    public IEnumerator GetCurrentTerms(Action<bool, APIManager.TermsData> onResult)
    {
        yield return EnsureDashboardUrl();
        if (string.IsNullOrEmpty(_baseUrl))
        {
            onResult?.Invoke(false, null);
            yield break;
        }

        bool done = false;
        bool ok = false;
        TermsData data = null;
        yield return TermsClient.GetTerms(_baseUrl, (success, d) =>
        {
            ok = success;
            data = d;
            done = true;
        });
        while (!done) yield return null;

        if (ok && data != null)
        {
            _currentTerms = new APIManager.TermsData
            {
                version_id = data.version_id,
                title = data.title,
                content = data.content,
                content_hash = data.content_hash,
                language = data.language,
                created_at = data.created_at
            };
        }
        onResult?.Invoke(ok, _currentTerms);
    }

    public IEnumerator AcceptTerms(string versionId, string contentHash, Action<bool, string> onResult)
    {
        yield return EnsureDashboardUrl();
        if (string.IsNullOrEmpty(_baseUrl))
        {
            onResult?.Invoke(false, null);
            yield break;
        }

        bool done = false;
        bool ok = false;
        string token = null;
        yield return TermsClient.AcceptTerms(_baseUrl, versionId, contentHash, (success, t) =>
        {
            ok = success;
            token = t;
            done = true;
        });
        while (!done) yield return null;

        _acceptanceToken = token;
        _apiManager.AcceptanceToken = token;
        onResult?.Invoke(ok, token);
    }

    public IEnumerator CreateSession(string acceptanceToken, Action<bool> onSuccess)
    {
        yield return EnsureDashboardUrl();
        if (string.IsNullOrEmpty(_baseUrl))
        {
            onSuccess?.Invoke(false);
            yield break;
        }

        bool done = false;
        bool ok = false;
        string sid = null;
        string mtoken = null;
        yield return SessionClient.CreateSession(_baseUrl, acceptanceToken, (success, id, token) =>
        {
            ok = success;
            sid = id;
            mtoken = token;
            done = true;
        });
        while (!done) yield return null;

        if (ok)
        {
            _sessionId = sid;
            _metricsToken = mtoken;
            _apiManager.SessionId = sid;
            _apiManager.MetricsToken = mtoken;
            _apiManager.DisplayCode = CurrentPin ?? "LAN";
            _apiManager.TtlSeconds = (int)_config.pairingTimeout;
        }
        onSuccess?.Invoke(ok);
    }

    public IEnumerator GetSessionStatus(Action<string> onStatus)
    {
        yield return EnsureDashboardUrl();
        if (string.IsNullOrEmpty(_baseUrl) || string.IsNullOrEmpty(_sessionId) || string.IsNullOrEmpty(_metricsToken))
        {
            onStatus?.Invoke(null);
            yield break;
        }

        bool done = false;
        bool ok = false;
        string status = null;
        yield return SessionClient.PollSessionConfirmed(_baseUrl, _sessionId, _metricsToken, _config, (success, s) =>
        {
            ok = success;
            status = s;
            done = true;
        });
        while (!done) yield return null;

        onStatus?.Invoke(status);
    }

    public IEnumerator SendMetrics(string jsonData, Action<bool> onSuccess)
    {
        // Filter the payload down to the 11 keys of the LAN dashboard contract
        // (APIManager.ExtractData carries 12 fields; TiempoReaccionVisual is not part of the contract).
        OfflineMetrics metrics = null;
        try
        {
            metrics = JsonUtility.FromJson<OfflineMetrics>(jsonData);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LanBackend] SendMetrics metrics parse error: {ex.Message}");
            onSuccess?.Invoke(false);
            yield break;
        }
        if (metrics == null)
        {
            // JsonUtility.FromJson returns null (instead of throwing) for null/empty input.
            Debug.LogWarning("[LanBackend] SendMetrics received an empty metrics payload");
            onSuccess?.Invoke(false);
            yield break;
        }
        string filteredJson = JsonUtility.ToJson(metrics);

        yield return EnsureDashboardUrl();
        if (string.IsNullOrEmpty(_baseUrl) || string.IsNullOrEmpty(_sessionId) || string.IsNullOrEmpty(_metricsToken))
        {
            onSuccess?.Invoke(false);
            yield break;
        }

        bool done = false;
        bool ok = false;
        yield return SessionClient.SendMetrics(_baseUrl, _sessionId, _metricsToken, filteredJson, success =>
        {
            ok = success;
            done = true;
        });
        while (!done) yield return null;

        if (ok)
        {
            _apiManager.MetricsToken = null;
            _metricsToken = null;
        }
        onSuccess?.Invoke(ok);
    }

    public IEnumerator Pair(Action<PairingClient.PairingResult> onDone)
    {
        yield return EnsureDashboardUrl();
        if (string.IsNullOrEmpty(_baseUrl))
        {
            onDone?.Invoke(new PairingClient.PairingResult { success = false, error = "No se encontró dashboard" });
            yield break;
        }

        bool done = false;
        PairingClient.PairingResult result = null;
        yield return PairingClient.RequestPairing(_baseUrl, _config, r =>
        {
            result = r;
            done = true;
        });
        while (!done) yield return null;

        if (result != null && !string.IsNullOrEmpty(result.pin))
        {
            CurrentPin = result.pin;
        }
        onDone?.Invoke(result);
    }

    private IEnumerator EnsureDashboardUrl()
    {
        if (!string.IsNullOrEmpty(_baseUrl)) yield break;

        if (_config == null)
        {
            Debug.LogError("[LanBackend] Cannot discover dashboard: LanConfig is null");
            yield break;
        }

        bool done = false;
        bool found = false;
        string url = null;
        DiscoveryService.DiscoverDashboard(_config, (ok, u) =>
        {
            found = ok;
            url = u;
            done = true;
        });
        while (!done) yield return null;

        if (found)
        {
            _baseUrl = url;
            Debug.Log($"[LanBackend] Dashboard found at {url}");
        }
        else
        {
            Debug.LogWarning("[LanBackend] Dashboard not found");
        }
    }
}
