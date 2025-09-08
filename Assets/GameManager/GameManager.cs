using System;
using UnityEngine;

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
    
    [SerializeField] private GameObject pausePanel;

    private void Start()
    {
        gameState = GameState.Play;
        pausePanel.SetActive(false);
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
        }
    }






  

    
    
}

public enum GameState
{
    Play,
    Pause,
    GameOver,
}