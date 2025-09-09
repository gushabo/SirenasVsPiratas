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
        GameManager.GetInstance().enemiesLeft += 1;
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
            if(isCoral) GameManager.GetInstance().GameOver();
            else if(!isCoral) Die();
            UiManager.GetInstance().LifeText.text = "Health: " + health;
        }
    }

    void Die()
    {
        GameManager.GetInstance().enemiesLeft--;
        GameManager.GetInstance().CheckForEnemies();
        Destroy(gameObject);
    }

    
    
}
