using UnityEngine;
using UnityEngine.UI;

public class uiManagerMenu : MonoBehaviour
{
    [SerializeField] GameObject menu;
    [SerializeField] GameObject settings;
    [SerializeField] Slider volumeSlider;
    [SerializeField] public Slider sliderSFX;
    

    void Start()
    {
        settings.SetActive(false);
        ActualizarValorSlider();
    }

    public void ActualizarValorSlider()
    {
        // 1) leer valor actual del manager y reflejarlo SIN notificar
        var sm = SoundManager.GetInstance();
        if (volumeSlider) volumeSlider.SetValueWithoutNotify(sm.GetMusicVolume01());
        if (sliderSFX)    sliderSFX.SetValueWithoutNotify(sm.GetSFXVolume01());

        // 2) ahora sí, listeners
        if (volumeSlider) volumeSlider.onValueChanged.AddListener(sm.SetMusicVolume);
        if (sliderSFX)    sliderSFX.onValueChanged.AddListener(sm.SetSFXVolume);
    }
    
    public void SendVolumeToManager()
    {
        SoundManager.GetInstance().SetMusicVolume(volumeSlider.value);
    }

    public void SendSFXToManager()
    {
        SoundManager.GetInstance().SetSFXVolume(sliderSFX.value);
    }
    
}
