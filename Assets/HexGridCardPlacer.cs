using System;
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
    [Tooltip("Este flag ya no arranca la ronda; la ronda inicia al terminar el tutorial.")]
    public bool startRoundOnFirstPlacement = false; // <- mantenlo en false
    private const string PREF_FIRST_TOWER_PLACED = "first_tower_placed";

    [Header("Referencias")]
    public Camera cam;

    [Header("SOUNDSSS")] 
    private AudioSource spawnSirena;

    [Header("Tablero / Layer de celdas")]
    public LayerMask boardMask;
    public bool useFixedBoardY = true;
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
    public Material ghostMaterial;
    public Color ghostOK = new Color(0.2f, 1f, 0.6f, 0.5f);
    public Color ghostBlocked = new Color(1f, 0.3f, 0.3f, 0.5f);

    private struct Selected
    {
        public GameObject prefab;
        public bool isUpgrade;
        public CardHighlight cardHL;
        public Vector3 scale;
        public AudioClip sfx;
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
            if (hexOutline) hexOutline.enabled = false;
            hasHover = false;
            ClearPending();
            DestroyGhost();

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

            hexOutline.alignment = LineAlignment.View;
            hexOutline.numCornerVertices = 2;
            hexOutline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hexOutline.receiveShadows = false;
        }

        // Si el tutorial ya fue completado en sesiones anteriores, puedes ocultar cualquier UI de "primer paso"
        bool placedOnce = PlayerPrefs.GetInt(PREF_FIRST_TOWER_PLACED, 0) == 1;
        if (firstPlaceTutorialUI != null)
            firstPlaceTutorialUI.SetActive(!TutorialProgress.IsCompleted() && !placedOnce);
    }

    void Update()
    {
        if (!IsPlaying) return;

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
        DestroyGhost();
    }

    public void SelectUpgrade(GameObject upgradePrefab, CardHighlight fromCard, AudioClip sfx)
    {
        selected.prefab = upgradePrefab;
        selected.isUpgrade = true;
        selected.cardHL = fromCard;
        selected.scale = upgradePrefab.transform.localScale;
        selected.sfx = sfx;
        ClearPending();
        DestroyGhost();
    }

    public void ClearSelection()
    {
        selected.prefab = null;
        selected.isUpgrade = false;
        selected.cardHL = null;
        selected.sfx = null; 
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
            EnsureGhostBuilt();
            MoveGhostTo(axial, yGround);
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
                    
                    if (selected.sfx != null)
                    {
                        SoundManager.GetInstance().PlaySFX(selected.sfx);
                    }
                    
                    
                    AfterSuccessfulUse();

                    // Progreso tutorial (mejora)
                    TutorialProgress.Increment(BuildKind.Upgrade);
                }
                
            }
           
        }
        else
        {
            if (placedBuilds.ContainsKey(axial) && placedBuilds[axial] != null)
            {
                
                return;
            }

            Vector3 spawnPos = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, yGround + buildYOffset);

            var go = Instantiate(selected.prefab, spawnPos, selected.prefab.transform.rotation);
            go.transform.localScale = selected.prefab.transform.localScale;
            placedBuilds[axial] = go;

            // Progreso tutorial (torre o mina)
            BuildKind kindToReport = IsBombPrefab(selected.prefab) ? BuildKind.Mine : BuildKind.Tower;
            TutorialProgress.Increment(kindToReport);

            // Guardar "primera torre" si lo usas para UI, PERO YA NO INICIA LA RONDA
            if (PlayerPrefs.GetInt(PREF_FIRST_TOWER_PLACED, 0) == 0)
            {
                PlayerPrefs.SetInt(PREF_FIRST_TOWER_PLACED, 1);
                PlayerPrefs.Save();
                if (firstPlaceTutorialUI) firstPlaceTutorialUI.SetActive(false);
            }
            SoundManager.GetInstance().PlaySFX(spawnSirena);
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
            DestroyGhost();
            return;
        }

        Vector3 worldPoint = gotHit ? hit.point : fallback;
        int layerForRules = gotHit ? hit.collider.gameObject.layer : 0;
        float yForDraw = useFixedBoardY ? boardY : worldPoint.y;

        Vector2Int axial = HexGridFlat.WorldToAxial(worldPoint, cellRadius, gridOrigin);
        hoveredAxial = axial;
        hasHover = true;

        if (selected.prefab != null)
        {
            bool isBomb = IsBombPrefab(selected.prefab);
            bool onRoad = LayerInMask(layerForRules, roadLayerMask);
            bool onWater = LayerInMask(layerForRules, waterLayerMask);
            bool allowed = (isBomb && onRoad) || (!isBomb && onWater);

            EnsureGhostBuilt();
            MoveGhostTo(axial, yForDraw);
            SetGhostTint(allowed ? ghostOK : ghostBlocked);
        }
        else
        {
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

        if (Physics.Raycast(ray, out hit, 5000f, boardMask))
            return true;

        if (useFixedBoardY)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, boardY, 0f));
            if (plane.Raycast(ray, out float dist))
            {
                fallbackPoint = ray.origin + ray.direction * dist;
                return false;
            }
        }

        return false;
    }

    void SetPending(Vector2Int axial, float yGround)
    {
        pendingAxial = axial;
        pendingY = yGround;
        hasPendingCell = true;

        if (firstPlaceTutorialUI != null) firstPlaceTutorialUI.SetActive(false);
    }

    void ClearPending() => hasPendingCell = false;

    void EnsureGhostBuilt()
    {
        if (ghostInstance != null || selected.prefab == null) return;

        ghostInstance = Instantiate(selected.prefab);
        ghostInstance.name = selected.prefab.name + "_GHOST";
        ghostInstance.layer = LayerMask.NameToLayer("Ignore Raycast");
        ghostInstance.SetActive(false);
        ghostInstance.transform.localScale = selected.scale;

        foreach (var b in ghostInstance.GetComponentsInChildren<Behaviour>(true))
        {
            if (b is Renderer) continue;
            b.enabled = false;
        }
        foreach (var col in ghostInstance.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        var rb = ghostInstance.GetComponentInChildren<Rigidbody>(true);
        if (rb) { rb.isKinematic = true; rb.detectCollisions = false; }

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

        if (!ghostInstance.activeSelf) ghostInstance.SetActive(true);
    }

    void SetGhostTint(Color c)
    {
        if (ghostRenderers.Count == 0) return;
        foreach (var r in ghostRenderers)
            foreach (var m in r.materials)
                if (m.HasProperty("_Color")) m.color = c;
    }

    bool IsPointerOverUI(Vector2 screenPos, int pointerId = -1)
    {
        if (EventSystem.current == null) return false;
        var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
        if (pointerId >= 0) eventData.pointerId = pointerId;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    static bool LayerInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    bool IsBombPrefab(GameObject p)
    {
        if (p == null) return false;
        if (p.CompareTag("Bomb")) return true;
        string n = p.name;
        if (n.EndsWith("(Clone)")) n = n[..^7];
        return n == bombCardId || n.Contains(bombCardId);
    }
}
