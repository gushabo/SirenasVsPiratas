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
                
        }
    }

    void Start() => health = maxHealth;

    public void TakeDamage(int damage)
    {
        health -= damage;
        if(health <= 0) Die();
    } 
    void Die() => Destroy(gameObject);

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Coral"))
        {
            Destroy(gameObject);
        }
    }
}

public enum EnemyType
{
    Normal,
    Bandit,
    Tank
}