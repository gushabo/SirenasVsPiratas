using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{

    public int health;
    public int maxHealth = 100;
    public bool isCoral;
    
    [SerializeField] public Slider healthSlider;
    [SerializeField] public GameObject sliderGO;
    private Transform cameraPosition;
    
    [SerializeField] public Material hitMaterial;
    [SerializeField] public GameObject body;
    Coroutine flashCo;

    void Start()
    {
        if (healthSlider == null || sliderGO == null)
        {
            var slider = GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                healthSlider = slider;
                sliderGO = slider.gameObject;
            }
        }

        cameraPosition = CameraManager.GetInstance().transform;
    
        health = maxHealth;
        if (isCoral)
        {
            UiManager.GetInstance().LifeText.text = "Health: " + health;
        }
        
        healthSlider.maxValue = maxHealth;
        sliderGO.SetActive(false);
    }

    private void Update()
    {
        if (sliderGO.activeSelf)
        {
            sliderGO.transform.rotation = Quaternion.LookRotation(sliderGO.transform.position - cameraPosition.position);
        }
    }

    public IEnumerator wait(float seconds)
    {
        var originalMaterial = body.GetComponent<SkinnedMeshRenderer>().material;
        body.GetComponent<SkinnedMeshRenderer>().material = hitMaterial;
        yield return new WaitForSeconds(seconds);
        body.GetComponent<SkinnedMeshRenderer>().material = originalMaterial;
        flashCo = null;
    }

    public void CallWait()
    {
        if(flashCo != null) StopCoroutine(flashCo);
        flashCo = StartCoroutine(wait(0.3f));
    }
    
    public void TakeDamage(int damage)
    {

        if (!isCoral)
        {
            CallWait();
        }
        
        health -= damage;
        sliderGO.SetActive(true);
        healthSlider.value = health;
        if (health <= 0)
        {
            health = 0;
            if (isCoral)
            {
                GameManager.GetInstance().Lose = true;
                GameManager.GetInstance().ChangeGameState(GameState.Pause);
                UiManager.GetInstance().TurnOffPausePanel();
            }
            else if(!isCoral) Die();
        }
        if(isCoral) UiManager.GetInstance().LifeText.text = "Health: " + health;
    }

    void Die()
    {
        // Se muere el enemigo y hace danio una vez
        LevelManager.GetInstance().enemiesLeft--;
        LevelManager.GetInstance().CheckForEnemies();
        Destroy(gameObject);
    }
    
}
