using System;
using Unity.VisualScripting;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    
    [Header("Damage")]
    [SerializeField] public int damage;
    [SerializeField] private float speed;
    [SerializeField] private float lifeTime;
    
    public Transform target;
    private float life;

    public bool isPaused = false;
    
    
    [Header("Orientation")]
    [SerializeField] private Transform rotateTransform;        // si es null, usa this.transform
    [SerializeField] private Vector3 modelForwardAxis = Vector3.forward; // eje “nariz” del modelo
    [SerializeField] private Vector3 rotationOffsetEuler;      // offset fijo (p. ej., (0,0,90))
    [SerializeField] private float turnSpeed = 20f;            // suavizado de giro
    
    private void Start()
    {
        if (rotateTransform == null) rotateTransform = transform;
        GameManager.GetInstance().onChangeGameState += OnChangeGameStateCallback;
    }

    public void OnChangeGameStateCallback(GameState newState)
    {
        isPaused = newState != GameState.Play;
    }

    public void Shoot(Transform actualTarget)
    {
        target = actualTarget;
        life = 0f;
    }

    private void Update()
    {
        
        if (isPaused) return;
        
        // Tiempo de vida
        life += Time.deltaTime;
        
        // Comprobar objetivo y tiempo de vida
        if (life >= lifeTime || target == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Mover
        Vector3 dir = target.position - transform.position;
        float step = speed * Time.deltaTime;

        if (dir.magnitude <= step) { Hit(); return; }
        transform.position += dir.normalized * step;

        // Orientar correctamente usando quaternions
        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // Corrige si tu modelo no usa +Z como “frente”
        Quaternion axisFix = Quaternion.FromToRotation(Vector3.forward, modelForwardAxis.normalized);

        // Offset opcional (roll/pitch extra)
        Quaternion rotOffset = Quaternion.Euler(rotationOffsetEuler);

        Quaternion desired = look * axisFix * rotOffset;
        rotateTransform.rotation = Quaternion.Slerp(rotateTransform.rotation, desired, turnSpeed * Time.deltaTime);
    
        
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
