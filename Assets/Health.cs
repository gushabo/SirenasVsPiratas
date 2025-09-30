using System;
using UnityEngine;

public class Health : MonoBehaviour
{

    public int health;
    public int maxHealth = 100;
    public bool isCoral;

    void Start()
    {
        health = maxHealth;
        if (isCoral)
        {
            UiManager.GetInstance().LifeText.text = "Health: " + health;
        }
    } 

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            health = 0;
            if (isCoral)
            {
                GameManager.GetInstance().Lose = true;
                GameManager.GetInstance().GameOver();
            }
            else if(!isCoral) Die();
        }
        if(isCoral) UiManager.GetInstance().LifeText.text = "Health: " + health;
    }

    void Die()
    {
        LevelManager.GetInstance().enemiesLeft--;
        LevelManager.GetInstance().CheckForEnemies();
        Destroy(gameObject);
    }
    
}
