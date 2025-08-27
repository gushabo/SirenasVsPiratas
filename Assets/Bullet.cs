using System;
using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private int damage;
    [SerializeField] private float speed;
    [SerializeField] private float lifeTime;
    
    public Transform target;
    private float life;

    public void Shoot(Transform actualTarget)
    {
        target = actualTarget;
        life = 0f;
    }

    private void Update()
    {
        // Tiempo de vida
        life += Time.deltaTime;
        
        // Comprobar objetivo y tiempo de vida
        if (life >= lifeTime || target == null)
        {
            Destroy(gameObject);
            return;
        }
        
        Vector3 dir = target.position - transform.position;
        float distance = speed * Time.deltaTime;

        if (dir.magnitude < distance)
        {
            Hit();
            return;
        }
        
        transform.position += dir.normalized * distance;
        transform.forward = Vector3.Lerp(transform.forward, dir.normalized, 0.5f);
        
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            /*Health hp = target.GetComponent<Health>();
            if (hp) hp.TakeDamage(damage);
            Destroy(gameObject);*/
            Hit();
        }
    }


    public void Hit()
    {
        if (target != null)
        {
            Health hp = target.GetComponent<Health>();
            if (hp) hp.TakeDamage(damage);
            Destroy(gameObject);
        }
        
    }
    
}
