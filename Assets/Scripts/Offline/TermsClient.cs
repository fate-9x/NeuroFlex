using System;
using System.Collections;
using UnityEngine;

public static class TermsClient
{
    public static IEnumerator GetTerms(string baseUrl, Action<bool, TermsData> onDone)
    {
        string url = baseUrl.TrimEnd('/') + "/api/lan/terms/current";
        bool done = false;
        int code = 0;
        string body = null;
        yield return ApiClient.GetJson(url, DeviceIdentity.DeviceToken, null, (c, b) => { code = c; body = b; done = true; });
        while (!done) yield return null;

        if (code != 200)
        {
            Debug.LogWarning($"[TermsClient] GetTerms error {code}: {body}");
            if (code == 401)
            {
                DeviceIdentity.ClearToken();
                Debug.LogWarning("[TermsClient] Device token rejected (401); token cleared so pairing can restart");
            }
            onDone?.Invoke(false, null);
            yield break;
        }

        TermsData data = null;
        try
        {
            data = JsonUtility.FromJson<TermsData>(body);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TermsClient] GetTerms invalid JSON: {e.Message}");
            onDone?.Invoke(false, null);
            yield break;
        }

        onDone?.Invoke(data != null && !string.IsNullOrEmpty(data.content), data);
    }

    public static IEnumerator AcceptTerms(string baseUrl, string versionId, string contentHash, Action<bool, string> onDone)
    {
        string url = baseUrl.TrimEnd('/') + "/api/lan/terms/accept";
        string json = JsonUtility.ToJson(new TermsAcceptRequest
        {
            version_id = versionId,
            content_hash = contentHash,
            application_version = Application.version
        });

        bool done = false;
        int code = 0;
        string body = null;
        yield return ApiClient.PostJson(url, json, DeviceIdentity.DeviceToken, null, (c, b) => { code = c; body = b; done = true; });
        while (!done) yield return null;

        if (code != 200)
        {
            Debug.LogWarning($"[TermsClient] AcceptTerms error {code}: {body}");
            if (code == 401)
            {
                DeviceIdentity.ClearToken();
                Debug.LogWarning("[TermsClient] Device token rejected (401); token cleared so pairing can restart");
            }
            onDone?.Invoke(false, null);
            yield break;
        }

        TermsAcceptResponse response = null;
        try
        {
            response = JsonUtility.FromJson<TermsAcceptResponse>(body);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TermsClient] AcceptTerms invalid JSON: {e.Message}");
            onDone?.Invoke(false, null);
            yield break;
        }

        onDone?.Invoke(response != null && !string.IsNullOrEmpty(response.acceptance_token), response?.acceptance_token);
    }
}
