using System;
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
        enemiesLeft = -1;
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
    
    // Win system
    public int enemiesLeft;
    public bool Lose;
    
    private void Start()
    {
        Lose = false;
        gameState = GameState.Play;
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
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

    public void CheckForEnemies()
    {
        if (enemiesLeft <= 0 && !Lose)
        {
            Win();
        }
    }

    public void GameOver()
    {
        UiManager.GetInstance().gameOverPanel.SetActive(true);
        Lose = true;
        Invoke(nameof(ChangeMainScene), 3f);
    }

    public void Win()
    {
        UiManager.GetInstance().winPanel.SetActive(true);
        Invoke(nameof(ChangeMainScene), 3f);
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