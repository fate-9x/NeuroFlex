using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboarding : MonoBehaviour
{
    [SerializeField] private Camera mainCamera; // La cámara que el texto debe seguir
    [SerializeField] private float lerpSpeed = 0.1f; // La velocidad de la interpolación

    [SerializeField] private Vector3 rotationOff = new Vector3(0, 0, 0); // El offset de la rotación
    [SerializeField] private Vector3 fixRotation = new Vector3(0, 0, 0); // El offset de la rotación


    public enum BillboardType
{
    Text,
    Adjust,
    Numbers
}

[SerializeField] private BillboardType billboardType;

    void Start()
    {
        if (mainCamera == null)
        {
            // Si no se ha asignado ninguna cámara, usa la cámara principal por defecto
            mainCamera = Camera.main;
        }

        if(billboardType == BillboardType.Text)
        {
            // Hacer que el objeto mire hacia la cámara al comienzo
            transform.LookAt(mainCamera.transform);
            // Restablecer la rotación en el eje Z
            Vector3 rotation = transform.rotation.eulerAngles;
            rotation.z = 0;
            transform.rotation = Quaternion.Euler(rotation);
        }
    }

    void Update()
    {
        if (billboardType == BillboardType.Text)
        {
            // Calcular la rotación deseada
            Quaternion targetRotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position, Vector3.up);

            // Interpolar entre la rotación actual y la rotación deseada
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, lerpSpeed);

            // Restablecer la rotación en el eje Z
            Vector3 rotation = transform.rotation.eulerAngles;
            rotation.z = 0;
            transform.rotation = Quaternion.Euler(rotation);
        } 
        else if (billboardType == BillboardType.Adjust)
        {
            // Calcular la rotación deseada
            Quaternion targetRotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position, Vector3.up);

            // Ajustar la rotación deseada
            Vector3 targetEuler = targetRotation.eulerAngles;
            targetEuler.x = (fixRotation.x == 0) ? targetEuler.x - rotationOff.x : fixRotation.x;
            targetEuler.y = (fixRotation.y == 0) ? targetEuler.y - rotationOff.y : fixRotation.y;
            targetEuler.z = (fixRotation.z == 0) ? targetEuler.z - rotationOff.z : fixRotation.z;

            // Aplicar la rotación ajustada
            transform.rotation = Quaternion.Euler(targetEuler);
        }
        else if (billboardType == BillboardType.Numbers)
        {
            // Hacer que el objeto mire hacia la cámara al comienzo
            transform.LookAt(mainCamera.transform);
            // Restablecer la rotación en el eje Z
            Vector3 rotation = transform.rotation.eulerAngles;
            rotation.z = 0;
            transform.rotation = Quaternion.Euler(rotation);
        } 
    }
}
