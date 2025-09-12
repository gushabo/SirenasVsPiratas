using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemySpawner : MonoBehaviour
{
    public List<GameObject> enemies;
    int enemiesLeftToSpawn;
    public List<Transform> targets = new List<Transform>();
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 4f;

    private void Start()
    {
        GameManager.GetInstance().enemiesLeft = enemies.Count;
        enemiesLeftToSpawn = enemies.Count;
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        int i = 0;
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));

            if (enemiesLeftToSpawn > 0)
            {
                GameObject enemyToSpawn = enemies[i];
                // Instanciar el enemigo en la posición del spawner
                var enemy = Instantiate(enemyToSpawn, transform.position, transform.rotation);
                enemy.GetComponent<EnemyMovement>().targets = targets;

                enemiesLeftToSpawn--;
                i++;
            }
        }
    }
}
