using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private EnemyTypes type;
    
    private void Awake()
    {
        gameObject.GetComponent<Health>().maxHealth = type.health;
        gameObject.GetComponent<Damage>().damage = type.damage;
        gameObject.GetComponent<EnemyMovement>().speed = type.speed;
    }
}
