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
    
    // Win system
    public int enemiesLeft;
    public int roundsLeft;
    public int actualRound;
    public bool Lose;
    
    public List<GameObject> EnemySpawners;

    public bool canPause;
    
    private void OnEnable()
    {
        //roundsLeft = EnemySpawners.Count - 1;
        Lose = false;
        actualRound = 0;
        enemiesLeft = 0;
        if (EnemySpawners.Count > 0)
            EnemySpawners[actualRound].SetActive(true);
    }


    private void Start()
    {
        gameState = GameState.Play;
        canPause = true;
        //UiManager.GetInstance().roundsText.text = "Ronda " + (actualRound+1) + " de " + (roundsLeft+1);
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (canPause)
            {
                PauseGame();
            }
        }
    }

    public void CheckForEnemies()
    {
        if (enemiesLeft <= 0 && !Lose && roundsLeft  == actualRound)
        {
            Win();
        }
        else
        {
            EndOfRound();
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

    public void EndOfRound()
    {
        if (roundsLeft == actualRound) return;
        
        EnemySpawners[actualRound].SetActive(false);
        actualRound++;
        canPause = false;
        UiManager.GetInstance().roundsText.text = "Ronda " + (actualRound+1) + " de " + (roundsLeft+1);
        UiManager.GetInstance().changeRoundPanel.SetActive(true);
        StartCoroutine(ChangeRound());
    }

    public IEnumerator ChangeRound()
    {
        yield return new WaitForSeconds(1f);
        UiManager.GetInstance().changeRoundPanel.SetActive(false);
        UiManager.GetInstance().countDownPanel.SetActive(true);
        UiManager.GetInstance().countDownText.text = "3";
        yield return new WaitForSeconds(1f);
        UiManager.GetInstance().countDownText.text = "2";
        yield return new WaitForSeconds(1f);
        UiManager.GetInstance().countDownText.text = "1";
        yield return new WaitForSeconds(1f);
        UiManager.GetInstance().countDownPanel.SetActive(false);
        canPause = true;
        EnemySpawners[actualRound].SetActive(true);
    }

}

public enum GameState
{
    Play,
    Pause,
    GameOver
}