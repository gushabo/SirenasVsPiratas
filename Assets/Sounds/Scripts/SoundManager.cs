using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    public static SoundManager GetInstance() => Instance;

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string sfxParam   = "SFXVol";

    [Range(0f,1f)] public float defaultMusic = 1f;
    [Range(0f,1f)] public float defaultSFX   = 1f;

    [Header("Audio Sources (route to mixer groups)")]
    [SerializeField] public AudioSource musicSource; // Output -> Music group
    [SerializeField] public AudioSource sfxSource;   // Output -> SFX group

    const string PREF_MUSIC = "VOL_MUSIC";
    const string PREF_SFX   = "VOL_SFX";

    // --- Solo una vez por sesión ---
    static bool sessionVolumesInitialized = false;

    float music01, sfx01;

    public event System.Action<float,float> OnVolumesChanged; // (music01, sfx01)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Los AudioSource a 1; el mixer controla el volumen real
        if (musicSource) musicSource.volume = 1f;
        if (sfxSource)   sfxSource.volume   = 1f;

        if (!sessionVolumesInitialized)
        {
            // Primera vez en esta sesión: fuerza sliders/volumen a 1
            SetMusicVolume(1f);
            SetSFXVolume(1f);
            sessionVolumesInitialized = true;
        }
        else
        {
            // Siguientes cargas en la misma sesión: respeta PlayerPrefs
            SetMusicVolume(PlayerPrefs.GetFloat(PREF_MUSIC, defaultMusic));
            SetSFXVolume  (PlayerPrefs.GetFloat(PREF_SFX,   defaultSFX));
        }
    }

    public void SetMusicVolume(float value01)
    {
        music01 = Mathf.Clamp01(value01);
        if (mixer) mixer.SetFloat(musicParam, LinearToDb(music01));
        PlayerPrefs.SetFloat(PREF_MUSIC, music01);
        PlayerPrefs.Save();
        OnVolumesChanged?.Invoke(music01, sfx01);
    }

    public void SetSFXVolume(float value01)
    {
        sfx01 = Mathf.Clamp01(value01);
        if (mixer) mixer.SetFloat(sfxParam, LinearToDb(sfx01));
        PlayerPrefs.SetFloat(PREF_SFX, sfx01);
        PlayerPrefs.Save();
        OnVolumesChanged?.Invoke(music01, sfx01);
    }

    public float GetMusicVolume01() => music01;
    public float GetSFXVolume01()   => sfx01;

    public static float LinearToDb(float v) => (v <= 0.0001f) ? -80f : Mathf.Log10(v) * 20f;
    public static float DbToLinear(float db) => Mathf.Pow(10f, db / 20f);

    // Lee dB reales del mixer (true = parámetros existen)
    public bool TryGetMixerDb(out float musicDb, out float sfxDb)
    {
        musicDb = 0f; sfxDb = 0f;
        if (!mixer) return false;
        bool okM = mixer.GetFloat(musicParam, out musicDb);
        bool okS = mixer.GetFloat(sfxParam,   out sfxDb);
        return okM && okS;
    }

    // Opcional de reproducción
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (!musicSource || !clip) return;
        musicSource.loop = loop; musicSource.clip = clip; musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volume01 = 1f)
    {
        if (!sfxSource || !clip) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume01));
    }
}
