using UnityEngine;

public class TouchOverlay : MonoBehaviour
{
    private GUIStyle style;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        style = new GUIStyle(GUI.skin.label);
        style.fontSize = 28;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        var r = new Rect(Screen.width / 2f - 480, Screen.height / 2f - 160, 960, 320);
        // fondo oscuro
        Color prev = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(r, GUIContent.none);
        GUI.color = Color.white;

        // texto
        var txt = TouchSniffer.I != null ? TouchSniffer.I.report : "(TouchSniffer no presente)";
        GUI.Label(r, txt, style);

        GUI.color = prev;
    }
}
