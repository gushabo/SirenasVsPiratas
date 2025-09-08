using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public int numEnemies;
    public List<GameObject> enemies;

    private void Start()
    {
        GameManager.GetInstance().enemiesLeft = enemies.Count;
    }

    private void Update()
    {
        for (int i = 0; i < numEnemies; i++)
        {
            Instantiate(enemies[i], transform.position, transform.rotation);
            GameManager.GetInstance().enemiesLeft--;
        }
    }
}
