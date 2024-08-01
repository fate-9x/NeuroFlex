using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioClip audioClip;
    [SerializeField]
    private VideoPlayer videoPlayer;
    [SerializeField]
    private List<GameObject> buttons; // Lista de botones

    void Start()
    {
        // Desactivar todos los botones al inicio
        foreach (var button in buttons)
        {
            button.SetActive(false);
        }
        // Llamar a la corutina ShowButtonAfterDelay
        StartCoroutine(ShowButtonAfterDelay(audioClip.length - 5f));
    }

    public void PlayAudio()
    {
        if (audioSource != null && audioClip != null)
        {
            audioSource.Play();
            videoPlayer.Play();
            Debug.Log("Audio: " + audioClip.length);
        }
        else
        {
            Debug.LogWarning("AudioSource o AudioClip no está asignado.");
        }
    }

    // Corutina que espera un cierto retraso y luego llama a ShowButton
    IEnumerator ShowButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Activar todos los botones cuando termine el audio
        foreach (var button in buttons)
        {
            button.SetActive(true);
        }
        Debug.Log("Botones activados");
    }
}