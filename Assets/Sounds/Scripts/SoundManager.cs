using System;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [SerializeField] public Scrollbar scrollBar;
    private AudioListener listener;
    private AudioSource audioSource;
    private void Awake() => DontDestroyOnLoad(this);

    public void Start()
    {
        listener = GetComponent<AudioListener>();
        audioSource = GetComponent<AudioSource>();
        scrollBar.value = .5f;
    }

    public void ChangeAudioValue(float value)
    {
        audioSource.volume = value;
    }
    
    
}
