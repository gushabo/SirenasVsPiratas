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
            gameObject.GetComponent<Health>().Die();
        }
    }
}
