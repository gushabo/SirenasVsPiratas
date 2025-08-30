using TMPro;
using UnityEngine;

public class HealthText : MonoBehaviour
{
    private TextMeshProUGUI text;
    
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }
    
    public void ChangeHealthText(int health)
    {
        text.text = "Health: " + health;
    }
}
