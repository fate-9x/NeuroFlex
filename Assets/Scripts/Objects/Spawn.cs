using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawn : MonoBehaviour
{

    [SerializeField] private List<GameObject> numbers;

    public Vector3 toScale = new (1, 1, 1);

    [SerializeField] private AnimationsUtils animationsUtils;

    public GameObject numberSpawned;
    public bool spawnUp = false;

    public int numberSelected = -1;

    private void Start() {
        if (transform.position.y > 1.40)
        {
            spawnUp = true;
        }
    }

    private void OnTriggerEnter(Collider other) {

        if (other.CompareTag("Player")) {

            if (numberSelected == -1)
            {
                numberSelected = Random.Range(1, 10);
            }

            numberSpawned = Instantiate(numbers[numberSelected -1], transform.position, Quaternion.identity);

            numberSpawned.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            StartCoroutine(animationsUtils.ScaleOverTime(numberSpawned, new Vector3(200f, 200f, 200f), 1f));

            if (spawnUp)
            {
                other.GetComponent<Inputs>().numberSpawned = true;
            }
        }
    }
}
