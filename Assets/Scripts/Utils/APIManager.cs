using System;
using System.Collections;
using UnityEngine;

public class APIManager : MonoBehaviour
{
    public enum BackendMode { Online, Offline }

    [SerializeField] private ApiConfig apiConfig;
    [SerializeField] private LanConfig lanConfig;

    public ExtractData data = new ExtractData();

    public string SessionId { get; set; }
    public string MetricsToken { get; set; }
    public string DisplayCode { get; set; }
    public int TtlSeconds { get; set; }
    public string AcceptanceToken { get; set; }
    public bool SessionActive => !string.IsNullOrEmpty(MetricsToken);
    public bool LastCreateSessionUnauthorized { get; set; }
    public BackendMode Mode { get; private set; }

    public IBackend ActiveBackend { get; private set; }

    public void SetBackend(BackendMode mode)
    {
        Mode = mode;
        if (mode == BackendMode.Online)
        {
            ActiveBackend = new AwsBackend(apiConfig, this);
        }
        else
        {
            ActiveBackend = new LanBackend(lanConfig, this);
        }
    }

    public void CreateSession(string acceptanceToken, Action<bool> onSuccess)
    {
        if (ActiveBackend == null) { onSuccess?.Invoke(false); return; }
        StartCoroutine(ActiveBackend.CreateSession(acceptanceToken, onSuccess));
    }

    public void GetCurrentTerms(Action<bool, TermsData> onResult)
    {
        if (ActiveBackend == null) { onResult?.Invoke(false, null); return; }
        StartCoroutine(ActiveBackend.GetCurrentTerms(onResult));
    }

    public void AcceptTerms(string versionId, string contentHash, Action<bool, string> onResult)
    {
        if (ActiveBackend == null) { onResult?.Invoke(false, null); return; }
        StartCoroutine(ActiveBackend.AcceptTerms(versionId, contentHash, onResult));
    }

    public void GetSessionStatus(Action<string> onStatus)
    {
        if (ActiveBackend == null) { onStatus?.Invoke(null); return; }
        StartCoroutine(ActiveBackend.GetSessionStatus(onStatus));
    }

    public void SendMetrics(Action<bool> onSuccess)
    {
        if (ActiveBackend == null) { onSuccess?.Invoke(false); return; }
        StartCoroutine(ActiveBackend.SendMetrics(GetJsonData(), onSuccess));
    }

    public void ClearMetricsToken()
    {
        MetricsToken = null;
    }

    public string GetJsonData()
    {
        return JsonUtility.ToJson(data);
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
