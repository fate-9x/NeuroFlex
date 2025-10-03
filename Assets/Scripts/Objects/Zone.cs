using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class Zone : MonoBehaviour
{
    private Spawner spawner;
    [SerializeField] Animator animatorPlayer;
    [SerializeField] public QuestionManager questionManager;
    [SerializeField] public bool tutorialMode = false;
    [SerializeField] public GameObject screen;
    public int operation; // Si es 1 es una suma, si es 0 es una resta

    [HideInInspector]
    public List<int> numbers = new List<int>();

    void Start()
    {
        spawner = GameObject.Find("Utils").GetComponent<Spawner>();
        if (animatorPlayer != null)
        {
            animatorPlayer.enabled = true;
        }
        operation = Random.Range(0, 2);
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !tutorialMode)
        {
            int[] numbers = operation == 1 ? GetAdditionNumbers(1, 9) : GetSubtractionNumbers(1, 9);
            spawner.activeSpawn(gameObject, numbers);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !tutorialMode)
        {
            numbers = spawner.getNumbers(gameObject);
            
            activeQuest();
            
            if (animatorPlayer != null)
            {
                animatorPlayer.enabled = false;
            }
            spawner.deactiveSpawn(gameObject);
        }
    }

    public void activeQuest()
    {
        Texts questions = spawner.GetComponent<Texts>();

        int[] positionsNumbers = getPositionNumbers();

        string question = operation == 1 ? questions.getQuestionAddition(numbers.Count) : questions.getQuestionSubtraction(numbers.Count, positionsNumbers[0] + 1, positionsNumbers[1] + 1);

        questionManager.setQuestion(question, operation == 1 ? numbers.Sum() : numbers[positionsNumbers[0]] - numbers[positionsNumbers[1]]);
    }

    public void activePlayer()
    {
        if (animatorPlayer != null)
        {
            animatorPlayer.enabled = true;
        }
    }

    public int[] GetSubtractionNumbers(int minValue, int maxValue)
    {
        if (maxValue - minValue < 2)
        {
            Debug.LogError("El rango debe ser al menos 3 para generar 3 números diferentes.");
            return new int[3];
        }

        int[] numbers = new int[3];
        List<int> availableNumbers = new List<int>();

        // Crear una lista de números disponibles
        for (int i = minValue; i <= maxValue; i++)
        {
            availableNumbers.Add(i);
        }

        // Generar los tres números
        for (int i = 0; i < 3; i++)
        {
            int index = Random.Range(0, availableNumbers.Count);
            numbers[i] = availableNumbers[index];
            // availableNumbers.RemoveAt(index);
        }

        return numbers;
    }

    public int[] GetAdditionNumbers(int minValue, int maxValue)
    {

        if (maxValue - minValue < 2)
        {
            Debug.LogError("El rango debe ser al menos 3 para generar 3 números diferentes.");
            return new int[3];
        }

        int[] numbers = new int[3];
        List<int> availableNumbers = new List<int>();

        // Crear una lista de números disponibles
        for (int i = minValue; i <= maxValue; i++)
        {
            availableNumbers.Add(i);
        }

        // Generar los tres números
        for (int i = 0; i < 3; i++)
        {
            int index = Random.Range(0, availableNumbers.Count);
            numbers[i] = availableNumbers[index];
            availableNumbers.RemoveAt(index);
        }

        // Ordenar los números de mayor a menor
        System.Array.Sort(numbers);
        System.Array.Reverse(numbers);

        return numbers;
    }

    public int[] getPositionNumbers() {
        int max1 = int.MinValue;
        int max2 = int.MinValue;
        int pos1 = -1;
        int pos2 = -1;
        int[] positions = new int[3];

        for (int i = 0; i < numbers.Count; i++)
        {
            if (numbers[i] > max1)
            {
                max2 = max1;
                pos2 = pos1;
                max1 = numbers[i];
                pos1 = i;
            }
            else if (numbers[i] > max2)
            {
                max2 = numbers[i];
                pos2 = i;
            }
        }
        
        positions[0] = pos1;
        positions[1] = pos2;

        return positions;
    }
    

    public void Destroy()
    {
        Destroy(gameObject);
    }
}
