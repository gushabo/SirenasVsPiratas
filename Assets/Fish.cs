using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [Header("Preparacion")] 
    [SerializeField] string[] triggerTags = {"Enemy"};
    [SerializeField] float armTime = 0.25f;
    [SerializeField] float triggerDelay = 5f;

    [Header("Explocion")] 
    [SerializeField] float radius;
    [SerializeField] int damage; 
    [SerializeField] GameObject vfxPrefab;

    private bool armed;

    

    private void Start()
    {
        Invoke(nameof(Arm), armTime);
    }

    public void Arm() => armed = true; 

    private void OnTriggerEnter(Collider other)
    {
        if (!armed) return;
        if(!MatchesTag(other)) return;
        
        print("Se va a detonar el fish");
        
        StartCoroutine(ExplodeAfter(triggerDelay));
        
    }
    
    bool MatchesTag(Collider c)
    {
        for (int i = 0; i < triggerTags.Length; i++)
            if (c.CompareTag(triggerTags[i])) return true;
        return false;
    }
    
    // Explosion
    private readonly Collider[] buffer = new Collider[64];

    IEnumerator ExplodeAfter(float t)
    {
        yield return new WaitForSeconds(t);
        Explode();
    }

    public void Explode()
    {
        if (vfxPrefab) Instantiate(vfxPrefab, transform.position, Quaternion.identity);
        
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, radius, buffer);
        
        for (int i = 0; i < count; i++)
        {
            var col = buffer[i];

            // Daño
            var hp = col.GetComponentInParent<Health>();
            if (hp != null && damage > 0f) hp.TakeDamage(damage);
            print("Hizo el dmg");
        }

        Destroy(gameObject);
    }
    
    // Gizmos para ver el radio
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
