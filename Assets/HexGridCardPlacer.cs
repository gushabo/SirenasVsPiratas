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

    [Header("Tablero / Layer de celdas")]
    [Tooltip("Capa(s) que representan el tablero/celdas jugables. Ej: Road | Water.")]
    public LayerMask boardMask;               // solo tablero
    [Tooltip("Si es true, el hover/ghost usan una altura fija del tablero (independiente de coliders).")]
    public bool useFixedBoardY = true;
    [Tooltip("Altura del tablero si usas Y fija.")]
    public float boardY = 0f;

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

    private bool IsPlaying => GameManager.GetInstance() == null
                              || GameManager.GetInstance().gameState == GameState.Play;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        var gm = GameManager.GetInstance();
        if (gm != null) gm.onChangeGameState += OnGameStateChanged;
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        var gm = GameManager.GetInstance();
        if (gm != null) gm.onChangeGameState -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Play)
        {
            // Apaga hover y ghost inmediatamente
            if (hexOutline) hexOutline.enabled = false;
            hasHover = false;
            ClearPending();
            DestroyGhost();
            // Si también quieres cancelar la carta seleccionada:
            selected.prefab = null;
            selected.isUpgrade = false;
            selected.cardHL = null;
        }
    }

    void Start()
    {
        if (cam == null) cam = Camera.main;

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

            // Más estable visualmente en ángulos oblicuos
            hexOutline.alignment = LineAlignment.View;
            hexOutline.numCornerVertices = 2;
            hexOutline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hexOutline.receiveShadows = false;
        }

        // Mostrar al inicio SOLO si nunca se ha colocado una torre
        bool placedOnce = PlayerPrefs.GetInt(PREF_FIRST_TOWER_PLACED, 0) == 1;
        if (firstPlaceTutorialUI != null) firstPlaceTutorialUI.SetActive(!placedOnce);
    }

    void Update()
    {
        if (GameManager.GetInstance().gameState == GameState.Pause) return;

        UpdateHover();

        // Touch
        foreach (var t in ETouch.activeTouches)
        {
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 sp = t.screenPosition;
                if (!IsPointerOverUI(sp, t.touchId))
                    HandleClick();
                return;
            }
        }

        // Mouse
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 sp = Mouse.current.position.ReadValue();
            if (!IsPointerOverUI(sp))
                HandleClick();
        }
    }

    // === Selección desde la mano ===
    public void SelectBuild(GameObject buildPrefab, CardHighlight fromCard)
    {
        selected.prefab = buildPrefab;
        selected.isUpgrade = false;
        selected.cardHL = fromCard;
        selected.scale = buildPrefab.transform.localScale;
        ClearPending();
        DestroyGhost(); // *** No construimos ghost aún
        // *** Esperamos a tener hover válido para crearlo y posicionarlo
    }

    public void SelectUpgrade(GameObject upgradePrefab, CardHighlight fromCard)
    {
        selected.prefab = upgradePrefab;
        selected.isUpgrade = true;
        selected.cardHL = fromCard;
        selected.scale = upgradePrefab.transform.localScale;
        ClearPending();
        DestroyGhost(); // *** No construimos ghost aún
    }

    public void ClearSelection()
    {
        selected.prefab = null;
        selected.isUpgrade = false;
        selected.cardHL = null;
        ClearPending();
        DestroyGhost();

        if (firstPlaceTutorialUI != null) firstPlaceTutorialUI.SetActive(false);
    }

    // === CLICK (2 pasos) ===
    void HandleClick()
    {
        if (selected.prefab == null || cam == null) return;

        bool gotHit = PointerToBoard(out var hit, out var fallback);
        if (!gotHit && !(useFixedBoardY && fallback != Vector3.zero)) return;

        Vector3 worldPoint = gotHit ? hit.point : fallback;
        int layer = gotHit ? hit.collider.gameObject.layer : 0;

        bool isBomb = IsBombPrefab(selected.prefab);
        bool onRoad = LayerInMask(layer, roadLayerMask);
        bool onWater = LayerInMask(layer, waterLayerMask);
        bool allowed = (isBomb && onRoad) || (!isBomb && onWater);
        if (!allowed)
        {
            SetGhostTint(ghostBlocked);
            return;
        }

        Vector2Int axial = HexGridFlat.WorldToAxial(worldPoint, cellRadius, gridOrigin);
        float yGround = useFixedBoardY ? boardY : worldPoint.y;

        if (!hasPendingCell)
        {
            SetPending(axial, yGround);
            EnsureGhostBuilt();             // *** se crea aquí si no existe
            MoveGhostTo(axial, yGround);    // *** y se posiciona antes de activar
            SetGhostTint(ghostOK);
            return;
        }

        if (axial == pendingAxial)
        {
            TryPlaceAtAxial(axial, yGround);
            return;
        }

        SetPending(axial, yGround);
        EnsureGhostBuilt();
        MoveGhostTo(axial, yGround);
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
            go.transform.localScale = selected.prefab.transform.localScale;
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
        // Outline visible sólo si está activo y hay cámara
        if (!showHover || cam == null)
        {
            if (hexOutline) hexOutline.enabled = false;
            hasHover = false;
            return;
        }

        bool gotHit = PointerToBoard(out var hit, out var fallback);
        if (!gotHit && !(useFixedBoardY && fallback != Vector3.zero))
        {
            if (hexOutline) hexOutline.enabled = false;
            hasHover = false;

            // *** Si no hay punto válido de hover, ocultamos/ destruimos el ghost
            DestroyGhost();
            return;
        }

        Vector3 worldPoint = gotHit ? hit.point : fallback;
        int layerForRules = gotHit ? hit.collider.gameObject.layer : 0;
        float yForDraw = useFixedBoardY ? boardY : worldPoint.y;

        Vector2Int axial = HexGridFlat.WorldToAxial(worldPoint, cellRadius, gridOrigin);
        hoveredAxial = axial;
        hasHover = true;

        // *** Si hay una carta seleccionada, construimos/movemos el ghost durante el hover
        if (selected.prefab != null)
        {
            bool isBomb = IsBombPrefab(selected.prefab);
            bool onRoad = LayerInMask(layerForRules, roadLayerMask);
            bool onWater = LayerInMask(layerForRules, waterLayerMask);
            bool allowed = (isBomb && onRoad) || (!isBomb && onWater);

            EnsureGhostBuilt();                          // *** crea ghost si no existe (aún inactivo)
            MoveGhostTo(axial, yForDraw);                // *** lo posiciona y activa
            SetGhostTint(allowed ? ghostOK : ghostBlocked);
        }
        else
        {
            // *** Sin selección, no mostramos ghost
            DestroyGhost();
        }

        if (hexOutline != null)
        {
            if (hasPendingCell && axial == pendingAxial)
            {
                hexOutline.startColor = pendingColor;
                hexOutline.endColor = pendingColor;
            }
            else
            {
                bool isBomb = IsBombPrefab(selected.prefab);
                bool onRoad = LayerInMask(layerForRules, roadLayerMask);
                bool onWater = LayerInMask(layerForRules, waterLayerMask);
                bool allowed = (isBomb && onRoad) || (!isBomb && onWater);

                hexOutline.startColor = allowed ? hoverOK : hoverBlocked;
                hexOutline.endColor = hexOutline.startColor;
            }

            Vector3 center = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, yForDraw + 0.001f);
            var corners = HexGridFlat.GetHexCorners(center, cellRadius);
            hexOutline.enabled = true;
            for (int i = 0; i < 6; i++) hexOutline.SetPosition(i, corners[i]);
            hexOutline.SetPosition(6, corners[0]);
        }
    }

    // === Ray desde cámara -> tablero (layer) o plano a boardY ===
    bool PointerToBoard(out RaycastHit hit, out Vector3 fallbackPoint)
    {
        hit = default;
        fallbackPoint = Vector3.zero;

        if (cam == null) return false;

        Vector2 sp;
        if (ETouch.activeTouches.Count > 0)
            sp = ETouch.activeTouches[0].screenPosition;
        else if (Mouse.current != null)
            sp = Mouse.current.position.ReadValue();
        else
            return false;

        var ray = cam.ScreenPointToRay(sp);

        // 1) Intentar con colisionadores del tablero
        if (Physics.Raycast(ray, out hit, 5000f, boardMask))
            return true;

        // 2) Fallback: intersección con plano horizontal a boardY
        if (useFixedBoardY)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, boardY, 0f));
            if (plane.Raycast(ray, out float dist))
            {
                fallbackPoint = ray.origin + ray.direction * dist;
                return false; // sin collider, pero tenemos un punto válido
            }
        }

        return false;
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
    void EnsureGhostBuilt()
    {
        if (ghostInstance != null) return;
        if (selected.prefab == null) return;

        ghostInstance = Instantiate(selected.prefab);
        ghostInstance.name = selected.prefab.name + "_GHOST";
        ghostInstance.layer = LayerMask.NameToLayer("Ignore Raycast");

        // *** NACE DESACTIVADO para que no “flashee” en una posición basura
        ghostInstance.SetActive(false);

        // Escala del prefab
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
        float y = useFixedBoardY ? boardY : yGround;
        Vector3 p = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, y + buildYOffset);
        ghostInstance.transform.SetPositionAndRotation(p, Quaternion.identity);
        ghostInstance.transform.localScale = selected.scale;

        // *** Activar solo después de posicionar
        if (!ghostInstance.activeSelf) ghostInstance.SetActive(true);
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
