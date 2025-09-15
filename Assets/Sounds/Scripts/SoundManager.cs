using System;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    // --- Singleton ---
    public static SoundManager Instance { get; private set; }
    public static SoundManager GetInstance() => Instance;

    [Header("UI")]
    [SerializeField] private Scrollbar scrollBar;

    [Header("Volumen")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultVolume = 0.5f;

    private AudioSource audioSource;
    private const string PREF_KEY = "MasterVolume";
    
    private void Awake()
    {
        
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("SoundManager requiere un AudioSource en el mismo GameObject.");
        }
    }

    private void OnEnable()
    {
        if (scrollBar != null)
            scrollBar.onValueChanged.AddListener(ChangeAudioValue);
    }

    private void Start()
    {
        float volume = PlayerPrefs.GetFloat(PREF_KEY, defaultVolume);
        ApplyVolume(volume);

        if (scrollBar != null)
            scrollBar.value = volume;
    }

    private void OnDisable()
    {
        if (scrollBar != null)
            scrollBar.onValueChanged.RemoveListener(ChangeAudioValue);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    
    public void ChangeAudioValue(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(PREF_KEY, value);
        PlayerPrefs.Save();
    }

    public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volumeScale);
    }
    
    private void ApplyVolume(float value)
    {
        value = Mathf.Clamp01(value);
        
        if (audioSource != null)
            audioSource.volume = value;
    }
    
    
}
