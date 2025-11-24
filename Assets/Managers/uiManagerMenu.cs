using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class uiManagerMenu : MonoBehaviour
{
    [SerializeField] GameObject menu;
    [SerializeField] GameObject settings;
    [SerializeField] Slider volumeSlider;   // Música
    [SerializeField] Slider sliderSFX;      // Efectos

    void Start()
    {
        if (settings) settings.SetActive(false);
    }

    // Solo refresca VISUAL sin disparar eventos ni agregar listeners
    public void ActualizarValorSlider()
    {
        var sm = SoundManager.GetInstance();
        if (!sm) return;

        if (sm.TryGetMixerDb(out var mDb, out var sDb))
        {
            if (volumeSlider) volumeSlider.SetValueWithoutNotify(SoundManager.DbToLinear(mDb));
            if (sliderSFX)    sliderSFX.SetValueWithoutNotify(SoundManager.DbToLinear(sDb));
        }
        else
        {
            if (volumeSlider) volumeSlider.SetValueWithoutNotify(sm.GetMusicVolume01());
            if (sliderSFX)    sliderSFX.SetValueWithoutNotify(sm.GetSFXVolume01());
        }
    }

    void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    IEnumerator BindWhenReady()
    {
        while (SoundManager.GetInstance() == null) yield return null;

        var sm = SoundManager.GetInstance();

        // 1) Sincroniza sin notificar
        ActualizarValorSlider();

        // 2) UI -> Manager
        if (volumeSlider) volumeSlider.onValueChanged.AddListener(sm.SetMusicVolume);
        if (sliderSFX)    sliderSFX.onValueChanged.AddListener(sm.SetSFXVolume);

        // 3) Manager -> UI
        sm.OnVolumesChanged += HandleVolumesChanged;
    }

    void OnDisable()
    {
        var sm = SoundManager.GetInstance();
        if (sm != null) sm.OnVolumesChanged -= HandleVolumesChanged;

        if (volumeSlider) volumeSlider.onValueChanged.RemoveAllListeners();
        if (sliderSFX)    sliderSFX.onValueChanged.RemoveAllListeners();
    }

    void HandleVolumesChanged(float music01, float sfx01)
    {
        if (volumeSlider) volumeSlider.SetValueWithoutNotify(music01);
        if (sliderSFX)    sliderSFX.SetValueWithoutNotify(sfx01);
    }

    // Si estás usando los eventos del Slider en el Inspector, estos siguen funcionando:
    public void SendVolumeToManager() { var sm = SoundManager.GetInstance(); if (sm && volumeSlider) sm.SetMusicVolume(volumeSlider.value); }
    public void SendSFXToManager()    { var sm = SoundManager.GetInstance(); if (sm && sliderSFX)    sm.SetSFXVolume(sliderSFX.value);    }
}
