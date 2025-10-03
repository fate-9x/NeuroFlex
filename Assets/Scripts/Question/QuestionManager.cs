using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class QuestionManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> alternativesObj;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Zone zone;
    [SerializeField] private AnimationsUtils animationsUtils;
    [HideInInspector] public bool flag = false;
    private bool blockAlternative = false;

    private int correctAlternative;
    private Coroutine responseTimerCoroutine;
    public float responseTimeQuestion = 0f;
    private float responseTimerStart = 0f;

    public int questionIndex; // 1, 2 o 3 según corresponda

    public ExtractDataCollector dataCollector;

    public void setQuestion(string question, int correctNumber)
    {
        questionText.text = question;

        correctAlternative = correctNumber;

        // Crear alternativas a partir de la respuesta correcta
        List<int> alternatives = new List<int>();
        int randomAlternative;

        for (int i = 0; i < alternativesObj.Count - 1; i++)
        {
            do
            {
                randomAlternative = Random.Range(correctNumber - 2, correctNumber + 4);
            }
            // Verificar si la alternativa ya fue elegida, si es la respuesta correcta o si es un número negativo
            while (alternatives.Contains(randomAlternative) || randomAlternative == correctNumber || randomAlternative < 0);

            // Añadir la alternativa a la lista
            alternatives.Add(randomAlternative);
        }

        // Asegurarse de que la respuesta correcta está en la lista
        if (!alternatives.Contains(correctNumber))
        {
            // Añadir la respuesta correcta en una posición aleatoria en la lista
            System.Random rng = new System.Random();
            alternatives.Insert(rng.Next(alternatives.Count + 1), correctNumber);
        }

        Debug.Log("Correct Number: " + correctNumber);

        for (int i = 0; i < alternatives.Count; i++)
        {
            TextMeshProUGUI childText = alternativesObj[i].GetComponentInChildren<TextMeshProUGUI>();

            childText.text = alternatives[i].ToString();

            // alternativesObj[i].text = alternatives[i].ToString();
        }

        gameObject.SetActive(true);

        // Iniciar el temporizador de respuesta
        if (responseTimerCoroutine != null)
        {
            StopCoroutine(responseTimerCoroutine);
        }
        responseTimerCoroutine = StartCoroutine(ResponseTimerCoroutine());
    }
    


    // Color 406DC2
    public void isCorrectAlternative(TextMeshProUGUI alternativeText)
    {
        // Detener el temporizador y guardar el tiempo
        if (responseTimerCoroutine != null)
        {
            StopCoroutine(responseTimerCoroutine);
            responseTimerCoroutine = null;
            responseTimeQuestion = Time.time - responseTimerStart;

            dataCollector = GameObject.Find("Utils").GetComponent<ExtractDataCollector>();

            // Guardar el tiempo en el centralizador
            if (dataCollector != null)
                dataCollector.SetTiempoRespuestaPregunta(questionIndex, responseTimeQuestion);
        }
        if (!blockAlternative)
        {
            blockAlternative = true;
            int alternative;
            int.TryParse(alternativeText.text, out alternative);

            // Navegar en la jerarquía
            Transform canvasParent = alternativeText.transform.parent;
            Transform borderParent = canvasParent.parent;
            Transform background = borderParent.Find("bgScreen");
            
            MeshRenderer backgroundRenderer = background.GetComponent<MeshRenderer>();

            if (alternative == correctAlternative)
            {
                if (backgroundRenderer != null)
                {
                    // Crear una instancia única del material
                    Material instanceMaterial = new Material(backgroundRenderer.material);
                    backgroundRenderer.material = instanceMaterial;
                    // Verde #47C241 convertido a RGB (71, 194, 65)
                    instanceMaterial.SetColor("_Color", new Color(71f/255f, 194f/255f, 65f/255f, 0.5f));
                    ScoreManager scoreManager = GameObject.Find("Utils").GetComponent<ScoreManager>();
                    scoreManager.scoreQuestions += 1;
                }
            }
            else
            {
                if (backgroundRenderer != null)
                {
                    // Crear una instancia única del material
                    Material instanceMaterial = new Material(backgroundRenderer.material);
                    backgroundRenderer.material = instanceMaterial;
                    // Rojo #C24144 convertido a RGB (194, 65, 68)
                    instanceMaterial.SetColor("_Color", new Color(194f/255f, 65f/255f, 68f/255f, 0.5f));
                }
            }

            Invoke("CallFunctionsAfterDelay", 2f);
        }
    }

    private void CallFunctionsAfterDelay()
    {
        if (zone.tutorialMode)
        {
            flag = true;
            zone.Destroy();
        }
        else
        {
            zone.activePlayer();
            zone.Destroy();
        }   
    }

    private IEnumerator ResponseTimerCoroutine()
    {
        responseTimerStart = Time.time;
        responseTimeQuestion = 0f;
        while (true)
        {
            responseTimeQuestion = Time.time - responseTimerStart;
            yield return null;
        }   
    }
}
