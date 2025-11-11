using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] public GameObject QuitButton;
    
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
    [SerializeField] public GameObject changeLvlButton;
    
    [Header("Cosas Cartas")]
    [SerializeField] public GameObject CardCanvasSystem;
    
    [Header("Aviso Rondas")]
    [SerializeField] public TextMeshProUGUI roundsText;
    
    [Header("Sonido")]
    [SerializeField] public Slider sliderSonido;
    
    private void Start()
    {
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);
        changeRoundPanel.SetActive(false);
        countDownPanel.SetActive(false);
        QuitButton.SetActive(false);
    }
    
    //Pausa
    public void ActivateQuitButton()
    {
        QuitButton.SetActive(!QuitButton.activeSelf);
    }

    // Sonidos
    public void ActualizarValorSlider()
    {
        sliderSonido.value = SoundManager.GetInstance().audioSource.volume;
    }

    public void SendVolumeToManager()
    {
        SoundManager.GetInstance().ChangeAudioValue(sliderSonido.value);
    }

    // Rondas y niveles
    public void CambioDeRonda(int num)
    {
        changeRoundPanel.SetActive(true);
        changeRoundText.text = ($"Ronda {num+1} superada");
        changeLvlButton.SetActive(false);
    }

    public void ApagarCambioRondas()
    {
        changeRoundPanel.SetActive(false);
    }

    public void UpdateRoundLevelText(int roundIndex, int levelIndex)
    {
        roundsText.text = $"Nivel {levelIndex + 1} / Ronda {roundIndex + 1}";
    }

    public void CambiarDeNivel()
    {
        changeRoundText.text = ($"Felicidades pasaste de nivel");
        changeLvlButton.SetActive(true);
        changeRoundPanel.SetActive(true);
    }

    public void CambiarDeNivel2()
    {
        changeLvlButton.SetActive(false);
        changeRoundPanel.SetActive(false);
    }

    public void TurnOffPausePanel()
    {
        pausePanel.SetActive(false);
    }
    
    
}
