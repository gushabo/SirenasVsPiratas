using UnityEngine;
using UnityEngine.UI;

public class DebugTouch : MonoBehaviour
{
    private static DebugTouch _inst;
    private Text _text;

    // Crea el HUD centrado, super visible, y encima de todo
    private static DebugTouch Ensure()
    {
        if (_inst != null) return _inst;

        var root = new GameObject("~DebugTouchHUD");
        DontDestroyOnLoad(root);
        _inst = root.AddComponent<DebugTouch>();

        // Canvas overlay, por encima de todo
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // <- MUY ARRIBA

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        root.AddComponent<GraphicRaycaster>(); // por si lo necesitas; no raycasteamos

        // Panel de fondo (negro semi)
        var panelGO = new GameObject("Panel", typeof(Image));
        panelGO.transform.SetParent(root.transform, false);
        var img = panelGO.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.65f);
        img.raycastTarget = false; // NO bloquea toques

        var prt = img.rectTransform;
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(900, 280); // tamaño del panel

        // Texto centrado, grande, con sombra
        var tGO = new GameObject("Text", typeof(Text), typeof(Shadow));
        tGO.transform.SetParent(panelGO.transform, false);
        _inst._text = tGO.GetComponent<Text>();
        _inst._text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _inst._text.fontSize = 28;
        _inst._text.alignment = TextAnchor.MiddleCenter;
        _inst._text.color = Color.white;
        _inst._text.raycastTarget = false; // NO bloquea toques

        // Sombra para contraste
        var shadow = tGO.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        var trt = _inst._text.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = Vector2.zero;
        trt.sizeDelta = new Vector2(860, 240);

        _inst._text.text = "DebugTouch HUD listo";
        return _inst;
    }

    /// Reemplaza el contenido (y fuerza la creación si no existe)
    public static void Set(string msg)
    {
        Ensure()._text.text = msg ?? "";
    }

    /// Agrega una línea (con salto)
    public static void Append(string line)
    {
        var inst = Ensure();
        if (!string.IsNullOrEmpty(inst._text.text))
            inst._text.text += "\n";
        inst._text.text += line;
    }
}
