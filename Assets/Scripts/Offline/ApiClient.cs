using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class ApiClient
{
    public static IEnumerator GetJson(string url, string bearer, string metricsToken, Action<int, string> onDone)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(bearer))
                req.SetRequestHeader("Authorization", "Bearer " + bearer);
            if (!string.IsNullOrEmpty(metricsToken))
                req.SetRequestHeader("X-Metrics-Token", metricsToken);

            yield return req.SendWebRequest();

            int code = (int)req.responseCode;
            string body = req.downloadHandler?.text;
            onDone?.Invoke(code, body);
        }
    }

    public static IEnumerator PostJson(string url, string json, string bearer, string metricsToken, Action<int, string> onDone)
    {
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json ?? "{}");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(bearer))
                req.SetRequestHeader("Authorization", "Bearer " + bearer);
            if (!string.IsNullOrEmpty(metricsToken))
                req.SetRequestHeader("X-Metrics-Token", metricsToken);

            yield return req.SendWebRequest();

            int code = (int)req.responseCode;
            string body = req.downloadHandler?.text;
            onDone?.Invoke(code, body);
        }
    }
}
