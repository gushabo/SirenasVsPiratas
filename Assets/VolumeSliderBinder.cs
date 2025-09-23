using UnityEngine;
using UnityEngine.UI;
public class VolumeSliderBinder : MonoBehaviour
{
    void Awake()
    {
        var sm = SoundManager.GetInstance();
        var slider = GetComponent<Slider>();
        if (sm != null && slider != null) sm.BindSlider(slider);
    }
}
