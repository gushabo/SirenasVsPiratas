using UnityEngine;
using UnityEngine.UI;

public class uiManagerMenu : MonoBehaviour
{
    [SerializeField] GameObject menu;
    [SerializeField] GameObject settings;
    [SerializeField] Slider volumeSlider;
    

    void Start()
    {
        settings.SetActive(false);
    }

    public void ActualizarValorSlider()
    {
        volumeSlider.value = SoundManager.GetInstance().audioSource.volume;
    }
    
    public void SendVolumeToManager()
    {
        SoundManager.GetInstance().ChangeAudioValue(volumeSlider.value);
    }




}
