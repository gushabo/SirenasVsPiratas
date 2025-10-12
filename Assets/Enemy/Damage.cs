using System;
using UnityEngine;

public class Damage : MonoBehaviour
{
    public int damage;
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Coral"))
        {
            other.gameObject.GetComponent<Health>().TakeDamage(damage);
            // hace danio 1 vez y luego se muere
            LevelManager.GetInstance().enemiesLeft--;
            LevelManager.GetInstance().CheckForEnemies();
            Destroy(gameObject);
        }
    }
}
