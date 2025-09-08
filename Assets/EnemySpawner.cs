using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemySpawner : MonoBehaviour
{
    public List<GameObject> enemies; // Prefabs de enemigos para instanciar
    public float spawnInterval = 4f;

    private void Start()
    {
        GameManager.GetInstance().enemiesLeft = enemies.Count;
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (enemies.Count > 0)
            {
                // Seleccionar un enemigo aleatorio de la lista
                int randomIndex = Random.Range(0, enemies.Count);
                GameObject enemyToSpawn = enemies[randomIndex];

                // Instanciar el enemigo en la posición del spawner
                Instantiate(enemyToSpawn, transform.position, transform.rotation);

                // Actualizar el contador en el GameManager
                GameManager.GetInstance().enemiesLeft--;
            }
        }
    }
}
