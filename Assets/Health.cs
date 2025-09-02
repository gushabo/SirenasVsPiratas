using System;
using UnityEngine;

public class Health : MonoBehaviour
{

    public int health;
    public int maxHealth = 100;
    public bool isCoral;

    void Start() => health = maxHealth;

    public void TakeDamage(int damage)
    {
        health -= damage;
        if(health <= 0 && isCoral) LoseGame();
        else if(health <= 0 && !isCoral) Die();
        
    } 
    void Die() => Destroy(gameObject);
    
    void LoseGame(){}
    
}
