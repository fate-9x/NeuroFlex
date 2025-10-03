using System.Collections;
using System.Collections.Generic;
using Meta.WitAi;
using UnityEngine;

public class ScoreAdder : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource; // Referencia al AudioSource
    public bool isGrabbed = false;

    // Variables para medir tiempo de captura
    private float spawnTime;
    private ExtractDataCollector dataCollector;

    [SerializeField] private Collider precisionCollider; // Collider interno (100% precisión)
    [SerializeField] private Collider captureCollider;   // Collider externo (captura permitida)

    private void Start()
    {
        // Obtener el AudioSource si no fue asignado en el Inspector
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // Inicializar tiempo de spawn y buscar el data collector
        spawnTime = Time.time;
        dataCollector = GameObject.Find("Utils").GetComponent<ExtractDataCollector>();
    }

    
    
    private float CalculatePrecision(Vector3 playerPosition)
    {
        // Si está dentro del collider de precisión, 100%
        if (precisionCollider.bounds.Contains(playerPosition))
        {
            return 100f;
        }
        
        // Si está dentro del collider de captura pero fuera del de precisión
        if (captureCollider.bounds.Contains(playerPosition))
        {
            // Calcular distancia al centro del collider de precisión
            Vector3 precisionCenter = precisionCollider.bounds.center;
            float distanceToCenter = Vector3.Distance(playerPosition, precisionCenter);
            
            // Calcular el radio del collider de precisión
            float precisionRadius = precisionCollider.bounds.extents.magnitude;
            
            // Calcular precisión basada en la distancia
            // Mientras más cerca del centro, mayor precisión
            float precision = Mathf.Max(0f, 100f - (distanceToCenter / precisionRadius) * 100f);
            return precision;
        }
        
        // Si está fuera de ambos colliders, 0%
        return 0f;
    }
    
    public void AddScore() {
        // Medir tiempo de captura y calcular precisión antes de destruir el objeto
        float captureTime = Time.time - spawnTime;
        
        // Obtener posición de las manos
        Vector3 handPosition = GetClosestHandPosition();
        float precision = CalculatePrecision(handPosition);
        
        if (dataCollector != null)
        {
            dataCollector.AddCaptureTime(captureTime);
            dataCollector.AddPrecision(precision);
            Debug.Log($"Número capturado en {captureTime} segundos con {precision}% de precisión");
        }
        
        // Buscar el GameObject Utils
        GameObject utilsObject = GameObject.Find("Utils");
        
        // Comprobar si el GameObject existe
        if (utilsObject != null)
        {
            // Obtener el componente ScoreManager
            ScoreManager scoreManager = utilsObject.GetComponent<ScoreManager>();

            // Comprobar si el componente existe
            if (scoreManager != null)
            {
                // Reproducir el sonido si existe el AudioSource
                if (audioSource != null && audioSource.clip != null)
                {
                    audioSource.Play();
                }

                Debug.Log("ScoreManager encontrado");
                scoreManager.scoreNumbers += 1;
                isGrabbed = true;
                Destroy(gameObject, 0.2f);
            }
        }
        else
        {
            Debug.LogError("No se encontró el GameObject Utils");
        }
    }

    private Vector3 GetClosestHandPosition()
    {
        GameObject leftHand = GameObject.FindGameObjectWithTag("LeftHand");
        GameObject rightHand = GameObject.FindGameObjectWithTag("RightHand");
        
        Vector3 numberPosition = transform.position;
        
        // Si solo una mano existe, usar esa
        if (leftHand != null && rightHand == null)
            return leftHand.transform.position;
        if (rightHand != null && leftHand == null)
            return rightHand.transform.position;
        
        // Si ambas manos existen, usar la más cercana
        if (leftHand != null && rightHand != null)
        {
            float leftDistance = Vector3.Distance(numberPosition, leftHand.transform.position);
            float rightDistance = Vector3.Distance(numberPosition, rightHand.transform.position);
            
            return leftDistance < rightDistance ? leftHand.transform.position : rightHand.transform.position;
        }
        
        // Si no se encuentra ninguna mano, usar la posición de la cámara como fallback
        Debug.LogWarning("No se encontraron manos con tags LeftHand o RightHand, usando posición de cámara");
        return Camera.main.transform.position;
    }
}
