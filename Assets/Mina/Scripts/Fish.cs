using System.Collections;
using UnityEngine;

public class Fish : MonoBehaviour
{
    [Header("Preparacion")] 
    [SerializeField] string[] triggerTags = {"Enemy"};
    [SerializeField] float armTime = 0.25f;
    [SerializeField] float triggerDelay = 1f;

    [Header("Explosión")] 
    [SerializeField] float radius = 3f;
    [SerializeField] int damage = 20; 
    [SerializeField] GameObject vfxPrefab;

    private bool armed;
    private bool isPaused;
    private bool detonationStarted;              // <-- evita múltiples corrutinas
    private readonly Collider[] buffer = new Collider[64];

    void Start()
    {
        Invoke(nameof(Arm), armTime);

        var gm = GameManager.GetInstance();
        if (gm != null)
        {
            gm.onChangeGameState += OnChangeGameStateCallback;
            isPaused = gm.gameState != GameState.Play; // estado inicial
        }
    }

    void OnDestroy()
    {
        var gm = GameManager.GetInstance();
        if (gm != null) gm.onChangeGameState -= OnChangeGameStateCallback;
    }

    public void OnChangeGameStateCallback(GameState newState)
    {
        isPaused = newState != GameState.Play;
    }

    void Arm() => armed = true; 

    void OnTriggerEnter(Collider other)
    {
        if (!armed || detonationStarted) return;
        if (!MatchesTag(other)) return;

        detonationStarted = true;
        StartCoroutine(ExplodeAfterPausedAware(triggerDelay));
    }

    bool MatchesTag(Collider c)
    {
        for (int i = 0; i < triggerTags.Length; i++)
            if (c.CompareTag(triggerTags[i])) return true;
        return false;
    }

    // Corrutina "pausable": solo descuenta tiempo cuando NO está en pausa
    IEnumerator ExplodeAfterPausedAware(float delay)
    {
        float remaining = delay;
        while (remaining > 0f)
        {
            // si está pausado, espera al siguiente frame sin descontar tiempo
            if (!isPaused)
                remaining -= Time.deltaTime;
            yield return null;
        }
        Explode();
    }

    public void Explode()
    {
        if (vfxPrefab) Instantiate(vfxPrefab, transform.position, Quaternion.identity);

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer);
        for (int i = 0; i < count; i++)
        {
            var col = buffer[i];
            var hp = col.GetComponentInParent<Health>();
            if (hp != null && damage > 0) hp.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
