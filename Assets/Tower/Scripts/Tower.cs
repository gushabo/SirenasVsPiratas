using System.Collections;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] public int damageBullet = 20;
    [SerializeField] public float range = 12f;
    [SerializeField] public float fireRate = 1.5f; // tiempo entre ataques
    [SerializeField] public float turnSpeed = 10f;
    [SerializeField] public float retarget = 0.25f;

    [Header("References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;

    private float fireCooldown;
    private Transform currentTarget;
    private bool isPaused;
    private bool canAttack = true; // nuevo control

    public Animator anim;
    [SerializeField] private GameObject tridente;

    void Start()
    {
        InvokeRepeating(nameof(UpdateTarget), 0f, retarget);
        GameManager.GetInstance().onChangeGameState += OnChangeGameStateCallback;
        anim = GetComponent<Animator>();
    }

    public void OnChangeGameStateCallback(GameState newState)
    {
        isPaused = newState != GameState.Play;
    }

    private void Update()
    {
        if (isPaused) return;
        if (!canAttack) return;
        if (currentTarget == null) return;

        // Rotar hacia el objetivo
        Vector3 dir = currentTarget.position - head.position;
        Vector3 direction = new Vector3(dir.x, 0f, dir.z);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            head.rotation = Quaternion.Slerp(head.rotation, lookRotation, turnSpeed * Time.deltaTime);
        }

        // Si ya pasó el tiempo del fireRate, ataca
        if (fireCooldown <= 0f)
        {
            print("atacando");
            anim.SetTrigger("Attack"); // la animación ejecutará el Event "Shoot"
            StartCoroutine(AttackCooldown());
        }
        else
        {
            fireCooldown -= Time.deltaTime;
        }
    }

    IEnumerator AttackCooldown()
    {
        canAttack = false;
        fireCooldown = fireRate; // espera el tiempo completo
        yield return new WaitForSeconds(fireRate);
        canAttack = true;
    }

    public void UpdateTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, ~0, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        Transform bestTarget = null;

        foreach (var h in hits)
        {
            if (!h.CompareTag("Enemy")) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                bestTarget = h.transform;
            }
        }

        currentTarget = (bestTarget != null && (bestTarget.position - transform.position).sqrMagnitude <= range * range)
            ? bestTarget
            : null;
    }

    // 🎯 ESTE MÉTODO se llama desde el Event en la animación
    public void Shoot()
    {
        if (currentTarget == null) return;

        tridente.SetActive(false);
        Bullet b = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation).GetComponent<Bullet>();
        b.damage = damageBullet;
        b.Shoot(currentTarget);
    }

    public void ShowTridente() => tridente.SetActive(true);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }

    public void AddDamage(float amount)
    {
        damageBullet = Mathf.Max(0, damageBullet + Mathf.RoundToInt(amount));
    }

    public void AddFireRate(float amount)
    {
        // Cuanto menor es fireRate, más rápido dispara
        fireRate = Mathf.Max(0.1f, fireRate - amount);
    }

    public void AddRange(float amount)
    {
        range = Mathf.Max(0f, range + amount);
        UpdateTarget();
    }
}
