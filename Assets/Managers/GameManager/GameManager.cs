using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // ----- SingleTon ---------
    #region Singleton
    public static GameManager instance { get; private set; }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    public static GameManager GetInstance() => instance;
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    #endregion
    // ------ Fin del singleton  ---------
    
    public GameState gameState;
    public Action<GameState> onChangeGameState;
    
    // Win or lose
    public bool Lose;

    public bool canPause;
    
    private void OnEnable()
    {
        Lose = false;
    }


    private void Start()
    {
        gameState = GameState.Play;
        canPause = true;
    }

    public void PauseGame()
    {
        if (canPause)
        {
            if (gameState == GameState.Pause)
            {
                ChangeGameState(GameState.Play);
            }
            else if (gameState == GameState.Play)
            {
                ChangeGameState(GameState.Pause);
            }
        }
    }

    public void ChangeGameState(GameState newGameState)
    {
        gameState = newGameState;
        onChangeGameState?.Invoke(gameState);
        if (newGameState == GameState.Play)
        {
            UiManager.GetInstance().pausePanel.SetActive(false);
            
        }
        else if (newGameState == GameState.Pause)
        {
            UiManager.GetInstance().pausePanel.SetActive(true);    
        }else if (newGameState == GameState.GameOver)
        {
            UiManager.GetInstance().gameOverPanel.SetActive(true);
         
        }
    }

   
    public void GameOver()
    {
        UiManager.GetInstance().gameOverPanel.SetActive(true);
        UiManager.GetInstance().CardCanvasSystem.SetActive(false);
     
    }

    public void Win()
    {
        UiManager.GetInstance().winPanel.SetActive(true);
        UiManager.GetInstance().CardCanvasSystem.SetActive(false);
    
    }

    public void ChangeMainScene()
    {
        SceneManager.LoadScene("MainMenu");
    }

}

public enum GameState
{
    Play,
    Pause,
    GameOver
}