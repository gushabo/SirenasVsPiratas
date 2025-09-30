using UnityEngine;

public class uiManagerMenu : MonoBehaviour
{
    [SerializeField] GameObject menu;
    [SerializeField] GameObject settings;

    void Start()
    {
        settings.SetActive(false);
    }
    
}
