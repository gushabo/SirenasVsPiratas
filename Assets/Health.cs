using System;
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
        else if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            sliderGO.SetActive(false);
        }
    }

    private void Update()
    {
        if (sliderGO.activeSelf)
        {
            sliderGO.transform.rotation = Quaternion.LookRotation(sliderGO.transform.position - cameraPosition.position);
        }
    }

    public void TakeDamage(int damage)
    {
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
