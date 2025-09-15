using System;
using TMPro;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    // ----- SingleTon ---------
    #region Singleton
    public static UiManager instance { get; private set; }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
    }
    
    public static UiManager GetInstance() => instance;
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    #endregion
    // ------ Fin del singleton  ---------
    [Header("Pause Panel")]
    [SerializeField] public GameObject pausePanel;
    [SerializeField] public TextMeshProUGUI pauseText;
    [Header("GameOver Panel")]
    [SerializeField] public GameObject gameOverPanel;
    [SerializeField] public TextMeshProUGUI gameOverText;
    [Header("Win Panel")]
    [SerializeField] public GameObject winPanel;
    [SerializeField] public TextMeshProUGUI winText;
    [Header("Life Panel")]
    [SerializeField] public GameObject LifePanel;
    [SerializeField] public TextMeshProUGUI LifeText;
    [Header("Cambio de rondas")]
    [SerializeField] public GameObject changeRoundPanel;
    [SerializeField] public TextMeshProUGUI changeRoundText;
    [SerializeField] public GameObject countDownPanel;
    [SerializeField] public TextMeshProUGUI countDownText;
    [Header("Aviso Rondas")]
    [SerializeField] public TextMeshProUGUI roundsText;
    private void Start()
    {
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        changeRoundPanel.SetActive(false);
        countDownPanel.SetActive(false);
    }
}
