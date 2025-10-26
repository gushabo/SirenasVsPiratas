using UnityEngine;
using UnityEngine.InputSystem; // Touchscreen, Pointer, Mouse

public class TapRaycastTest : MonoBehaviour
{
    [Header("Asignaciones")]
    public Camera cam;                // Asigna tu cámara de juego
    public LayerMask mask = ~0;       // Everything por defecto

    [Header("Debug")]
    public float rayMaxDistance = 500f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam) cam = FindFirstObjectByType<Camera>();
        if (!cam) Debug.LogError("[TapRaycastTest] No hay cámara asignada ni Camera.main en escena.");
        // Asegura que TimeScale no esté pausado por error
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }

    void Update()
    {
        // 1) ¿Hubo tap/click este frame?
        bool pressed = false;
        Vector2 pos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // Touch (móvil)
        var ts = Touchscreen.current;
        if (ts != null)
        {
            var t = ts.primaryTouch;
            pressed = t.press.wasPressedThisFrame;
            pos = t.position.ReadValue();
        }
        // Mouse (editor/PC)
        else if (Pointer.current != null)
        {
            pressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            pos = Pointer.current.position.ReadValue();
        }

        if (!pressed || cam == null) return;

        // 2) Raycast desde pantalla al mundo
        Ray ray = cam.ScreenPointToRay(pos);
        int layermaskInt = mask.value; // evitar ambigüedad LayerMask/int
        if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, layermaskInt, QueryTriggerInteraction.Collide))
        {
            // 3) Coloca un cubito en el punto de impacto
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = hit.point + Vector3.up * 0.25f;
            cube.transform.localScale = Vector3.one * 0.3f;
            // opcional: destruir tras unos segundos
            Destroy(cube, 5f);
        }
    }
}
