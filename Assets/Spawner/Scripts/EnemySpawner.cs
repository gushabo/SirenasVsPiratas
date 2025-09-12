using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemySpawner : MonoBehaviour
{
    public List<GameObject> enemies;
    int enemiesLeftToSpawn;
    public List<Transform> targets = new List<Transform>();
    public float spawnInterval = 4f;

    private void Start()
    {
        GameManager.GetInstance().enemiesLeft = enemies.Count;
        enemiesLeftToSpawn = enemies.Count;
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (enemiesLeftToSpawn > 0)
            {
                // Seleccionar un enemigo aleatorio de la lista
                int randomIndex = Random.Range(0, enemies.Count);
                GameObject enemyToSpawn = enemies[randomIndex];

                // Instanciar el enemigo en la posición del spawner
                var enemy = Instantiate(enemyToSpawn, transform.position, transform.rotation);
                enemy.GetComponent<EnemyMovement>().targets = targets;

                enemiesLeftToSpawn--;
            }
        }
    }
}
