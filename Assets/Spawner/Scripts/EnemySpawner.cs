using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public List<GameObject> enemies;
    public int enemiesLeftToSpawn;
    public List<Transform> targets = new List<Transform>();
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 4f;
    public bool isPaused;
    
    public LevelManager lvlManager;

    private void Start()
    {
        
        //Revisar la pausa
        GameManager.GetInstance().onChangeGameState += OnChangeGameStateCallback;
        if(GameManager.GetInstance().gameState ==  GameState.Pause) isPaused = true;
        
        lvlManager = LevelManager.GetInstance();
        if (lvlManager == null) return;
        
        // Conteo de enemigos
        enemiesLeftToSpawn = enemies.Count;
        lvlManager.enemiesLeft = enemiesLeftToSpawn;
        StartCoroutine(SpawnEnemies());
    }
    
    public void OnChangeGameStateCallback(GameState newState)
    {
        isPaused = newState != GameState.Play;
    }

    private IEnumerator SpawnEnemies()
    {
        int i = 0;
        while (enemiesLeftToSpawn > 0)
        {
            float delay = Random.Range(minSpawnInterval, maxSpawnInterval);
            while (delay > 0f)
            {
                // si está pausado, espera al siguiente frame sin descontar tiempo
                if (!isPaused)
                    delay -= Time.deltaTime;
                yield return null;
            }

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
        gameObject.SetActive(false);
    }
}
