using UnityEngine;
using TMPro;

public class EndZone : MonoBehaviour
{
    [SerializeField] private GameObject dialogScreen;
    [SerializeField] private Animator animatorPlayer;
    public TextMeshProUGUI SessionText;
    private ExtractDataCollector extractDataCollector;
    private APIManager apiManager;

    private void Start()
    {
        GameObject utilsObj = GameObject.Find("Utils");
        if (utilsObj != null)
        {
            extractDataCollector = utilsObj.GetComponent<ExtractDataCollector>();
            apiManager = utilsObj.GetComponent<APIManager>();
        }

        if (SessionText != null && apiManager != null && apiManager.SessionActive)
        {
            SessionText.text = $"N\u00famero de sesi\u00f3n: {apiManager.DisplayCode}";
        }
        else if (SessionText != null)
        {
            SessionText.text = "Esperando inicio de sesi\u00f3n...";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("Player ha entrado en la zona final");
        if (animatorPlayer != null) animatorPlayer.speed = 0;
        if (dialogScreen != null) dialogScreen.SetActive(true);

        if (extractDataCollector != null)
        {
            extractDataCollector.CalculateAverageCaptureTime();
            extractDataCollector.CalculateAveragePrecision();
            extractDataCollector.ExtractScores();
            extractDataCollector.ExtractResponseTimePlayerUp();

            Inputs inputs = other.GetComponent<Inputs>();
            if (inputs != null)
            {
                inputs.StopActiveTaskTimer();
                extractDataCollector.ExtractActiveTaskTime();
                inputs.ShowDebugData();
            }

            string displayCode = apiManager != null ? apiManager.DisplayCode : null;
            extractDataCollector.SendDataToAPI((success) =>
            {
                if (SessionText == null) return;
                if (success)
                    SessionText.text = $"{displayCode}\nSesi\u00f3n completada";
                else
                    SessionText.text = "Error al enviar m\u00e9tricas. Contacte al operador.";
            });
        }
    }
}
