using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoundBank", menuName = "Scriptable Objects/SoundBank")]
public class SoundBank : ScriptableObject
{
    [System.Serializable]
    public class SoundEntry
    {
        [Tooltip("ID único. Ej: ui_click, gun_shot, hook_throw, music_menu")]
        public string id;

        [Tooltip("Una o varias variantes. El manager elige aleatoriamente.")]
        public List<AudioClip> clips = new List<AudioClip>();

        [Header("Routing")]
        public AudioMixerGroup outputGroup;

        [Header("Comportamiento")]
        public bool loop = false;
        [Range(0f, 1f)] public float spatialBlend = 0f; // 0=2D, 1=3D
        public float minDistance = 1f;
        public float maxDistance = 25f;

        [Header("Volumen/Pitch")]
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRandom = new Vector2(1f, 1f); // (min, max)

        [Header("Límites")]
        public int maxSimultaneous = 8;       // evita spam
        public float cooldown = 0f;           // tiempo mínimo entre reproducciones del MISMO id

        [Header("Prioridad (menor = más importante)")]
        [Range(0, 256)] public int priority = 128;

        [Header("Opcional: Ducking Música")]
        [Range(0f, 1f)] public float musicDuckAmount = 0f;  // cuánto bajar la música
        public float musicDuckSeconds = 0.25f;               // tiempo de entrada/salida ducking
    }

    public List<SoundEntry> sounds = new List<SoundEntry>();
}
