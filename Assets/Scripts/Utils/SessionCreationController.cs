using System.Collections;
using UnityEngine;
using TMPro;

public class SessionCreationController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayCodeText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private SceneController sceneController;

    private const float PollInterval = 5f;
    private const int DefaultTtlSeconds = 600;
    private const int MaxCreateAttempts = 3;
    private const float RetryDelay = 2f;

    private APIManager apiManager;
    private float sessionStartWallTime;
    private int ttlSeconds = DefaultTtlSeconds;
    private int consecutivePollFailures = 0;

    private void Start()
    {
        GameObject utilsObj = GameObject.Find("Utils");
        if (utilsObj != null)
        {
            apiManager = utilsObj.GetComponent<APIManager>();
        }

        if (apiManager == null)
        {
            SetError("No se encontr\u00f3 APIManager en Utils.");
            return;
        }

        if (statusText != null) statusText.text = "Creando sesi\u00f3n...";
        if (errorText != null) errorText.text = "";
        if (displayCodeText != null) displayCodeText.text = "";

        StartCoroutine(CreateSessionWithRetry(0));
    }

    private IEnumerator CreateSessionWithRetry(int attempt)
    {
        if (apiManager == null) yield break;

        bool completed = false;
        bool success = false;
        apiManager.CreateSession(s =>
        {
            completed = true;
            success = s;
        });

        while (!completed) yield return null;

        if (success)
        {
            if (displayCodeText != null && !string.IsNullOrEmpty(apiManager.DisplayCode))
            {
                displayCodeText.text = apiManager.DisplayCode;
            }
            if (statusText != null) statusText.text = "Esperando confirmaci\u00f3n del operador...";
            sessionStartWallTime = Time.realtimeSinceStartup;
            ttlSeconds = apiManager.TtlSeconds > 0 ? apiManager.TtlSeconds : DefaultTtlSeconds;
            consecutivePollFailures = 0;
            yield return StartCoroutine(PollStatus());
        }
        else
        {
            if (attempt < MaxCreateAttempts - 1)
            {
                Debug.Log($"Reintentando CreateSession en {RetryDelay}s ({attempt + 2}/{MaxCreateAttempts})");
                yield return new WaitForSeconds(RetryDelay);
                yield return StartCoroutine(CreateSessionWithRetry(attempt + 1));
            }
            else
            {
                SetError("Error al crear sesi\u00f3n. Reinicie la aplicaci\u00f3n.");
            }
        }
    }

    private IEnumerator PollStatus()
    {
        while (true)
        {
            yield return new WaitForSeconds(PollInterval);

            if (Time.realtimeSinceStartup - sessionStartWallTime >= ttlSeconds)
            {
                SetError("Sesi\u00f3n expirada. El operador no confirm\u00f3. Reinicie la app.");
                yield break;
            }

            bool completed = false;
            string status = null;
            apiManager.GetSessionStatus(s =>
            {
                completed = true;
                status = s;
            });

            while (!completed) yield return null;

            if (status == "unauthorized")
            {
                SetError("Sesi\u00f3n inv\u00e1lida. Reinicie la app.");
                yield break;
            }

            if (status == null)
            {
                consecutivePollFailures++;
                Debug.LogWarning($"Poll fallido consecutivo #{consecutivePollFailures}");
                if (consecutivePollFailures >= 5 && statusText != null)
                {
                    statusText.text = "Reintentando conexi\u00f3n...";
                }
                continue;
            }

            consecutivePollFailures = 0;
            if (statusText != null) statusText.text = "Esperando confirmaci\u00f3n del operador...";

            if (status == "confirmed")
            {
                if (statusText != null) statusText.text = "Sesi\u00f3n confirmada. Iniciando...";
                if (sceneController != null)
                {
                    sceneController.LoadScene("Start");
                }
                else
                {
                    SetError("SceneController no asignado en SessionCreationController.");
                }
                yield break;
            }
        }
    }

    private void SetError(string msg)
    {
        if (errorText != null) errorText.text = msg;
        if (statusText != null) statusText.text = "";
        Debug.LogError(msg);
    }
}
