using System;
using UnityEngine;

public class Damage : MonoBehaviour
{
    public int damage;
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Coral"))
        {
            GameObject.Find("HealthText").GetComponent<HealthText>().ChangeHealthText(other.gameObject.GetComponent<Health>().health - damage);
            Destroy(gameObject);
        }
    }
}
