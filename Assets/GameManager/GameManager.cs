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
    
    // UI Panels
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    
    // Win system
    public int enemiesLeft;

    private void Start()
    {
        
        gameState = GameState.Play;
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        
    }

    public void ChangeGameState(GameState newGameState)
    {
        gameState = newGameState;
        onChangeGameState?.Invoke(gameState);
        pausePanel.SetActive(!pausePanel.activeSelf);
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
            
            else if (gameState == GameState.GameOver)
            {
                gameOverPanel.SetActive(true);
                ChangeGameState(GameState.Pause);
                Invoke(nameof(GameOver),3f);
            }
        }

        if (enemiesLeft == 0)
        {
            Win();
        }
        
    }
    
    public void GameOver() => ChangeMainScene();

    public void Win()
    {
        winPanel.SetActive(true);
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