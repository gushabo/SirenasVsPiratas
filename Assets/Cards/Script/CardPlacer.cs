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
    public float cellRadius = 1.2f;
    public Vector3 gridOrigin = Vector3.zero;

    public LayerMask groundMask = ~0;

    [Tooltip("Altura del preview/hex sobre el piso")]
    public float previewYOffset = 0.03f;
    [Tooltip("Grosor de la línea del hex")]
    public float lineWidth = 0.04f;

    private struct SelectedCard
    {
        public GameObject prefab;
        public bool isUpgrade;
    }
    private SelectedCard selected;

    private readonly Dictionary<Vector2Int, GameObject> placedTowers = new();

    // Preview
    private LineRenderer hexPreview;
    private MeshFilter hexFill;
    private MeshRenderer hexFillRenderer;

    private Vector2Int hoveredAxial;
    private bool hasValidHover;

    private Vector2 pointerPos;
    private bool pointerActive;
    private bool pressedThisFrame;
    private int activeFingerId = -1;

    void Awake()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;

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

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        hexPreview.material = new Material(shader);

        hexPreview.enabled = false;

        // Relleno (Mesh)
        var fillObj = new GameObject("HexFill");
        fillObj.transform.SetParent(transform, false);
        hexFill = fillObj.AddComponent<MeshFilter>();
        hexFillRenderer = fillObj.AddComponent<MeshRenderer>();

        var shaderFill = Shader.Find("Unlit/Color");
        if (shaderFill == null) shaderFill = Shader.Find("Universal Render Pipeline/Unlit");

        var matFill = new Material(shaderFill);
        matFill.color = new Color(0f, 1f, 0f, 0.5f); // verde transparente
        hexFillRenderer.material = matFill;

        hexFillRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hexFillRenderer.receiveShadows = false;


        hexFill.gameObject.SetActive(false);

        previewYOffset = Mathf.Max(previewYOffset, 0.03f);
        lineWidth = Mathf.Max(lineWidth, 0.03f);
    }

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
        ReadPointer();
        UpdateHoverAndPreview(pointerActive ? (Vector2?)pointerPos : null);

        if (selected.prefab == null) return;

        if (pressedThisFrame)
        {
            if (IsPointerOverUI()) return;
            if (!hasValidHover) return;

            Vector3 basePos = HexGridFlat.AxialToWorld(hoveredAxial, cellRadius, gridOrigin);
            Vector3 spawnPos = basePos;

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

            if (HandManager.Instance != null && currentUIHighlight != null)
                HandManager.Instance.RemoveCardByHighlight(currentUIHighlight);

            ClearSelectionAndUIHighlight();
        }

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
            hexFill.gameObject.SetActive(false);
            hasValidHover = false;
            return;
        }

        Ray ray = cam.ScreenPointToRay(pointer.Value);
        if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 flatPoint = groundHit.point; flatPoint.y = 0f;

            Vector2Int axial = HexGridFlat.WorldToAxial(flatPoint, cellRadius, gridOrigin);
            Vector3 center = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin);
            center.y = groundHit.point.y + previewYOffset;

            hoveredAxial = axial;
            bool cellHasTower = placedTowers.ContainsKey(axial);
            hasValidHover = selected.isUpgrade ? cellHasTower : !cellHasTower;

            // Borde
            if (hexPreview.positionCount != 7) hexPreview.positionCount = 7;
            Vector3[] corners = HexGridFlat.GetHexCorners(center, cellRadius);
            hexPreview.enabled = true;
            for (int i = 0; i < 6; i++) hexPreview.SetPosition(i, corners[i]);
            hexPreview.SetPosition(6, corners[0]);

            // Color
            Color c = !hasValidHover ? new Color(1f, 0.2f, 0.2f, 0.95f)   // rojo
                     : (selected.isUpgrade ? new Color(1f, 0.85f, 0.1f, 1f) // amarillo
                                           : new Color(0.2f, 0.9f, 0.2f, 0.7f)); // verde semi-transparente

            hexPreview.startColor = c;
            hexPreview.endColor = c;

            var mat = hexPreview.material;
            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }

            // Relleno
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
        hexPreview.enabled = (selected.prefab != null);
        if (hexFill != null) hexFill.gameObject.SetActive(selected.prefab != null);
    }

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

    private void ReadPointer()
    {
        pressedThisFrame = false;
        pointerActive = false;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        pointerPos = Input.mousePosition;
        pointerActive = true;
        if (Input.GetMouseButtonDown(0)) pressedThisFrame = true;
        activeFingerId = -1;
#else
        if (Input.touchCount > 0)
        {
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
        int fid = (activeFingerId >= 0) ? activeFingerId :
                  (Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1);
        return fid >= 0 && EventSystem.current.IsPointerOverGameObject(fid);
#endif
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
