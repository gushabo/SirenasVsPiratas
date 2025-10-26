using UnityEngine;
using UnityEngine.InputSystem;
// Aliases New Input
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ETouchSupport = UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport;
using TouchSim = UnityEngine.InputSystem.EnhancedTouch.TouchSimulation;
using ETouchPhase = UnityEngine.InputSystem.TouchPhase;
using Pointer = UnityEngine.InputSystem.Pointer;
using MouseDev = UnityEngine.InputSystem.Mouse;

public class TouchSniffer : MonoBehaviour
{
    public static TouchSniffer I;
    public string report = "(sin datos)";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        // habilitamos EnhancedTouch + simulación desde mouse
        ETouchSupport.Enable();
        TouchSim.Enable();
    }

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== TOUCH SNIFFER ===");

        // NEW INPUT — EnhancedTouch
        if (ETouch.activeTouches.Count > 0)
        {
            var t = ETouch.activeTouches[0];
            sb.AppendLine($"NEW/Enhanced: count={ETouch.activeTouches.Count} pos={t.screenPosition} phase={t.phase}");
        }
        else
        {
            sb.AppendLine("NEW/Enhanced: count=0");
        }

        // NEW INPUT — Pointer/Mouse
        if (Pointer.current != null)
        {
            var pos = Pointer.current.position.ReadValue();
            bool pressed = MouseDev.current != null && MouseDev.current.leftButton.isPressed;
            sb.AppendLine($"NEW/Pointer : pos={pos} mousePressed={pressed}");
        }
        else
        {
            sb.AppendLine("NEW/Pointer : null");
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        // LEGACY — Touch
        sb.AppendLine($"LEGACY/Touch: count={Input.touchCount}");
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            sb.AppendLine($"  pos={t.position} phase={t.phase}");
        }
        // LEGACY — Mouse
        sb.AppendLine($"LEGACY/Mouse: pos={Input.mousePosition} pressed={Input.GetMouseButton(0)}");
#else
        sb.AppendLine("LEGACY: deshabilitado (no Both)");
#endif

        // Info extra
        sb.AppendLine($"Time.timeScale={Time.timeScale}");
        sb.AppendLine($"Application.platform={Application.platform}");

        report = sb.ToString();
    }
}
