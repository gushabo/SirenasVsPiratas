using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[DefaultExecutionOrder(100)]
public class HexGridCardPlacer : MonoBehaviour
{
    public static HexGridCardPlacer Instance { get; private set; }

    // ===== Tutorial (primera vez) =====
    [Header("Tutorial (primera vez)")]
    public GameObject firstPlaceTutorialUI;
    public bool startRoundOnFirstPlacement = true;
    private const string PREF_FIRST_TOWER_PLACED = "first_tower_placed";

    [Header("Referencias")]
    public Camera cam;
    public LayerMask groundMask = ~0;

    [Header("Grid Hex Flat-Top")]
    public float cellRadius = 1.2f;
    public Vector3 gridOrigin = Vector3.zero;
    public float buildYOffset = 0.02f;

    [Header("Reglas de colocación")]
    public string bombCardId = "Bomba";
    public LayerMask waterLayerMask;
    public LayerMask roadLayerMask;

    [Header("Hover/Preview")]
    public bool showHover = true;
    public float lineWidth = 0.04f;
    public Color hoverOK = Color.white;
    public Color hoverBlocked = new Color(1f, 0.2f, 0.2f, 1f);
    public Color pendingColor = new Color(0.2f, 0.8f, 1f, 1f);

    [Header("Ghost Preview 3D")]
    [Tooltip("Material semitransparente para el ghost (debe tener color).")]
    public Material ghostMaterial;
    public Color ghostOK = new Color(0.2f, 1f, 0.6f, 0.5f);
    public Color ghostBlocked = new Color(1f, 0.3f, 0.3f, 0.5f);

    private struct Selected
    {
        public GameObject prefab;
        public bool isUpgrade;
        public CardHighlight cardHL;
        public Vector3 scale; // escala del prefab (p.ej. 13,13,13)
    }

    private Selected selected;

    private readonly Dictionary<Vector2Int, GameObject> placedBuilds = new();

    // Hover runtime
    private Vector2Int hoveredAxial;
    private bool hasHover;
    private LineRenderer hexOutline;

    // Flujo 2 clics + ghost
    private bool hasPendingCell;
    private Vector2Int pendingAxial;
    private float pendingY;

    private GameObject ghostInstance;
    private readonly List<Renderer> ghostRenderers = new();

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

        groundMask = groundMask | roadLayerMask | waterLayerMask;

        if (showHover)
        {
            var go = new GameObject("HexHover");
            go.transform.SetParent(transform);
            hexOutline = go.AddComponent<LineRenderer>();
            hexOutline.positionCount = 7;
            hexOutline.loop = false;
            hexOutline.useWorldSpace = true;
            hexOutline.widthMultiplier = lineWidth;
            hexOutline.material = new Material(Shader.Find("Sprites/Default"));
            hexOutline.enabled = false;
        }

        // Mostrar al inicio SOLO si nunca se ha colocado una torre
        bool placedOnce = PlayerPrefs.GetInt(PREF_FIRST_TOWER_PLACED, 0) == 1;
        if (firstPlaceTutorialUI != null) firstPlaceTutorialUI.SetActive(!placedOnce);
    }

    void Update()
    {
        UpdateHover();

        // Touch
        foreach (var t in ETouch.activeTouches)
        {
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 sp = t.screenPosition;
                if (!IsPointerOverUI(sp, t.touchId))
                    HandleClick(sp);
                return;
            }
        }

        // Mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 sp = Mouse.current.position.ReadValue();
            if (!IsPointerOverUI(sp))
                HandleClick(sp);
        }
    }

    // === Selección desde la mano ===
    public void SelectBuild(GameObject buildPrefab, CardHighlight fromCard)
    {
        selected.prefab = buildPrefab;
        selected.isUpgrade = false;
        selected.cardHL = fromCard;
        selected.scale = buildPrefab.transform.localScale; // usa escala del prefab
        ClearPending();
        RebuildGhostIfNeeded();
    }

    public void SelectUpgrade(GameObject upgradePrefab, CardHighlight fromCard)
    {
        selected.prefab = upgradePrefab;
        selected.isUpgrade = true;
        selected.cardHL = fromCard;
        selected.scale = upgradePrefab.transform.localScale;
        ClearPending();
        RebuildGhostIfNeeded();
    }

    public void ClearSelection()
    {
        selected.prefab = null;
        selected.isUpgrade = false;
        selected.cardHL = null;
        ClearPending();
        DestroyGhost();

        // Apaga tutorial si el jugador cancela
        if (firstPlaceTutorialUI != null) firstPlaceTutorialUI.SetActive(false);
    }

    // === CLICK (2 pasos) ===
    void HandleClick(Vector2 screenPos)
    {
        if (selected.prefab == null || cam == null) return;
        if (!Physics.Raycast(cam.ScreenPointToRay(screenPos), out var hit, 2000f, groundMask)) return;

        int layer = hit.collider.gameObject.layer;
        bool isBomb = IsBombPrefab(selected.prefab);
        bool onRoad = LayerInMask(layer, roadLayerMask);
        bool onWater = LayerInMask(layer, waterLayerMask);
        bool allowed = (isBomb && onRoad) || (!isBomb && onWater);
        if (!allowed)
        {
            SetGhostTint(ghostBlocked);
            return;
        }

        Vector2Int axial = HexGridFlat.WorldToAxial(hit.point, cellRadius, gridOrigin);

        if (!hasPendingCell)
        {
            SetPending(axial, hit.point.y);
            EnsureGhostBuilt();
            MoveGhostTo(axial, hit.point.y);
            SetGhostTint(ghostOK);
            return;
        }

        if (axial == pendingAxial)
        {
            TryPlaceAtAxial(axial, pendingY);
            return;
        }

        // Mover selección (no coloca)
        SetPending(axial, hit.point.y);
        EnsureGhostBuilt();
        MoveGhostTo(axial, hit.point.y);
        SetGhostTint(ghostOK);
    }

    // === Colocar en celda validada ===
    void TryPlaceAtAxial(Vector2Int axial, float yGround)
    {
        if (selected.prefab == null) return;

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

            Vector3 spawnPos = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, yGround + buildYOffset);

            var go = Instantiate(selected.prefab, spawnPos, selected.prefab.transform.rotation);
            go.transform.localScale = selected.prefab.transform.localScale; // respeta escala del prefab
            placedBuilds[axial] = go;

            if (PlayerPrefs.GetInt(PREF_FIRST_TOWER_PLACED, 0) == 0)
            {
                PlayerPrefs.SetInt(PREF_FIRST_TOWER_PLACED, 1);
                PlayerPrefs.Save();
                if (firstPlaceTutorialUI) firstPlaceTutorialUI.SetActive(false);

                if (startRoundOnFirstPlacement)
                {
                    var lm = LevelManager.GetInstance();
                    if (lm != null) lm.StartRound();
                    else Debug.LogWarning("[HexGridCardPlacer] No encontré LevelManager para iniciar la ronda.");
                }
            }

            AfterSuccessfulUse();
        }

        ClearPending();
        DestroyGhost();
    }

    void AfterSuccessfulUse()
    {
        if (selected.cardHL != null && HandManager.Instance != null)
        {
            HandManager.Instance.RemoveCardByHighlight(selected.cardHL);
            HandManager.Instance.NotifyPlacementCleared();
        }
        ClearSelection();
    }

    // === Hover / Outline ===
    void UpdateHover()
    {
        if (!showHover || cam == null) return;

        Vector2 screenPos = Vector2.zero;
        bool hasPointer = false;

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
            hasHover = false;
            return;
        }

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
            if (hasPendingCell && axial == pendingAxial)
            {
                hexOutline.startColor = pendingColor;
                hexOutline.endColor = pendingColor;
            }
            else
            {
                int layer = hit.collider.gameObject.layer;
                bool isBomb = IsBombPrefab(selected.prefab);
                bool onRoad = LayerInMask(layer, roadLayerMask);
                bool onWater = LayerInMask(layer, waterLayerMask);
                bool allowed = (isBomb && onRoad) || (!isBomb && onWater);

                hexOutline.startColor = allowed ? hoverOK : hoverBlocked;
                hexOutline.endColor = hexOutline.startColor;
            }

            Vector3 center = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, hit.point.y + 0.001f);
            var corners = HexGridFlat.GetHexCorners(center, cellRadius);
            hexOutline.enabled = true;
            for (int i = 0; i < 6; i++) hexOutline.SetPosition(i, corners[i]);
            hexOutline.SetPosition(6, corners[0]);
        }
    }

    // === Pending helpers ===
    void SetPending(Vector2Int axial, float yGround)
    {
        pendingAxial = axial;
        pendingY = yGround;
        hasPendingCell = true;

        // Oculta tutorial al primer clic de fijado
        if (firstPlaceTutorialUI != null) firstPlaceTutorialUI.SetActive(false);
    }

    void ClearPending()
    {
        hasPendingCell = false;
    }

    // === Ghost helpers ===
    void RebuildGhostIfNeeded()
    {
        DestroyGhost();
        if (selected.prefab != null) EnsureGhostBuilt();
    }

    void EnsureGhostBuilt()
    {
        if (ghostInstance != null) return;
        if (selected.prefab == null) return;

        ghostInstance = Instantiate(selected.prefab);
        ghostInstance.name = selected.prefab.name + "_GHOST";
        ghostInstance.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Escala del prefab (p.ej. 13)
        ghostInstance.transform.localScale = selected.scale;

        // Desactivar lógica/sensores del prefab
        foreach (var b in ghostInstance.GetComponentsInChildren<Behaviour>(true))
        {
            if (b is Renderer) continue;
            b.enabled = false;
        }
        foreach (var col in ghostInstance.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        var rb = ghostInstance.GetComponentInChildren<Rigidbody>(true);
        if (rb) { rb.isKinematic = true; rb.detectCollisions = false; }

        // Material fantasma
        ghostRenderers.Clear();
        var rends = ghostInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            ghostRenderers.Add(r);
            if (ghostMaterial != null)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
                r.sharedMaterials = mats;
            }
        }

        SetGhostTint(ghostOK);
    }

    void DestroyGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }
        ghostRenderers.Clear();
    }

    void MoveGhostTo(Vector2Int axial, float yGround)
    {
        if (ghostInstance == null) return;
        Vector3 p = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, yGround + buildYOffset);
        ghostInstance.transform.SetPositionAndRotation(p, Quaternion.identity);
        ghostInstance.transform.localScale = selected.scale; // mantener escala
    }

    void SetGhostTint(Color c)
    {
        if (ghostRenderers.Count == 0) return;
        foreach (var r in ghostRenderers)
        {
            foreach (var m in r.materials)
            {
                if (m.HasProperty("_Color")) m.color = c;
            }
        }
    }

    // === Utilidades ===
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
        => (mask.value & (1 << layer)) != 0;

    bool IsBombPrefab(GameObject p)
    {
        if (p == null) return false;
        if (p.CompareTag("Bomb")) return true;
        string n = p.name;
        if (n.EndsWith("(Clone)")) n = n.Substring(0, n.Length - "(Clone)".Length);
        return n == bombCardId || n.Contains(bombCardId);
    }
}
