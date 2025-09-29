using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardPlacer : MonoBehaviour
{
    public static CardPlacer Instance;

    private CardHighlight currentUIHighlight;

    [Header("Camera")]
    public Camera cam;

    [Header("Grid")]
    [Tooltip("Radio del hex (distancia del centro a cada vértice)")]
    public float cellRadius = 1.0f;

    [Tooltip("Capas válidas a las que se les puede hacer click (suelo)")]
    public LayerMask groundMask = ~0;

    [Header("Preview")]
    [Tooltip("Altura del preview/hex sobre el piso")]
    public float previewYOffset = 0.03f;
    [Tooltip("Grosor de la línea del hex")]
    public float lineWidth = 0.04f;

    // --- selección actual ---
    private struct SelectedCard
    {
        public GameObject prefab;
        public bool isUpgrade;
    }
    private SelectedCard selected;

    // Torretas por celda
    private readonly Dictionary<Vector2Int, GameObject> placedTowers = new();

    // Preview
    private LineRenderer hexPreview;
    private Vector2Int hoveredAxial;
    private bool hasValidHover;

    // Estado de puntero
    private Vector2 pointerPos;
    private bool pointerActive;          // hay mouse o al menos 1 toque
    private bool pressedThisFrame;       // click/touch Began
    private int activeFingerId = -1;     // finger que seguimos para hover/colocar

    void Awake()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;

        // LineRenderer
        var lrObj = new GameObject("HexPreview");
        lrObj.transform.SetParent(transform, false);
        hexPreview = lrObj.AddComponent<LineRenderer>();
        hexPreview.positionCount = 7;
        hexPreview.useWorldSpace = true;
        hexPreview.widthMultiplier = lineWidth;
        hexPreview.loop = false;
        hexPreview.numCornerVertices = 3;
        hexPreview.numCapVertices = 3;
        hexPreview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hexPreview.receiveShadows = false;
        hexPreview.alignment = LineAlignment.View;
        hexPreview.textureMode = LineTextureMode.Stretch;

        // Material que respeta color
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        hexPreview.material = new Material(shader);

        hexPreview.enabled = false;

        // evitar z-fighting
        previewYOffset = Mathf.Max(previewYOffset, 0.03f);
        lineWidth = Mathf.Max(lineWidth, 0.03f);
    }

    // --- API desde HandManager ---
    public void SetSelectedBuild(GameObject towerPrefab, CardHighlight sourceHighlight = null)
    {
        selected = new SelectedCard { prefab = towerPrefab, isUpgrade = false };
        currentUIHighlight = sourceHighlight;
        UpdatePreviewVisibility();
    }

    public void SetSelectedUpgrade(GameObject upgradePrefab, CardHighlight sourceHighlight = null)
    {
        selected = new SelectedCard { prefab = upgradePrefab, isUpgrade = true };
        currentUIHighlight = sourceHighlight;
        UpdatePreviewVisibility();
    }

    public void CancelSelection()
    {
        ClearSelectionAndUIHighlight();
    }

    void Update()
    {
        // 1) Leer input unificado (actualiza pointerPos, pressedThisFrame, pointerActive)
        ReadPointer();

        // 2) Actualizar hover/preview siguiendo el mouse o el dedo (Moved/Stationary)
        UpdateHoverAndPreview(pointerActive ? (Vector2?)pointerPos : null);

        if (selected.prefab == null) return;

        // 3) Colocar/mejorar en el frame del toque/click
        if (pressedThisFrame)
        {
            if (IsPointerOverUI()) return;
            if (!hasValidHover) return;

            // Centro del hex
            Vector3 basePos = HexGrid.AxialToWorld(hoveredAxial, cellRadius);
            Vector3 spawnPos = basePos;

            // Alinear Y al piso bajo el puntero
            Ray ray = cam.ScreenPointToRay(pointerPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
                spawnPos.y = hit.point.y;

            if (!selected.isUpgrade)
            {
                if (!placedTowers.ContainsKey(hoveredAxial))
                {
                    float lift = 0f;
                    var rend = selected.prefab.GetComponentInChildren<Renderer>();
                    if (rend != null) lift = rend.bounds.size.y * 0.5f;
                    spawnPos.y += lift;

                    var tower = Instantiate(selected.prefab, spawnPos, Quaternion.identity);
                    placedTowers[hoveredAxial] = tower;

                    var h = tower.GetComponent<TurretCellHandle>();
                    if (h == null) h = tower.AddComponent<TurretCellHandle>();
                    h.axial = hoveredAxial;
                }
            }
            else
            {
                if (placedTowers.TryGetValue(hoveredAxial, out var tower))
                {
                    var upgradable = tower.GetComponent<TowerUpgradable>();
                    if (upgradable == null) upgradable = tower.AddComponent<TowerUpgradable>();
                    upgradable.ApplyUpgrade(selected.prefab);
                }
            }

            // Consumir carta de la mano
            if (HandManager.Instance != null && currentUIHighlight != null)
                HandManager.Instance.RemoveCardByHighlight(currentUIHighlight);

            ClearSelectionAndUIHighlight();
        }

        // 4) Cancelar con dos dedos (o clic derecho en PC)
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        if (Input.GetMouseButtonDown(1))
        {
            selected = default;
            UpdatePreviewVisibility();
        }
#else
        if (Input.touchCount >= 2)
        {
            selected = default;
            UpdatePreviewVisibility();
        }
#endif
    }

    private void ClearSelectionAndUIHighlight()
    {
        selected = default;

        if (currentUIHighlight != null)
        {
            currentUIHighlight.SetSelected(false);
            currentUIHighlight = null;
        }

        if (HandManager.Instance != null)
            HandManager.Instance.NotifyPlacementCleared();

        UpdatePreviewVisibility();
    }

    private void UpdateHoverAndPreview(Vector2? pointer)
    {
        if (selected.prefab == null || !pointer.HasValue)
        {
            hexPreview.enabled = false;
            hasValidHover = false;
            return;
        }

        // Raycast al suelo desde el puntero actual
        Ray ray = cam.ScreenPointToRay(pointer.Value);
        if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 flatPoint = groundHit.point; flatPoint.y = 0f;

            Vector2Int axial = HexGrid.WorldToAxial(flatPoint, cellRadius);
            Vector3 center = HexGrid.AxialToWorld(axial, cellRadius);
            center.y = groundHit.point.y + previewYOffset;

            hoveredAxial = axial;

            bool cellHasTower = placedTowers.ContainsKey(axial);
            hasValidHover = selected.isUpgrade ? cellHasTower : !cellHasTower;

            // Dibujar hex
            if (hexPreview.positionCount != 7) hexPreview.positionCount = 7;
            Vector3[] corners = HexGrid.GetHexCorners(center, cellRadius);
            hexPreview.enabled = true;
            for (int i = 0; i < 6; i++) hexPreview.SetPosition(i, corners[i]);
            hexPreview.SetPosition(6, corners[0]);

            // Colores por estado
            Color c = !hasValidHover ? new Color(1f, 0.2f, 0.2f, 0.95f)   // rojo
                     : (selected.isUpgrade ? new Color(1f, 0.85f, 0.1f, 1f) // amarillo
                                           : new Color(0.2f, 0.9f, 0.2f, 1f)); // verde

            hexPreview.startColor = c;
            hexPreview.endColor = c;

            // URP/otros shaders pueden requerir setear el color del material
            var mat = hexPreview.material;
            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }
        }
        else
        {
            hexPreview.enabled = false;
            hasValidHover = false;
        }
    }

    private void UpdatePreviewVisibility()
    {
        hexPreview.enabled = (selected.prefab != null);
    }

    // Si destruyes una torreta, puedes liberar su celda:
    public void FreeCell(Vector2Int axial)
    {
        if (placedTowers.TryGetValue(axial, out var t))
        {
            placedTowers.Remove(axial);
            if (t != null) Destroy(t);
        }
        else
        {
            placedTowers.Remove(axial);
        }
    }

    // =========================
    //       INPUT UNIFICADO
    // =========================
    private void ReadPointer()
    {
        pressedThisFrame = false;
        pointerActive = false;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        // Mouse
        pointerPos = Input.mousePosition;
        pointerActive = true;
        if (Input.GetMouseButtonDown(0)) pressedThisFrame = true;
        activeFingerId = -1; // no aplica
#else
        // Touch
        if (Input.touchCount > 0)
        {
            // Usamos siempre el primer toque como puntero de “hover” y colocación.
            Touch t = Input.GetTouch(0);
            pointerPos = t.position;
            pointerActive = (t.phase == TouchPhase.Began ||
                             t.phase == TouchPhase.Moved ||
                             t.phase == TouchPhase.Stationary);

            if (t.phase == TouchPhase.Began)
            {
                pressedThisFrame = true;
                activeFingerId = t.fingerId;
            }
        }
        else
        {
            activeFingerId = -1;
        }
#endif
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        return EventSystem.current.IsPointerOverGameObject();
#else
        // Usa el fingerId activo si existe; si no, intenta con el 0.
        int fid = (activeFingerId >= 0) ? activeFingerId :
                  (Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1);
        return fid >= 0 && EventSystem.current.IsPointerOverGameObject(fid);
#endif
    }
}

// Helper para liberar celda al destruir una torreta
public class TurretCellHandle : MonoBehaviour
{
    public Vector2Int axial;
    private void OnDestroy()
    {
        if (CardPlacer.Instance != null)
            CardPlacer.Instance.FreeCell(axial);
    }
}
