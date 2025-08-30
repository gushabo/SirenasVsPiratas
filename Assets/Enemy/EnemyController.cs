using System;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] public EnemyType type;

    private void Start()
    {
        gameObject.GetComponent<Health>().enemyType = type;
        gameObject.GetComponent<Damage>().enemyType = type;
        gameObject.GetComponent<EnemyMovement>().enemyType = type;
    }
}
