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

    [SerializeField] public AudioSource musicSource; // Output -> Music
    [SerializeField] public AudioSource sfxSource;   // Output -> SFX

    const string PREF_MUSIC = "VOL_MUSIC";
    const string PREF_SFX   = "VOL_SFX";

    // valores actuales en 0..1
    float music01;
    float sfx01;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // inicia a 1 la ganancia de los AudioSource; el mixer hará el volumen
        if (musicSource) musicSource.volume = 1f;
        if (sfxSource)   sfxSource.volume   = 1f;

        // cargar prefs y aplicar
        float m = PlayerPrefs.GetFloat(PREF_MUSIC, defaultMusic);
        float s = PlayerPrefs.GetFloat(PREF_SFX,   defaultSFX);
        SetMusicVolume(m);
        SetSFXVolume(s);
    }

    public void SetMusicVolume(float value01)
    {
        music01 = Mathf.Clamp01(value01);
        if (mixer) mixer.SetFloat(musicParam, LinearToDb(music01));
        PlayerPrefs.SetFloat(PREF_MUSIC, music01);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value01)
    {
        sfx01 = Mathf.Clamp01(value01);
        if (mixer) mixer.SetFloat(sfxParam, LinearToDb(sfx01));
        PlayerPrefs.SetFloat(PREF_SFX, sfx01);
        PlayerPrefs.Save();
    }

    public float GetMusicVolume01() => music01;
    public float GetSFXVolume01()   => sfx01;

    static float LinearToDb(float v) => (v <= 0.0001f) ? -80f : Mathf.Log10(v) * 20f;

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
