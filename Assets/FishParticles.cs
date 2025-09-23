using System;
using UnityEngine;

public class FishParticles : MonoBehaviour
{
    private bool isPaused;
    private ParticleSystem[]  particleSystems;
    
    
    void Start()
    {
        GameManager.GetInstance().onChangeGameState += OnChangeGameStateCallback;
        if(GameManager.GetInstance().gameState == GameState.Pause) isPaused = true;
    }
    
    public void OnChangeGameStateCallback(GameState newState)
    {
        isPaused = newState != GameState.Play;
        
        particleSystems = GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (isPaused)
            {
                particleSystem.Pause();
            }
            else
            {
                particleSystem.Play();
            }
        }
    }

    private void OnDestroy()
    {
        GameManager.GetInstance().onChangeGameState -= OnChangeGameStateCallback;
    }
}
