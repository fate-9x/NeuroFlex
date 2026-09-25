[System.Serializable]
public class PairRequest
{
    public string device_id;
    public string device_name;
    public string pin;
}

[System.Serializable]
public class PairResponse
{
    public string pairing_id;
    public int expires_in;
}

[System.Serializable]
public class PairStatus
{
    public string status;
    public string device_token;
}

[System.Serializable]
public class TermsData
{
    public string version_id;
    public string title;
    public string content;
    public string language;
    public string content_hash;
    public string created_at;
}

[System.Serializable]
public class TermsAcceptRequest
{
    public string version_id;
    public string content_hash;
    public string application_version;
}

[System.Serializable]
public class TermsAcceptResponse
{
    public string acceptance_token;
}

[System.Serializable]
public class CreateSessionRequest
{
    public string acceptance_token;
    public PatientInfo patient_info;
}

[System.Serializable]
public class PatientInfo
{
    public string notes;
}

[System.Serializable]
public class CreateSessionResponse
{
    public string session_id;
    public string metrics_token;
    public string status;
}

[System.Serializable]
public class SessionStatusResponse
{
    public string session_id;
    public string status;
}

[System.Serializable]
public class MetricsResponse
{
    public bool ok;
}

[System.Serializable]
public class OfflineMetrics
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
}
