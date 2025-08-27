using UnityEngine;

public class Health : MonoBehaviour
{

    public int health;
    private int maxHealth = 100;
    
    void Awake() => health = maxHealth;

    public void TakeDamage(int damage)
    {
        health -= damage;
        if(health <= 0) Die();
    } 
    void Die() => Destroy(gameObject);
}
