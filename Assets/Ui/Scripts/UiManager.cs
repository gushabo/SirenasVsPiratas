using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    #region Singleton
    public static UiManager instance { get; private set; }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }
    public static UiManager GetInstance() => instance;
    private void OnDestroy() { if (instance == this) instance = null; }
    #endregion

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
    [SerializeField] public Slider sliderSonido; // Music
    [SerializeField] public Slider sliderSFX;    // SFX

    private void Start()
    {
        if (pausePanel)       pausePanel.SetActive(false);
        if (gameOverPanel)    gameOverPanel.SetActive(false);
        if (winPanel)         winPanel.SetActive(false);
        if (changeRoundPanel) changeRoundPanel.SetActive(false);
        if (countDownPanel)   countDownPanel.SetActive(false);
        if (QuitButton)       QuitButton.SetActive(false);
        // No sincronizamos aquí. Se hace en OnEnable.
    }

    public void ActivateQuitButton()
    {
        if (QuitButton) QuitButton.SetActive(!QuitButton.activeSelf);
    }

    public void ActualizarValorSlider()
    {
        var sm = SoundManager.GetInstance();
        if (!sm) return;

        // Preferimos leer del mixer (en dB) y convertir a 0..1
        if (sm.TryGetMixerDb(out var mDb, out var sDb))
        {
            if (sliderSonido) sliderSonido.SetValueWithoutNotify(SoundManager.DbToLinear(mDb));
            if (sliderSFX)    sliderSFX.SetValueWithoutNotify(SoundManager.DbToLinear(sDb));
        }
        else
        {
            // Fallback: valores cacheados por el manager
            if (sliderSonido) sliderSonido.SetValueWithoutNotify(sm.GetMusicVolume01());
            if (sliderSFX)    sliderSFX.SetValueWithoutNotify(sm.GetSFXVolume01());
        }
    }

    public void SendVolumeToManager()
    {
        var sm = SoundManager.GetInstance();
        if (sm && sliderSonido) sm.SetMusicVolume(sliderSonido.value);
    }

    public void SendSFXToManager()
    {
        var sm = SoundManager.GetInstance();
        if (sm && sliderSFX) sm.SetSFXVolume(sliderSFX.value);
    }

    // --- Rondas (igual que antes) ---
    public void CambioDeRonda(int num)
    {
        if (changeRoundPanel) changeRoundPanel.SetActive(true);
        if (changeRoundText)  changeRoundText.text = $"Ronda {num+1} superada";
        if (changeLvlButton)  changeLvlButton.SetActive(false);
    }
    public void ApagarCambioRondas()           { if (changeRoundPanel) changeRoundPanel.SetActive(false); }
    public void UpdateRoundLevelText(int r, int l) { if (roundsText) roundsText.text = $"Nivel {l+1} / Ronda {r+1}"; }
    public void CambiarDeNivel()
    {
        if (changeRoundText) changeRoundText.text = "Felicidades pasaste de nivel";
        if (changeLvlButton) changeLvlButton.SetActive(true);
        if (changeRoundPanel) changeRoundPanel.SetActive(true);
    }
    public void CambiarDeNivel2()
    {
        if (changeLvlButton)  changeLvlButton.SetActive(false);
        if (changeRoundPanel) changeRoundPanel.SetActive(false);
    }
    public void TurnOffPausePanel() { if (pausePanel) pausePanel.SetActive(false); }

    // ====== Binding de sliders ======
    void OnEnable()  { StartCoroutine(BindWhenReady()); }

    IEnumerator BindWhenReady()
    {
        while (SoundManager.GetInstance() == null) yield return null;
        var sm = SoundManager.GetInstance();

        // 1) Sincroniza sin notificar
        ActualizarValorSlider();

        // 2) UI -> Manager
        if (sliderSonido) sliderSonido.onValueChanged.AddListener(sm.SetMusicVolume);
        if (sliderSFX)    sliderSFX.onValueChanged.AddListener(sm.SetSFXVolume);

        // 3) Manager -> UI
        sm.OnVolumesChanged += HandleVolumesChanged;
    }

    void OnDisable()
    {
        var sm = SoundManager.GetInstance();
        if (sm != null) sm.OnVolumesChanged -= HandleVolumesChanged;

        if (sliderSonido) sliderSonido.onValueChanged.RemoveAllListeners();
        if (sliderSFX)    sliderSFX.onValueChanged.RemoveAllListeners();
    }

    void HandleVolumesChanged(float music01, float sfx01)
    {
        if (sliderSonido) sliderSonido.SetValueWithoutNotify(music01);
        if (sliderSFX)    sliderSFX.SetValueWithoutNotify(sfx01);
    }
}
