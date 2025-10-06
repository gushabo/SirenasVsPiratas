using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

// ALIAS para evitar conflicto con UnityEngine.Touch
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[DefaultExecutionOrder(100)]
public class HexGridCardPlacer : MonoBehaviour
{
    public static HexGridCardPlacer Instance { get; private set; }

    // ===== Tutorial (NUEVO) =====
    [Header("Tutorial (primera vez)")]
    [Tooltip("Panel/Texto con instrucciones. Se mostrará SOLO antes de colocar la primera torreta en todo el juego.")]
    public GameObject firstPlaceTutorialUI;
    [Tooltip("Si está activo, al colocar la primera torreta se iniciará la ronda automáticamente.")]
    public bool startRoundOnFirstPlacement = true;

    private const string PREF_FIRST_TOWER_PLACED = "first_tower_placed"; // 0=no; 1=ya colocado alguna vez

    [Header("Referencias")]
    public Camera cam;
    [Tooltip("Capa del piso/terreno para el raycast")]
    public LayerMask groundMask = ~0;

    [Header("Grid Hex Flat-Top")]
    public float cellRadius = 1.2f;
    public Vector3 gridOrigin = Vector3.zero;
    [Tooltip("Altura para asentar la torreta sobre el piso")]
    public float buildYOffset = 0.02f;

    [Header("Reglas de colocación")]
    [Tooltip("Nombre exacto de la carta Bomba si no usas Tag")]
    public string bombCardId = "Bomba";

    [Tooltip("Celdas de agua (donde colocan las cartas NO-Bomba)")]
    public LayerMask waterLayerMask;

    [Tooltip("Celdas de carretera (donde SOLO puede ir la Bomba)")]
    public LayerMask roadLayerMask;

    [Header("Hover feedback")]
    public Color hoverOK = Color.white;
    public Color hoverBlocked = Color.red;

    [Header("Hover (opcional)")]
    public bool showHover = true;
    public float lineWidth = 0.04f;

    private struct Selected
    {
        public GameObject prefab;
        public bool isUpgrade;
        public CardHighlight cardHL; // para remover la carta si se usa
    }
    private Selected selected;

    // Construcciones por celda axial
    private readonly Dictionary<Vector2Int, GameObject> placedBuilds = new();

    // Hover
    private Vector2Int hoveredAxial;
    private bool hasHover;
    private LineRenderer hexOutline;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Start()
    {
        if (cam == null) cam = Camera.main;

        // ✅ Asegura que el raycast vea tanto Road como Water
        groundMask = groundMask | roadLayerMask | waterLayerMask;

        if (showHover)
        {
            var go = new GameObject("HexHover");
            go.transform.SetParent(transform);
            hexOutline = go.AddComponent<LineRenderer>();
            hexOutline.positionCount = 7; // 6 esquinas + cierre
            hexOutline.loop = false;
            hexOutline.useWorldSpace = true;
            hexOutline.widthMultiplier = lineWidth;
            hexOutline.material = new Material(Shader.Find("Sprites/Default"));
            hexOutline.enabled = false;
        }

        // === Mostrar tutorial si nunca se ha colocado una torreta (persistente) ===
        if (PlayerPrefs.GetInt(PREF_FIRST_TOWER_PLACED, 0) == 0)
        {
            if (firstPlaceTutorialUI != null) firstPlaceTutorialUI.SetActive(true);
        }
        else
        {
            if (firstPlaceTutorialUI != null) firstPlaceTutorialUI.SetActive(false);
        }
    }

    void Update()
    {
        UpdateHover();

        // Toque (móvil)
        var touches = ETouch.activeTouches;
        foreach (var t in touches)
        {
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 sp = t.screenPosition;
                if (!IsPointerOverUI(sp, t.touchId))
                    TryPlaceAtScreenPos(sp);
                return;
            }
        }

        // Mouse (Editor)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 sp = Mouse.current.position.ReadValue();
            if (!IsPointerOverUI(sp))
                TryPlaceAtScreenPos(sp);
        }
    }

    // --- Selección desde la mano ---
    public void SelectBuild(GameObject buildPrefab, CardHighlight fromCard)
    {
        selected.prefab = buildPrefab;
        selected.isUpgrade = false;
        selected.cardHL = fromCard;
    }

    public void SelectUpgrade(GameObject upgradePrefab, CardHighlight fromCard)
    {
        selected.prefab = upgradePrefab;
        selected.isUpgrade = true;
        selected.cardHL = fromCard;
    }

    public void ClearSelection()
    {
        selected.prefab = null;
        selected.isUpgrade = false;
        selected.cardHL = null;
    }

    // --- Lógica de colocación ---
    void TryPlaceAtScreenPos(Vector2 screenPos)
    {
        if (selected.prefab == null || cam == null) return;

        if (!Physics.Raycast(cam.ScreenPointToRay(screenPos), out var hit, 2000f, groundMask))
            return;

        // === REGLA DE COLOCACIÓN POR LAYER ===
        int hitLayer = hit.collider.gameObject.layer;
        string hitLayerName = LayerMask.LayerToName(hitLayer);

        bool isBomb = IsBombPrefab(selected.prefab);

        bool onRoad = LayerInMask(hitLayer, roadLayerMask);
        bool onWater = LayerInMask(hitLayer, waterLayerMask);

        // 🔎 Logs para depurar
        Debug.Log($"[Place] Hit:{hit.collider.name} | Layer:{hitLayerName}({hitLayer}) | isBomb:{isBomb} | onRoad:{onRoad} | onWater:{onWater}");

        // Solo Bomba en Road; el resto solo en Water
        if (isBomb && !onRoad)
        {
            Debug.LogWarning("[Grid] Bomba solo puede colocarse en capas Road.");
            return;
        }
        if (!isBomb && !onWater)
        {
            Debug.LogWarning("[Grid] Esta carta solo puede colocarse en capas Water.");
            return;
        }
        // === FIN DE REGLA ===

        Vector3 point = hit.point;
        Vector2Int axial = HexGridFlat.WorldToAxial(point, cellRadius, gridOrigin);

        if (selected.isUpgrade)
        {
            if (placedBuilds.TryGetValue(axial, out GameObject towerGo) && towerGo != null)
            {
                var upg = towerGo.GetComponent<TowerUpgradable>();
                if (upg != null)
                {
                    upg.ApplyUpgrade(selected.prefab);
                    AfterSuccessfulUse();
                }
                else
                {
                    Debug.Log($"[{name}] La construcción en {axial} no tiene TowerUpgradable.");
                }
            }
            else
            {
                Debug.Log($"[{name}] No hay construcción en {axial} para aplicar mejora.");
            }
        }
        else
        {
            if (placedBuilds.ContainsKey(axial) && placedBuilds[axial] != null)
            {
                Debug.Log($"[{name}] Celda {axial} ocupada. No se puede construir encima.");
                return;
            }

            Vector3 spawnPos = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, hit.point.y + buildYOffset);
            Quaternion rot = Quaternion.identity;

            var go = Instantiate(selected.prefab, spawnPos, rot);
            placedBuilds[axial] = go;

            // === NUEVO: primera vez colocada → ocultar tutorial, persistir y (opcional) iniciar ronda
            if (PlayerPrefs.GetInt(PREF_FIRST_TOWER_PLACED, 0) == 0)
            {
                PlayerPrefs.SetInt(PREF_FIRST_TOWER_PLACED, 1);
                PlayerPrefs.Save();

                if (firstPlaceTutorialUI != null) firstPlaceTutorialUI.SetActive(false);

                if (startRoundOnFirstPlacement)
                {
                    var lm = LevelManager.GetInstance();
                    if (lm != null) lm.StartRound();
                    else Debug.LogWarning("[HexGridCardPlacer] No encontré LevelManager para iniciar la ronda.");
                }
            }

            AfterSuccessfulUse();
        }
    }

    void AfterSuccessfulUse()
    {
        // Quita la carta usada de la mano (si vino de una)
        if (selected.cardHL != null && HandManager.Instance != null)
        {
            HandManager.Instance.RemoveCardByHighlight(selected.cardHL);
            HandManager.Instance.NotifyPlacementCleared();
        }
        ClearSelection();
    }

    // --- Hover visual ---
    void UpdateHover()
    {
        if (!showHover || cam == null) return;

        Vector2 screenPos = Vector2.zero;
        bool hasPointer = false;

        // Usa el primer toque o el mouse
        if (ETouch.activeTouches.Count > 0)
        {
            screenPos = ETouch.activeTouches[0].screenPosition;
            hasPointer = true;
        }
        else if (Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
            hasPointer = true;
        }

        if (!hasPointer)
        {
            if (hexOutline) hexOutline.enabled = false;
            return;
        }

        // Raycast al terreno
        if (!Physics.Raycast(cam.ScreenPointToRay(screenPos), out var hit, 2000f, groundMask))
        {
            if (hexOutline) hexOutline.enabled = false;
            hasHover = false;
            return;
        }

        Vector2Int axial = HexGridFlat.WorldToAxial(hit.point, cellRadius, gridOrigin);
        hoveredAxial = axial;
        hasHover = true;

        if (hexOutline != null)
        {
            // === color según regla ===
            int hitLayer = hit.collider.gameObject.layer;

            bool isBomb = IsBombPrefab(selected.prefab);
            bool onRoad = LayerInMask(hitLayer, roadLayerMask);
            bool onWater = LayerInMask(hitLayer, waterLayerMask);
            bool allowed = (isBomb && onRoad) || (!isBomb && onWater);

            hexOutline.startColor = allowed ? hoverOK : hoverBlocked;
            hexOutline.endColor = hexOutline.startColor;

            // === dibujar hexágono ===
            Vector3 center = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, hit.point.y + 0.001f);
            var corners = HexGridFlat.GetHexCorners(center, cellRadius);
            hexOutline.enabled = true;
            for (int i = 0; i < 6; i++)
                hexOutline.SetPosition(i, corners[i]);
            hexOutline.SetPosition(6, corners[0]); // cerrar
        }
    }

    // --- Utilidades ---
    bool IsPointerOverUI(Vector2 screenPos, int pointerId = -1)
    {
        if (EventSystem.current == null) return false;
        var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
        if (pointerId >= 0) eventData.pointerId = pointerId;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    static bool LayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // ✅ Detección robusta de la carta bomba
    bool IsBombPrefab(GameObject p)
    {
        if (p == null) return false;

        // Opción 1: por Tag (recomendado)
        if (p.CompareTag("Bomb")) return true;

        // Opción 2: por nombre (tolerante a "(Clone)")
        string n = p.name;
        if (n.EndsWith("(Clone)")) n = n.Substring(0, n.Length - "(Clone)".Length);
        return n == bombCardId || n.Contains(bombCardId);
    }
}
