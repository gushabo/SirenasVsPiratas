using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;                 // para Selectable (Button/Toggle/etc)
using UnityEngine.InputSystem;       // Touchscreen, Pointer, Mouse

public class CardPlacer : MonoBehaviour
{
    public static CardPlacer Instance;

    private CardHighlight currentUIHighlight;

    [Header("Camera")]
    public Camera cam;

    [Header("Grid")]
    public float cellRadius = 3.31f;
    public Vector3 gridOrigin = new Vector3(2.46f, 0f, 2.37f);

    [Tooltip("Capas que considera el raycast del terreno")]
    public LayerMask groundMask = ~0;

    [Tooltip("Altura del preview/hex sobre el piso")]
    public float previewYOffset = 0.5f;
    [Tooltip("Grosor de la línea del hex")]
    public float lineWidth = 0.32f;

    [Header("Preview Colors")]
    public Color colorBuild = new(0.2f, 0.9f, 0.2f, 0.7f);
    public Color colorUpgrade = new(1f, 0.85f, 0.1f, 1f);
    public Color colorRemove = new(1f, 0.2f, 0.2f, 0.95f);
    public Color colorInvalid = new(1f, 0.2f, 0.2f, 0.95f);

    // ===== DEBUG toggles (si te ayudan) =====
    [Header("Debug")]
    public bool ignoreUIForDebug = false;     // si true, nunca bloquea por UI
    public bool forceShowPreview = false;     // si true, muestra preview aunque no haya carta
    public bool useAllLayersForDebug = false; // si true, raycast en ~0 (todas las capas)

    private struct SelectedCard { public GameObject prefab; public bool isUpgrade; }
    private SelectedCard selected;

    private readonly Dictionary<Vector2Int, GameObject> placedTowers = new();

    // Preview
    private LineRenderer hexPreview;
    private MeshFilter hexFill;
    private MeshRenderer hexFillRenderer;

    // Hover
    private Vector2Int hoveredAxial;
    private bool hasValidHover;

    // Pointer
    private Vector2 pointerPos;
    private bool pointerActive;
    private bool pressedThisFrame;

    private bool removeMode = false;

    void Awake()
    {
        Instance = this;

        // Asegura cámara
        if (cam == null) cam = Camera.main;
        var comp = Object.FindFirstObjectByType<Camera>();
        if (cam == null) Debug.LogError("[CardPlacer] No hay cámara asignada ni Camera.main en escena.");

        // Borde (LineRenderer)
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

        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit");
        hexPreview.material = new Material(shader);
        hexPreview.enabled = false;

        // Relleno (Mesh)
        var fillObj = new GameObject("HexFill");
        fillObj.transform.SetParent(transform, false);
        hexFill = fillObj.AddComponent<MeshFilter>();
        hexFillRenderer = fillObj.AddComponent<MeshRenderer>();
        var shaderFill = Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Unlit");
        var matFill = new Material(shaderFill) { color = new Color(0f, 1f, 0f, 0.5f) };
        hexFillRenderer.material = matFill;
        hexFillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hexFillRenderer.receiveShadows = false;
        hexFill.gameObject.SetActive(false);

        previewYOffset = Mathf.Max(previewYOffset, 0.03f);
        lineWidth = Mathf.Max(lineWidth, 0.03f);
    }

    // ======= API =======
    public void SetSelectedBuild(GameObject towerPrefab, CardHighlight sourceHighlight = null)
    {
        removeMode = false;
        selected = new SelectedCard { prefab = towerPrefab, isUpgrade = false };
        currentUIHighlight = sourceHighlight;
        UpdatePreviewVisibility();
    }
    public void SetSelectedUpgrade(GameObject upgradePrefab, CardHighlight sourceHighlight = null)
    {
        removeMode = false;
        selected = new SelectedCard { prefab = upgradePrefab, isUpgrade = true };
        currentUIHighlight = sourceHighlight;
        UpdatePreviewVisibility();
    }
    public void CancelSelection() => ClearSelectionAndUIHighlight();
    public void ToggleRemoveMode() { selected = default; removeMode = !removeMode; UpdatePreviewVisibility(); }
    public void ExitRemoveMode() { if (!removeMode) return; removeMode = false; UpdatePreviewVisibility(); }

    // ======= Loop =======
    void Update()
    {
        ReadPointer();

        UpdateHoverAndPreview(pointerActive ? (Vector2?)pointerPos : null);

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ExitRemoveMode();

        if (pressedThisFrame)
        {
            if (!ignoreUIForDebug && IsOverBlockingUI(pointerPos)) return;
            if (!hasValidHover) return;

            if (removeMode)
            {
                if (placedTowers.TryGetValue(hoveredAxial, out var toDel) && toDel != null)
                {
                    placedTowers.Remove(hoveredAxial);
                    Destroy(toDel);
                }
                ExitRemoveMode();
                return;
            }

            if (selected.prefab == null && !forceShowPreview) return;

            Vector3 basePos = HexGridFlat.AxialToWorld(hoveredAxial, cellRadius, gridOrigin);
            Vector3 spawnPos = basePos;

            int mask = useAllLayersForDebug ? ~0 : groundMask.value;
            Ray ray = cam.ScreenPointToRay(pointerPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, mask, QueryTriggerInteraction.Collide))
                spawnPos.y = hit.point.y;

            if (selected.prefab != null && !selected.isUpgrade)
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
            else if (selected.prefab != null && selected.isUpgrade)
            {
                if (placedTowers.TryGetValue(hoveredAxial, out var tower))
                {
                    var upgradable = tower.GetComponent<TowerUpgradable>();
                    if (upgradable == null) upgradable = tower.AddComponent<TowerUpgradable>();
                    upgradable.ApplyUpgrade(selected.prefab);
                }
            }

            if (HandManager.Instance != null && currentUIHighlight != null)
                HandManager.Instance.RemoveCardByHighlight(currentUIHighlight);

            ClearSelectionAndUIHighlight();
        }

        // Salida rápida con dos dedos (si estás usando gestures propios, quítalo)
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
        {
            selected = default;
            removeMode = false;
            UpdatePreviewVisibility();
        }
    }

    // ======= Aux =======
    private void ClearSelectionAndUIHighlight()
    {
        selected = default;
        removeMode = false;

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
        bool showPreview = forceShowPreview || (selected.prefab != null) || removeMode;

        if (!showPreview || !pointer.HasValue)
        {
            hexPreview.enabled = false;
            hexFill.gameObject.SetActive(false);
            hasValidHover = false;
            return;
        }

        int mask = useAllLayersForDebug ? ~0 : groundMask.value;
        Ray ray = cam.ScreenPointToRay(pointer.Value);
        if (Physics.Raycast(ray, out RaycastHit groundHit, 500f, mask, QueryTriggerInteraction.Collide))
        {
            Vector3 flatPoint = groundHit.point; flatPoint.y = 0f;

            Vector2Int axial = HexGridFlat.WorldToAxial(flatPoint, cellRadius, gridOrigin);
            Vector3 center = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin);
            center.y = groundHit.point.y + previewYOffset;

            hoveredAxial = axial;
            bool cellHasTower = placedTowers.ContainsKey(axial);

            hasValidHover = removeMode
                ? cellHasTower
                : (selected.prefab == null && forceShowPreview) ? true
                : (selected.isUpgrade ? cellHasTower : !cellHasTower);

            if (hexPreview.positionCount != 7) hexPreview.positionCount = 7;
            Vector3[] corners = HexGridFlat.GetHexCorners(center, cellRadius);
            hexPreview.enabled = true;
            for (int i = 0; i < 6; i++) hexPreview.SetPosition(i, corners[i]);
            hexPreview.SetPosition(6, corners[0]);

            Color c = removeMode
                ? (hasValidHover ? colorRemove : colorInvalid)
                : (!hasValidHover ? colorInvalid : (selected.isUpgrade ? colorUpgrade : colorBuild));

            hexPreview.startColor = c;
            hexPreview.endColor = c;

            var mat = hexPreview.material;
            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }

            hexFill.mesh = HexGridFlat.BuildHexMesh(center, cellRadius);
            hexFill.gameObject.SetActive(true);

            var fillMat = hexFillRenderer.material;
            if (fillMat.HasProperty("_Color")) fillMat.color = c;
            if (fillMat.HasProperty("_BaseColor")) fillMat.SetColor("_BaseColor", c);
        }
        else
        {
            hexPreview.enabled = false;
            hexFill.gameObject.SetActive(false);
            hasValidHover = false;
        }
    }

    private void UpdatePreviewVisibility()
    {
        bool show = forceShowPreview || (selected.prefab != null) || removeMode;
        hexPreview.enabled = show;
        if (hexFill != null) hexFill.gameObject.SetActive(show);
    }

    public void FreeCell(Vector2Int axial)
    {
        if (placedTowers.TryGetValue(axial, out var t))
        {
            placedTowers.Remove(axial);
            if (t != null) Destroy(t);
        }
        else placedTowers.Remove(axial);
    }

    // ======= INPUT (solo New) =======
    private void ReadPointer()
    {
        pressedThisFrame = false;
        pointerActive = false;

        // 1) Toque real (Touchscreen)
        var ts = Touchscreen.current;
        if (ts != null)
        {
            var t = ts.primaryTouch;
            pointerPos = t.position.ReadValue();
            pressedThisFrame = t.press.wasPressedThisFrame;
            pointerActive = t.press.isPressed;
            if (pointerActive) return;
        }

        // 2) Mouse/Pointer (Editor/PC)
        if (Pointer.current != null)
        {
            pointerPos = Pointer.current.position.ReadValue();
            pointerActive = true;
            pressedThisFrame = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }
    }

    // ======= UI: bloquea SOLO controles interactivos (Buttons/Toggles/etc) =======
    private static readonly List<RaycastResult> _uiHits = new List<RaycastResult>(16);
    private bool IsOverBlockingUI(Vector2 screenPos)
    {
        if (ignoreUIForDebug) return false;
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        _uiHits.Clear();
        EventSystem.current.RaycastAll(ped, _uiHits);

        for (int i = 0; i < _uiHits.Count; i++)
        {
            var go = _uiHits[i].gameObject;
            if (!go) continue;
            if (go.GetComponent<Selectable>() != null) // Button/Toggle/Slider/etc
                return true;
        }
        return false;
    }
}

public class TurretCellHandle : MonoBehaviour
{
    public Vector2Int axial;
    private void OnDestroy()
    {
        if (CardPlacer.Instance != null)
            CardPlacer.Instance.FreeCell(axial);
    }
}
