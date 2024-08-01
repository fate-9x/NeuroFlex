using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawn : MonoBehaviour
{

    [SerializeField] private List<GameObject> numbers;

    public Vector3 toScale = new (1, 1, 1);

    private void OnTriggerEnter(Collider other) {

        if (other.CompareTag("Player")) {

            int randomIndex = Random.Range(0, numbers.Count);

            GameObject number = Instantiate(numbers[randomIndex], transform.position, Quaternion.identity);

            number.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            StartCoroutine(ScaleOverTime(number, new Vector3(700f, 700f, 700f), 1f));
            
        }
    }

    private IEnumerator ScaleOverTime(GameObject number, Vector3 toScale, float duration) {

        float counter = 0;

        Vector3 startScaleSize = number.transform.localScale;

        while (counter < duration) {

            counter += Time.deltaTime;

            number.transform.localScale = Vector3.Lerp(startScaleSize, toScale, counter / duration);

            yield return null;
        }

        transform.localScale = toScale;

    }
}
