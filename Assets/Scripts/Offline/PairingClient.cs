using System;
using System.Collections;
using UnityEngine;

public static class PairingClient
{
    [System.Serializable]
    public class PairingResult
    {
        public bool success;
        public string pin;
        public string error;
        public string deviceToken;
    }

    [System.Serializable]
    public class PairingStartResult
    {
        public bool success;
        public string pin;
        public string pairing_id;
        public string error;
    }

    public static IEnumerator StartPairing(string baseUrl, Action<PairingStartResult> onDone)
    {
        string pin = GeneratePin();
        string requestUrl = baseUrl.TrimEnd('/') + "/api/lan/pair/request";
        string json = JsonUtility.ToJson(new PairRequest
        {
            device_id = DeviceIdentity.DeviceId,
            device_name = FormatDeviceName(),
            pin = pin
        });

        bool done = false;
        int code = 0;
        string body = null;
        yield return ApiClient.PostJson(requestUrl, json, null, null, (c, b) => { code = c; body = b; done = true; });
        while (!done) yield return null;

        if (code == 409)
        {
            onDone?.Invoke(new PairingStartResult { success = false, pin = pin, error = "Ya hay una solicitud de emparejamiento activa. Cancelela en el dashboard." });
            yield break;
        }

        if (code != 201)
        {
            onDone?.Invoke(new PairingStartResult { success = false, pin = pin, error = $"Error {code}: {body}" });
            yield break;
        }

        PairResponse pairResponse = null;
        try
        {
            pairResponse = JsonUtility.FromJson<PairResponse>(body);
        }
        catch (Exception e)
        {
            onDone?.Invoke(new PairingStartResult { success = false, pin = pin, error = "Respuesta de emparejamiento inválida: " + e.Message });
            yield break;
        }

        if (pairResponse == null || string.IsNullOrEmpty(pairResponse.pairing_id))
        {
            onDone?.Invoke(new PairingStartResult { success = false, pin = pin, error = "Respuesta de emparejamiento inválida" });
            yield break;
        }

        onDone?.Invoke(new PairingStartResult { success = true, pin = pin, pairing_id = pairResponse.pairing_id });
    }

    public static IEnumerator PollPairing(string baseUrl, string pairingId, string pin, LanConfig config, Action<PairingResult> onDone)
    {
        if (config.pairingTimeout <= 0f || config.pollInterval <= 0f)
        {
            onDone?.Invoke(new PairingResult { success = false, pin = pin, error = "Configuración inválida: pairingTimeout y pollInterval deben ser mayores que cero" });
            yield break;
        }

        string statusUrl = baseUrl.TrimEnd('/') + "/api/lan/pair/status/" + pairingId;
        float elapsed = 0f;
        while (elapsed < config.pairingTimeout)
        {
            bool done = false;
            int code = 0;
            string body = null;
            yield return ApiClient.GetJson(statusUrl, null, null, (c, b) => { code = c; body = b; done = true; });
            while (!done) yield return null;

            PairStatus status = null;
            if (code == 200 && !string.IsNullOrEmpty(body))
            {
                try
                {
                    status = JsonUtility.FromJson<PairStatus>(body);
                }
                catch (Exception e)
                {
                    onDone?.Invoke(new PairingResult { success = false, pin = pin, error = "Respuesta de estado inválida: " + e.Message });
                    yield break;
                }
            }

            if (status != null)
            {
                if (status.status == "confirmed")
                {
                    if (string.IsNullOrEmpty(status.device_token))
                    {
                        onDone?.Invoke(new PairingResult { success = false, pin = pin, error = "Token de dispositivo vacío" });
                        yield break;
                    }
                    DeviceIdentity.DeviceToken = status.device_token;
                    onDone?.Invoke(new PairingResult { success = true, pin = pin, deviceToken = status.device_token });
                    yield break;
                }
                if (status.status == "expired")
                {
                    onDone?.Invoke(new PairingResult { success = false, pin = pin, error = "PIN expirado" });
                    yield break;
                }
            }

            yield return new WaitForSeconds(config.pollInterval);
            elapsed += config.pollInterval;
        }

        onDone?.Invoke(new PairingResult { success = false, pin = pin, error = "Tiempo de emparejamiento agotado" });
    }

    public static string GeneratePin()
    {
        return UnityEngine.Random.Range(100000, 999999).ToString();
    }

    private static string FormatDeviceName()
    {
        string name = "Quest-" + SystemInfo.deviceName;
        if (name.Length > 100)
            name = name.Substring(0, 100);
        return name;
    }
}
