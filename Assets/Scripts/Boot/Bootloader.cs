using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;

public class Bootloader : MonoBehaviour
{
    [SerializeField] private float onlineTimeout = 10f;
    [SerializeField] private float offlineHoldTime = 4f;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text displayCodeText;
    [SerializeField] private SceneController sceneController;

    private float elapsed;
    private float gripElapsed;
    private bool decided;
    private bool hasInternet;
    private APIManager apiManager;

    IEnumerator Start()
    {
        if (statusText != null) statusText.text = "Verificando conexión...";
        yield return CheckInternet();
        elapsed = 0f;
        gripElapsed = 0f;

        while (!decided)
        {
            bool leftGrip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.Touch);
            bool rightGrip = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger, OVRInput.Controller.Touch);
            bool bothGrips = leftGrip && rightGrip;

            if (bothGrips)
            {
                gripElapsed += Time.deltaTime;
                if (gripElapsed >= offlineHoldTime)
                {
                    EnterMode(APIManager.BackendMode.Offline);
                    yield break;
                }
            }
            else
            {
                gripElapsed = 0f;
                elapsed += Time.deltaTime;
                if (elapsed >= onlineTimeout)
                {
                    EnterMode(hasInternet ? APIManager.BackendMode.Online : APIManager.BackendMode.Offline);
                    yield break;
                }
            }

            UpdateUI(bothGrips);
            yield return null;
        }
    }

    private IEnumerator CheckInternet()
    {
        using (UnityWebRequest request = UnityWebRequest.Get("https://clients3.google.com/generate_204"))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();
            hasInternet = request.result == UnityWebRequest.Result.Success && request.responseCode == 204;
        }
    }

    private void UpdateUI(bool bothGrips)
    {
        if (statusText == null) return;

        if (bothGrips)
        {
            statusText.text = $"Modo offline en {Mathf.Max(0, offlineHoldTime - gripElapsed):F1} s";
        }
        else if (hasInternet)
        {
            statusText.text = $"Mantén ambos botones laterales para modo offline. Modo online en {Mathf.Max(0, onlineTimeout - elapsed):F1} s";
        }
        else
        {
            statusText.text = $"Sin conexión. Modo offline en {Mathf.Max(0, onlineTimeout - elapsed):F1} s";
        }
    }

    private void EnterMode(APIManager.BackendMode mode)
    {
        decided = true;
        GameObject utilsObj = GameObject.Find("Utils");
        if (utilsObj != null)
        {
            apiManager = utilsObj.GetComponent<APIManager>();
            if (apiManager != null)
            {
                apiManager.SetBackend(mode);
            }
        }

        if (mode == APIManager.BackendMode.Offline)
        {
            StartCoroutine(OfflinePairingFlow());
            return;
        }

        if (sceneController != null)
        {
            sceneController.LoadScene("TerminosYCondiciones");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TerminosYCondiciones");
        }
    }

    private IEnumerator OfflinePairingFlow()
    {
        LanBackend lan = apiManager != null ? apiManager.ActiveBackend as LanBackend : null;
        if (lan == null)
        {
            SetError("Backend offline no configurado.");
            yield break;
        }

        if (statusText != null) statusText.text = "Buscando dashboard local...";
        if (displayCodeText != null) displayCodeText.text = "";

        while (true)
        {
            bool startDone = false;
            PairingClient.PairingStartResult start = null;
            yield return lan.StartPairing(r => { start = r; startDone = true; });
            while (!startDone) yield return null;

            if (start == null || !start.success)
            {
                if (statusText != null) statusText.text = start?.error ?? "Error iniciando emparejamiento";
                yield return new WaitForSeconds(2f);
                if (statusText != null) statusText.text = "Reintentando emparejamiento...";
                yield return new WaitForSeconds(2f);
                continue;
            }

            if (displayCodeText != null)
                displayCodeText.text = start.pin;

            if (statusText != null)
                statusText.text = "Muestra este PIN al especialista...";

            bool pollDone = false;
            PairingClient.PairingResult result = null;
            yield return lan.PollPairing(start, r => { result = r; pollDone = true; });
            while (!pollDone) yield return null;

            if (result.success)
            {
                if (statusText != null) statusText.text = "Emparejamiento confirmado.";
                break;
            }

            if (statusText != null) statusText.text = result.error ?? "Emparejamiento fallido";
            yield return new WaitForSeconds(2f);
            if (statusText != null) statusText.text = "Reintentando emparejamiento...";
            yield return new WaitForSeconds(2f);
        }

        if (sceneController != null)
        {
            sceneController.LoadScene("TerminosYCondiciones");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TerminosYCondiciones");
        }
    }

    private void SetError(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.LogError(msg);
    }
}
