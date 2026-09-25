using System;
using System.Collections;
using UnityEngine;

public static class SessionClient
{
    public static IEnumerator CreateSession(string baseUrl, string acceptanceToken, Action<bool, string, string> onDone)
    {
        string url = baseUrl.TrimEnd('/') + "/api/lan/sessions";
        string json = JsonUtility.ToJson(new CreateSessionRequest
        {
            acceptance_token = acceptanceToken,
            patient_info = new PatientInfo { notes = "Sesión offline NeuroFlex" }
        });

        bool done = false;
        int code = 0;
        string body = null;
        yield return ApiClient.PostJson(url, json, DeviceIdentity.DeviceToken, null, (c, b) => { code = c; body = b; done = true; });
        while (!done) yield return null;

        if (code != 201)
        {
            Debug.LogWarning($"[SessionClient] CreateSession error {code}: {body}");
            if (code == 401)
            {
                DeviceIdentity.ClearToken();
            }
            onDone?.Invoke(false, null, null);
            yield break;
        }

        CreateSessionResponse response = null;
        try
        {
            response = JsonUtility.FromJson<CreateSessionResponse>(body);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SessionClient] CreateSession parse error: {ex.Message}");
            onDone?.Invoke(false, null, null);
            yield break;
        }

        onDone?.Invoke(
            response != null && !string.IsNullOrEmpty(response.session_id) && !string.IsNullOrEmpty(response.metrics_token),
            response?.session_id,
            response?.metrics_token);
    }

    public static IEnumerator PollSessionConfirmed(string baseUrl, string sessionId, string metricsToken, LanConfig config, Action<bool, string> onDone)
    {
        if (config.pollInterval <= 0)
        {
            Debug.LogError("[SessionClient] PollSessionConfirmed invalid config: pollInterval <= 0");
            onDone?.Invoke(false, "invalid config");
            yield break;
        }

        string url = baseUrl.TrimEnd('/') + "/api/lan/sessions/" + sessionId + "/status";
        float elapsed = 0f;
        while (elapsed < config.pairingTimeout)
        {
            bool done = false;
            int code = 0;
            string body = null;
            yield return ApiClient.GetJson(url, null, metricsToken, (c, b) => { code = c; body = b; done = true; });
            while (!done) yield return null;

            if (code == 401)
            {
                Debug.LogWarning($"[SessionClient] PollSessionConfirmed unauthorized (401): {body}");
                DeviceIdentity.ClearToken();
                onDone?.Invoke(false, "unauthorized");
                yield break;
            }

            if (code == 200 && !string.IsNullOrEmpty(body))
            {
                SessionStatusResponse status = null;
                try
                {
                    status = JsonUtility.FromJson<SessionStatusResponse>(body);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SessionClient] PollSessionConfirmed parse error: {ex.Message}");
                    onDone?.Invoke(false, "parse error");
                    yield break;
                }

                if (status != null)
                {
                    if (status.status == "confirmed")
                    {
                        onDone?.Invoke(true, status.status);
                        yield break;
                    }
                    if (status.status == "error")
                    {
                        onDone?.Invoke(false, status.status);
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(config.pollInterval);
            elapsed += config.pollInterval;
        }

        onDone?.Invoke(false, "timeout");
    }

    public static IEnumerator SendMetrics(string baseUrl, string sessionId, string metricsToken, string jsonMetrics, Action<bool> onDone)
    {
        OfflineMetrics metrics = null;
        try
        {
            metrics = JsonUtility.FromJson<OfflineMetrics>(jsonMetrics);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SessionClient] SendMetrics metrics parse error: {ex.Message}");
            onDone?.Invoke(false);
            yield break;
        }

        string json = JsonUtility.ToJson(metrics);
        string url = baseUrl.TrimEnd('/') + "/api/lan/sessions/" + sessionId + "/metrics";
        bool done = false;
        int code = 0;
        string body = null;
        yield return ApiClient.PostJson(url, json, null, metricsToken, (c, b) => { code = c; body = b; done = true; });
        while (!done) yield return null;

        if (code != 200)
        {
            Debug.LogWarning($"[SessionClient] SendMetrics error {code}: {body}");
            if (code == 401)
            {
                DeviceIdentity.ClearToken();
            }
        }
        onDone?.Invoke(code == 200);
    }
}
