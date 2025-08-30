using System;
using UnityEngine;

public class Health : MonoBehaviour
{

    public int health;
    private int maxHealth = 100;
    public EnemyType enemyType;
    
    private void Awake()
    {

        switch (enemyType)
        {
            case EnemyType.Normal:
            {
                maxHealth = 100;
                break;
            } 
            case EnemyType.Bandit:
            {
                maxHealth = 75;
                break;
            } 
            case EnemyType.Tank:
            {
                maxHealth = 125;
                break;
            }
            case EnemyType.Coral:
            {
                maxHealth = 150;
                break;
            }
                
        }
    }

    void Start() => health = maxHealth;

    public void TakeDamage(int damage)
    {
        health -= damage;
        if(health <= 0 && enemyType== EnemyType.Coral) LoseGame();
        else if(health <= 0 && enemyType!= EnemyType.Coral) Die();
        
    } 
    void Die() => Destroy(gameObject);
    
    void LoseGame(){}
    
}

public enum EnemyType
{
    Normal,
    Bandit,
    Tank,
    Coral
}