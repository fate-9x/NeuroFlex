using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zone : MonoBehaviour
{
    [SerializeField] private Spawner spawner;
    [SerializeField] private BoxCollider entryCollider;    
    [SerializeField] private BoxCollider exitCollider;

    void OnTriggerEnter(Collider other)
{
    // Comprobar si el collider que entró es el jugador
    if (other.gameObject.tag == "Player")
    {
        // Comprobar si el collider que entró es entryCollider
        if (other == entryCollider)
        {
            // El jugador entró en entryCollider
            Debug.Log("Player entered entryCollider");
        }
        // Comprobar si el collider que entró es exitCollider
        else if (other == exitCollider)
        {
            // El jugador entró en exitCollider
            Debug.Log("Player entered exitCollider");
        }
    }
}
}
