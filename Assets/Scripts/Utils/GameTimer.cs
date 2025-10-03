using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    public string timerText;
    private float startTime;

    // Iniciar el temporizador cuando el juego comienza
    void Start()
    {
        startTime = Time.time;
    }

    // Actualizar el temporizador cada frame
    void Update()
    {
        float t = Time.time - startTime;

        string minutes = ((int) t / 60).ToString("00");
        string seconds = ((int) t % 60).ToString("00");

        timerText = "Tiempo en juego\n" + minutes + ":" + seconds;
    }
}