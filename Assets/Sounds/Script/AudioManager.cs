// AudioManager.cs (COMPLETO)

using System;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    [Header("Banco de Sonidos")]
    public SoundBank bank;

    [Header("Música")]
    public float musicCrossfade = 1.5f;
    public AudioMixer mixer; // para ducking (opcional)
    [Tooltip("Nombre del parámetro expuesto en el Mixer para volumen de música (ej: MusicVolume)")]
    public string mixerMusicParam = "MusicVolume";

    [Header("Pooling")]
    public int poolSize = 24;

    AudioSource musicA, musicB, activeMusic;
    readonly Dictionary<string, SoundBank.SoundEntry> dict = new();
    readonly Dictionary<string, float> lastPlayedTime = new();
    readonly Dictionary<string, int> playingCount = new();

    // Para controlar y detener sonidos por ID/handle
    readonly Dictionary<string, List<AudioSource>> activeById = new();

    readonly Queue<AudioSource> pool = new();
    readonly HashSet<AudioSource> inUse = new();

    Coroutine duckRoutine;
    float cachedMusicDb = 0f;   // valor actual del mixer para música

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        BuildDictionary();
        BuildPool();
        BuildMusic();

        // Cachea el valor inicial del parámetro de música si existe
        if (mixer && !string.IsNullOrEmpty(mixerMusicParam))
            mixer.GetFloat(mixerMusicParam, out cachedMusicDb);
    }

    private void Start()
    {
        PlayMusic("music_background");
    }

    void BuildDictionary()
    {
        dict.Clear();
        activeById.Clear();

        if (!bank) { Debug.LogWarning("AudioManager: no hay SoundBank asignado."); return; }

        foreach (var s in bank.sounds)
        {
            if (string.IsNullOrEmpty(s.id)) continue;
            dict[s.id] = s;

            if (!playingCount.ContainsKey(s.id)) playingCount[s.id] = 0;
            if (!lastPlayedTime.ContainsKey(s.id)) lastPlayedTime[s.id] = -999f;
            if (!activeById.ContainsKey(s.id)) activeById[s.id] = new List<AudioSource>(8);
        }
    }

    void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.dopplerLevel = 0f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.enabled = true;
            pool.Enqueue(src);
        }
    }

    void BuildMusic()
    {
        musicA = gameObject.AddComponent<AudioSource>();
        musicB = gameObject.AddComponent<AudioSource>();
        foreach (var m in new[] { musicA, musicB })
        {
            m.loop = true;
            m.playOnAwake = false;
            m.spatialBlend = 0f;
            m.priority = 256; // baja prioridad, no compite con SFX
        }
        activeMusic = musicA;
    }

    AudioSource GetSource()
    {
        if (pool.Count > 0)
        {
            var src = pool.Dequeue();
            inUse.Add(src);
            return src;
        }

        // Si se agota el pool, reutiliza la primera fuente en uso (fallback sencillo)
        foreach (var s in inUse)
            return s;

        return null;
    }

    void ReleaseSource(AudioSource src, string id = null)
    {
        if (!src) return;

        if (!string.IsNullOrEmpty(id) && activeById.TryGetValue(id, out var list))
            list.Remove(src);

        src.Stop();
        src.clip = null;
        src.outputAudioMixerGroup = null;
        src.transform.parent = transform;
        src.transform.localPosition = Vector3.zero;
        src.spatialBlend = 0f;
        src.loop = false;
        src.minDistance = 1f;
        src.maxDistance = 25f;
        src.priority = 128;
        src.volume = 1f;
        src.pitch = 1f;

        inUse.Remove(src);
        pool.Enqueue(src);
    }

    AudioClip PickClip(SoundBank.SoundEntry s)
    {
        if (s.clips == null || s.clips.Count == 0) return null;
        if (s.clips.Count == 1) return s.clips[0];
        int i = Random.Range(0, s.clips.Count);
        return s.clips[i];
    }

    bool CanPlay(SoundBank.SoundEntry s)
    {
        float t = Time.unscaledTime;
        if (t - lastPlayedTime[s.id] < s.cooldown) return false;
        if (playingCount[s.id] >= s.maxSimultaneous) return false;
        return true;
    }

    IEnumerator ReturnWhenDone(AudioSource src, SoundBank.SoundEntry s)
    {
        while (src && src.isPlaying)
            yield return null;

        playingCount[s.id] = Mathf.Max(0, playingCount[s.id] - 1);
        ReleaseSource(src, s.id);
    }

    IEnumerator DuckMusic(float amount01, float seconds)
    {
        if (!mixer || string.IsNullOrEmpty(mixerMusicParam))
            yield break;

        // amount01 0..1 => atenuación en dB (hasta -10 dB por ejemplo)
        float targetDeltaDb = Mathf.Lerp(0f, -10f, Mathf.Clamp01(amount01));
        mixer.GetFloat(mixerMusicParam, out float startDb);

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = t / seconds;
            float db = Mathf.Lerp(startDb, startDb + targetDeltaDb, k);
            mixer.SetFloat(mixerMusicParam, db);
            yield return null;
        }

        // Mantén el duck mientras dure el frame; la des-ducción se hace fuera
        cachedMusicDb = startDb + targetDeltaDb;

        // Esperar a que no haya más duckings en curso sería complejo aquí;
        // este método solo baja. La subida ocurre en UnduckMusic().
    }

    IEnumerator UnduckMusic(float seconds)
    {
        if (!mixer || string.IsNullOrEmpty(mixerMusicParam))
            yield break;

        mixer.GetFloat(mixerMusicParam, out float startDb);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = t / seconds;
            float db = Mathf.Lerp(startDb, cachedMusicDb - (cachedMusicDb - startDb), k); // lineal hacia el valor cacheado original
            // Mejor: sube hacia el valor "base" (startDb es el actual, necesitamos el valor de referencia)
            // Ajustamos a 0 dB como ejemplo si desconocido
            float baseDb = 0f;
            float outDb = Mathf.Lerp(startDb, baseDb, k);
            mixer.SetFloat(mixerMusicParam, outDb);
            yield return null;
        }
        mixer.SetFloat(mixerMusicParam, 0f);
        cachedMusicDb = 0f;
    }

    // ---------- API PÚBLICA ----------

    /// <summary>
    /// Reproduce un SFX por ID. Si 'follow' no es nulo, el sonido sigue al transform.
    /// </summary>
    public void Play(string id, Vector3? atWorld = null, Transform follow = null, float vol01 = 1f, float pitch = -999f)
    {
        if (string.IsNullOrEmpty(id) || !dict.ContainsKey(id)) { Debug.LogWarning($"AudioManager: id '{id}' no encontrado."); return; }
        var s = dict[id];
        if (!CanPlay(s)) return;

        var clip = PickClip(s);
        if (!clip) return;

        var src = GetSource();
        if (!src) return;

        // Config
        src.outputAudioMixerGroup = s.outputGroup;
        src.clip = clip;
        src.loop = s.loop;
        src.spatialBlend = s.spatialBlend;
        src.minDistance = s.minDistance;
        src.maxDistance = s.maxDistance;
        src.priority = s.priority;
        src.volume = Mathf.Clamp01(s.volume * vol01);
        src.pitch = (pitch == -999f) ? Random.Range(s.pitchRandom.x, s.pitchRandom.y) : pitch;

        // Posición/seguimiento
        if (follow)
        {
            src.transform.parent = follow;
            src.transform.localPosition = Vector3.zero;
        }
        else if (atWorld.HasValue)
        {
            src.transform.parent = null;
            src.transform.position = atWorld.Value;
        }
        else
        {
            src.transform.parent = transform; // 2D
            src.transform.localPosition = Vector3.zero;
        }

        // Controles
        lastPlayedTime[s.id] = Time.unscaledTime;
        playingCount[s.id]++;

        // Registrar handle
        activeById[s.id].Add(src);

        // Ducking opcional de música
        if (s.musicDuckAmount > 0f)
        {
            if (duckRoutine != null) StopCoroutine(duckRoutine);
            duckRoutine = StartCoroutine(DuckMusic(s.musicDuckAmount, s.musicDuckSeconds));
            // Programar des-duck al terminar el clip (aprox)
            StartCoroutine(UnduckAfter(src, s.musicDuckSeconds));
        }

        src.Play();
        StartCoroutine(ReturnWhenDone(src, s));
    }

    IEnumerator UnduckAfter(AudioSource src, float seconds)
    {
        // Espera a que pase el fade-in de duck y un rato de reproducción
        yield return new WaitForSecondsRealtime(seconds + 0.05f);

        // Si el clip terminó, sube la música.
        if (!src || !src.isPlaying)
        {
            if (duckRoutine != null) StopCoroutine(duckRoutine);
            duckRoutine = StartCoroutine(UnduckMusic(seconds));
        }
        else
        {
            // Si sigue sonando (p.ej. loop largo), espera a que termine
            while (src && src.isPlaying) yield return null;
            if (duckRoutine != null) StopCoroutine(duckRoutine);
            duckRoutine = StartCoroutine(UnduckMusic(seconds));
        }
    }

    /// <summary>
    /// Reproduce y retorna el AudioSource (para controlar Stop() manualmente).
    /// Útil para loops o sonidos que quieras terminar tú.
    /// Nota: NO se auto-devuelve al pool; debes llamar StopHandle().
    /// </summary>
    public AudioSource PlayGetHandle(string id, Transform follow = null, float vol01 = 1f, float pitch = -999f)
    {
        if (string.IsNullOrEmpty(id) || !dict.ContainsKey(id))
        {
            Debug.LogWarning($"AudioManager: id '{id}' no encontrado.");
            return null;
        }

        var s = dict[id];
        var clip = PickClip(s);
        if (!clip) return null;

        var src = GetSource();
        if (!src) return null;

        // Config
        src.outputAudioMixerGroup = s.outputGroup;
        src.clip = clip;
        src.loop = true; // al usar handle, normalmente quieres loop; si no, cambialo luego.
        src.spatialBlend = s.spatialBlend;
        src.minDistance = s.minDistance;
        src.maxDistance = s.maxDistance;
        src.priority = s.priority;
        src.volume = Mathf.Clamp01(s.volume * vol01);
        src.pitch = (pitch == -999f) ? Random.Range(s.pitchRandom.x, s.pitchRandom.y) : pitch;

        if (follow)
        {
            src.transform.parent = follow;
            src.transform.localPosition = Vector3.zero;
        }
        else
        {
            src.transform.parent = transform;
            src.transform.localPosition = Vector3.zero;
        }

        playingCount[s.id]++;
        activeById[s.id].Add(src);
        src.Play();
        return src;
    }

    /// <summary>
    /// Detiene y libera un handle devuelto por PlayGetHandle.
    /// </summary>
    public void StopHandle(string id, AudioSource handle)
    {
        if (!handle) return;
        if (!string.IsNullOrEmpty(id) && playingCount.ContainsKey(id))
            playingCount[id] = Mathf.Max(0, playingCount[id] - 1);

        ReleaseSource(handle, id);
    }

    /// <summary>
    /// Detiene todos los sonidos activos de un ID.
    /// </summary>
    public void Stop(string id)
    {
        if (string.IsNullOrEmpty(id) || !activeById.ContainsKey(id)) return;

        var list = activeById[id];
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var src = list[i];
            if (!src) { list.RemoveAt(i); continue; }
            src.Stop();
            ReleaseSource(src, id);
        }

        playingCount[id] = 0;
        list.Clear();
    }

    /// <summary>
    /// Cambia de música usando crossfade. Usa un ID del SoundBank.
    /// </summary>
    public void PlayMusic(string id, float targetVolume01 = 1f)
    {
        if (string.IsNullOrEmpty(id) || !dict.ContainsKey(id))
        {
            Debug.LogWarning($"AudioManager: música id '{id}' no encontrado.");
            return;
        }
        var entry = dict[id];
        var clip = PickClip(entry);
        if (!clip) return;

        PlayMusicClip(clip, entry.outputGroup, Mathf.Clamp01(targetVolume01));
    }

    /// <summary>
    /// Cambia de música con un AudioClip directo (por si traes clip externo).
    /// </summary>
    public void PlayMusicClip(AudioClip clip, AudioMixerGroup group = null, float targetVolume01 = 1f)
    {
        if (!clip) return;

        var next = (activeMusic == musicA) ? musicB : musicA;
        next.clip = clip;
        next.volume = 0f;
        next.outputAudioMixerGroup = group;
        next.Play();

        StartCoroutine(CrossfadeMusic(activeMusic, next, Mathf.Clamp01(targetVolume01)));
        activeMusic = next;
    }

    IEnumerator CrossfadeMusic(AudioSource from, AudioSource to, float targetVol01)
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, musicCrossfade);
        float fromStart = from ? from.volume : 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            if (from) from.volume = Mathf.Lerp(fromStart, 0f, k);
            to.volume = Mathf.Lerp(0f, targetVol01, k);
            yield return null;
        }

        if (from) { from.Stop(); from.volume = 0f; }
        to.volume = targetVol01;
    }

    /// <summary>
    /// Ajusta el volumen de música en el AudioMixer usando 0..1 a dB.
    /// Si no usas Mixer, ajusta directamente el volumen del audio activo.
    /// </summary>
    public void SetMusicVolume01(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        if (mixer && !string.IsNullOrEmpty(mixerMusicParam))
        {
            float db = (v01 <= 0.0001f) ? -80f : Mathf.Log10(v01) * 20f;
            mixer.SetFloat(mixerMusicParam, db);
            cachedMusicDb = db;
        }
        else
        {
            if (activeMusic) activeMusic.volume = v01;
        }
    }

    /// <summary>
    /// Recarga el banco y reconstruye diccionarios (por si cambiaste el ScriptableObject en runtime).
    /// </summary>
    public void ReloadBank(SoundBank newBank = null)
    {
        if (newBank) bank = newBank;
        BuildDictionary();
    }
}
