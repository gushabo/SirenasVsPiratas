using System;
using UnityEngine;

public class Damage : MonoBehaviour
{
    public int damage;
    public EnemyType enemyType;

    private void Start()
    {
        switch (enemyType)
        {
            case EnemyType.Normal:
            {
                damage = 10;
                break;
            } 
            case EnemyType.Bandit:
            {
                damage = 8;
                break;
            } 
            case EnemyType.Tank:
            {
                damage = 13;
                break;
            } 
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Coral"))
        {
            GameObject.Find("HealthText").GetComponent<HealthText>().ChangeHealthText(other.gameObject.GetComponent<Health>().health - damage);
            Destroy(gameObject);
        }
    }
}
