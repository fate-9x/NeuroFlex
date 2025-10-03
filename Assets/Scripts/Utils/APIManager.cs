using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class APIManager : MonoBehaviour
{
    public ExtractData data = new ExtractData();
    public string apiUrl = "https://f13h4cz6id.execute-api.sa-east-1.amazonaws.com/data";
    // Método para enviar JSON a una API y recibir la respuesta
    public void SendJsonToAPI(Action<string> onResponse)
    {
        StartCoroutine(PostRequest(onResponse));
    }

    private IEnumerator PostRequest(Action<string> onResponse)
    {
        string jsonData = GetJsonData();
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onResponse?.Invoke(request.downloadHandler.text);
        }
        else
        {
            onResponse?.Invoke($"Error: {request.error}");
        }
    }

    public string GetJsonData()
    {
        // Asignar valores a las propiedades...
        string json = JsonUtility.ToJson(data);
        return json;
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
    }
}


