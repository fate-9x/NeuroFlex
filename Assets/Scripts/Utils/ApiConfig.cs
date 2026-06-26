using UnityEngine;

[CreateAssetMenu(fileName = "ApiConfig", menuName = "NeuroFlex/Api Config")]
public class ApiConfig : ScriptableObject
{
    [Tooltip("URL base de la API, sin trailing slash. Ej: https://xxx.execute-api.region.amazonaws.com/prod")]
    public string baseUrl;
}
