using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    (List<GameObject>, List<GameObject>, List<GameObject>) getSpawners(GameObject zone)
    {
        // Crear una lista para almacenar los GameObjects "Spawns" de las diferentes zonas
        List<GameObject> spawns_1 = new List<GameObject>();
        List<GameObject> spawns_2 = new List<GameObject>();
        List<GameObject> spawns_3 = new List<GameObject>();

        Debug.Log("ChildCount Zone: " + zone.transform.childCount);

        // Iterar sobre todos los hijos de la zona actual
        for (int i = 0; i < zone.transform.childCount; i++)
        {
            // Comparar el tag del hijo en el índice i
            if (zone.transform.GetChild(i).tag == "Spawn_1")
            {
                // Obtener el hijo en el índice i
                Transform child = zone.transform.GetChild(i);

                // Añadir el hijo a la lista "spawns"
                spawns_1.Add(child.gameObject);
            }
            else if (zone.transform.GetChild(i).tag == "Spawn_2")
            {
                // Obtener el hijo en el índice i
                Transform child = zone.transform.GetChild(i);

                // Añadir el hijo a la lista "spawns"
                spawns_2.Add(child.gameObject);
            }
            else if (zone.transform.GetChild(i).tag == "Spawn_3")
            {
                // Obtener el hijo en el índice i
                Transform child = zone.transform.GetChild(i);

                // Añadir el hijo a la lista "spawns"
                spawns_3.Add(child.gameObject);
            }
        }

        // Devolver la lista de "Spawns"
        return (spawns_1, spawns_2, spawns_3);
    }

    List<int> setSpawners(List<List<GameObject>> zones)
    {
        // Lista de Spawns Elegidos
        List<int> spawnsSelecteds = new List<int>();

        foreach (List<GameObject> zone in zones)
        {
            // Elegir un número aleatorio
            int randomSpawn = Random.Range(0, zone.Count);

            // Añadir el número a la lista de números elegidos
            spawnsSelecteds.Add(randomSpawn);
        }

        return spawnsSelecteds;
    }

    /*
    Esta funcion es para activar los spawns de la zona que se obtiene, normalmente esta funcion
    se llama desde el script Zone.cs y se le pasa como parametro a si mismo, dentro de esta zona
    deben estar los spawns que serian los numeros aleatorios que aparecen y que se deben agarrar

    La funcion obtiene los spawns de la zona, esta zona se divide en 3 subzonas, cada una con 3 spawns, estos
    spawns estan en 3 direcciones diferentes (puede ser modificable), esto para que se active un solo spawn
    que son los spawns elegidos por la funcion setSpawners, luego se activan esos spawns seleccionados ya que
    deben estar desactivados al inicio, finalmente se destruyen los spawns que no fueron seleccionados
    */    
    public void activeSpawn(GameObject zone, int[] numbersSelected)
    {
        // Obtener la lista de Spawns
        (List<GameObject> spawns1, List<GameObject> spawns2, List<GameObject> spawns3) = getSpawners(zone);

        // Obtener la lista de Spawns Elegidos
        List<int> spawnsSelecteds = setSpawners(new List<List<GameObject>> { spawns1, spawns2, spawns3 });

        spawns1[spawnsSelecteds[0]].SetActive(true);
        spawns1[spawnsSelecteds[0]].GetComponent<Spawn>().numberSelected = numbersSelected[0];

        spawns2[spawnsSelecteds[1]].SetActive(true);
        spawns2[spawnsSelecteds[1]].GetComponent<Spawn>().numberSelected = numbersSelected[1];

        spawns3[spawnsSelecteds[2]].SetActive(true);
        spawns3[spawnsSelecteds[2]].GetComponent<Spawn>().numberSelected = numbersSelected[2];

        // Destruir los spawns que no fueron elegidos en cada zona
        // Listas de spawns
        List<List<GameObject>> spawnsLists = new List<List<GameObject>> { spawns1, spawns2, spawns3 };

        // Iterar sobre cada lista de spawns
        for (int i = 0; i < spawnsLists.Count; i++)
        {
            // Iterar sobre cada spawn en la lista actual
            for (int j = 0; j < spawnsLists[i].Count; j++)
            {
                // Si el spawn actual no fue seleccionado, destruirlo
                if (j != spawnsSelecteds[i])
                {
                    Destroy(spawnsLists[i][j]);
                }
            }
        }
    }

    /*

    Esta funcion es para obtener los numeros que se eligieron en los spawns de la zona, se obtienen los spawns
    y luego se accede al componente Spawn de cada uno para obtener el numero que se selecciono ya que en ese script
    hay una variable que almacena el numero que se selecciono, este numero se guarda en una lista y se retorna

    */

    public List<int> getNumbers(GameObject zone)
    {
        // Crear una lista para almacenar los números
        List<int> numbers = new List<int>();

        // Iterar sobre todos los hijos de la zona actual
        for (int i = 0; i < zone.transform.childCount; i++)
        {
            // Comparar el tag del hijo en el índice i
            if (zone.transform.GetChild(i).tag == "Spawn_1" || zone.transform.GetChild(i).tag == "Spawn_2" || zone.transform.GetChild(i).tag == "Spawn_3")
            {
                // Obtener el hijo en el índice i
                Transform child = zone.transform.GetChild(i);

                // Obtener el componente Spawn del hijo
                Spawn spawn = child.GetComponent<Spawn>();

                // Añadir el número a la lista "numbers"

                numbers.Add(spawn.numberSelected);
            }
        }

        for (int i = 0; i < numbers.Count; i++)
        {
            Debug.Log("Number " + i + ": " + numbers[i]);
        }

        // Devolver la lista de "numbers"
        return numbers;
    }

    /*
    
    Esta funcion es para desactivar los spawns de la zona, normalmente se llama esta funcion cuando el jugador
    entra a la parte donde aparecen las preguntas.

    */

    public void deactiveSpawn(GameObject zone)
    {
        // Obtener la lista de Spawns
        (List<GameObject> spawns1, List<GameObject> spawns2, List<GameObject> spawns3) = getSpawners(zone);

        // Listas de spawns
        List<List<GameObject>> spawnsLists = new List<List<GameObject>> { spawns1, spawns2, spawns3 };

        // Iterar sobre cada lista de spawns
        for (int i = 0; i < spawnsLists.Count; i++)
        {
            // Iterar sobre cada spawn en la lista actual
            for (int j = 0; j < spawnsLists[i].Count; j++)
            {
                // Obtener el Spawn en el índice j
                GameObject spawn = spawnsLists[i][j];

                Spawn obj = spawn.GetComponent<Spawn>();

                Destroy(obj.numberSpawned);
            }
        }
    }

/*     public void activeSpawnTutorial(GameObject zone)
    {
        // Obtener la lista de Spawns
        (List<GameObject> spawns1, List<GameObject> spawns2, List<GameObject> spawns3) = getSpawners(zone);

        // Obtener la lista de Spawns Elegidos
        List<int> spawnsSelecteds = setSpawners(new List<List<GameObject>> { spawns1, spawns2, spawns3 });

        spawns1[spawnsSelecteds[0]].SetActive(true);
        spawns2[spawnsSelecteds[1]].SetActive(true);
        spawns3[spawnsSelecteds[2]].SetActive(true);
    } */

    public IEnumerator Tutorial(GameObject zone, int fase)
    {
        bool isGrab = false;
        // Iterar sobre todos los hijos de la zona actual
        for (int i = 0; i < zone.transform.childCount; i++)
        {
            if (fase == 1)
            {
                // Comparar el tag del hijo en el índice i
                if (zone.transform.GetChild(i).tag == "Spawn_1")
                {
                    // Obtener el hijo en el índice i
                    Transform child = zone.transform.GetChild(i);

                    Debug.Log(child.gameObject.name);

                    child.gameObject.SetActive(true);

                    yield return new WaitForSeconds(1f);

                    while (!isGrab)
                    {
                        isGrab = child.GetComponent<Spawn>().numberSpawned.GetComponent<ScoreAdder>().isGrabbed;
                        yield return null; // Esperar hasta el próximo frame
                    }
                }
            }
            else if (fase == 2)
            {
                // Comparar el tag del hijo en el índice i
                if (zone.transform.GetChild(i).tag == "Spawn_2")
                {
                    // Obtener el hijo en el índice i
                    Transform child = zone.transform.GetChild(i);

                    child.gameObject.SetActive(true);

                    yield return new WaitForSeconds(1f);

                    while (!isGrab)
                    {
                        isGrab = child.GetComponent<Spawn>().numberSpawned.GetComponent<ScoreAdder>().isGrabbed;
                        yield return null; // Esperar hasta el próximo frame
                    }
                }
            }
        }
    }
}
