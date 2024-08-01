using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    List<GameObject> getSpawners(GameObject zone)
    {
        // Crear una lista para almacenar los GameObjects "Spawns"
        List<GameObject> spawns = new List<GameObject>();

        // Iterar sobre todos los hijos de la zona actual
        for (int i = 0; i < zone.transform.childCount; i++)
        {
            // Comparar el tag del hijo en el índice i
            if (zone.transform.GetChild(i).tag == "Spawn")
            {
                // Obtener el hijo en el índice i
                Transform child = zone.transform.GetChild(i);

                // Añadir el hijo a la lista "spawns"
                spawns.Add(child.gameObject);
            }
        }

        // Devolver la lista de "Spawns"
        return spawns;
    }

    List<int> setSpawners(int totalSpawns)
    {
        // Lista de Spawns Elegidos
        List<int> spawnsSelecteds = new List<int>();

        for (int i = 0; i < (int)System.Math.Round((double)totalSpawns / 2); i++)
        {
            // Elegir un número aleatorio
            int randomSpawn = Random.Range(0, totalSpawns);

            // Verificar si el número ya fue elegido
            while (spawnsSelecteds.Contains(randomSpawn))
            {
                randomSpawn = Random.Range(0, totalSpawns);
            }

            // Añadir el número a la lista de números elegidos
            spawnsSelecteds.Add(randomSpawn);
        }

        return spawnsSelecteds;
    }

    public void activeSpawn(GameObject zone)
    {
        // Obtener la lista de Spawns
        List<GameObject> spawns = getSpawners(zone);

        // Obtener la lista de Spawns Elegidos
        List<int> spawnsSelecteds = setSpawners(spawns.Count);

        // Iterar sobre los Spawns Elegidos
        for (int i = 0; i < spawnsSelecteds.Count; i++)
        {
            // Obtener el Spawn en el índice i
            GameObject spawn = spawns[spawnsSelecteds[i]];

            spawn.SetActive(true);
        }
    }
}
