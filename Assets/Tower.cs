using System;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Stats")] 
    [SerializeField] private float range = 12f;
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float retarget = 0.25f;
    
    [Header("References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    
    private float fireCooldown;
    private Transform currentTarget;

    void Awake()
    {
        InvokeRepeating(nameof(UpdateTarget), 0f, retarget);
    }

    private void Update()
    {
        fireCooldown -= Time.deltaTime;
        
        if (currentTarget == null) return;
        
        Vector3 dir = currentTarget.position - head.position;
        Vector3 direction = new Vector3(dir.x, 0f, dir.z);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            head.rotation = Quaternion.Slerp(head.rotation, lookRotation, turnSpeed * Time.deltaTime);
        }

        if (fireCooldown <= 0f)
        {
            Shoot(currentTarget);
            fireCooldown = 1f / fireRate;
        }
    }

    public void UpdateTarget()
    {
        
        Collider[] hits = Physics.OverlapSphere(transform.position, range, ~0, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        Transform bestTarget = null;

        foreach (var h in hits)
        {
            if(!h.CompareTag("Enemy")) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                bestTarget = h.transform;
            }
        }
        
        currentTarget = (bestTarget != null && (bestTarget.position - transform.position).sqrMagnitude <= range * range) ? bestTarget : null;
        
    }


    void Shoot(Transform target)
    {
        print("se disparo");
        Bullet b = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation).GetComponent<Bullet>();
        b.Shoot(target);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
    
}
