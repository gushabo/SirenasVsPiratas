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
    public float previewYOffset = 0.02f;
    [Tooltip("Grosor de la línea del hex gris")]
    public float lineWidth = 0.03f;

    // --- NUEVO: tipo de carta seleccionada ---
    private struct SelectedCard
    {
        public GameObject prefab;   // prefab de torreta o de “módulo de mejora”
        public bool isUpgrade;      // true = mejora, false = construir
    }
    private SelectedCard selected;

    // En lugar de HashSet, guardamos la torreta colocada por celda
    private readonly Dictionary<Vector2Int, GameObject> placedTowers = new();

    // Vista previa (hex)
    private LineRenderer hexPreview;
    private Vector2Int hoveredAxial;
    private bool hasValidHover;

    private void Awake()
    {
        Instance = this;

        if (cam == null) cam = Camera.main;

        // Crear el line renderer para el hex gris
        GameObject lrObj = new GameObject("HexPreview");
        lrObj.transform.SetParent(transform, false);
        hexPreview = lrObj.AddComponent<LineRenderer>();
        hexPreview.positionCount = 7;
        hexPreview.useWorldSpace = true;
        hexPreview.widthMultiplier = lineWidth;
        hexPreview.loop = false;

        // Mejor compatibilidad (URP/Default):
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        hexPreview.material = new Material(shader);

        hexPreview.startColor = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        hexPreview.endColor = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        hexPreview.numCornerVertices = 3;
        hexPreview.numCapVertices = 3;
        hexPreview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        hexPreview.receiveShadows = false;
        hexPreview.sortingOrder = 1000;
        hexPreview.enabled = false;
    }

    // --- NUEVO: seleccionar carta con tipo ---
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
        Debug.Log("Hice cositas");
        UpdatePreviewVisibility();
    }

    private void Update()
    {
        UpdateHoverAndPreview();

        // Click izquierdo para colocar / mejorar
        if (selected.prefab != null && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!hasValidHover) return;

            // Centro del hex en mundo
            Vector3 basePos = HexGrid.AxialToWorld(hoveredAxial, cellRadius);
            Vector3 spawnPos = basePos;

            // Alinear Y con el suelo bajo el mouse
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
                spawnPos.y = hit.point.y;

            if (!selected.isUpgrade)
            {
                // Construcción de nueva torreta (solo si no hay torreta en esta celda)
                if (!placedTowers.ContainsKey(hoveredAxial))
                {
                    float lift = 0f;
                    var rend = selected.prefab.GetComponentInChildren<Renderer>();
                    if (rend != null) lift = rend.bounds.size.y * 0.5f;
                    spawnPos.y += lift;

                    var tower = Instantiate(selected.prefab, spawnPos, Quaternion.identity);
                    placedTowers[hoveredAxial] = tower;

                    // (Opcional) guarda su axial para liberar luego
                    var h = tower.GetComponent<TurretCellHandle>();
                    if (h == null) h = tower.AddComponent<TurretCellHandle>();
                    h.axial = hoveredAxial;
                }
            }
            else
            {
                // Mejora: requiere que YA exista una torreta en la celda
                if (placedTowers.TryGetValue(hoveredAxial, out var tower))
                {
                    // Estrategia simple: instanciar el “módulo” como hijo
                    // o llamar a un componente de la torre que aplique la mejora.
                    var upgradable = tower.GetComponent<TowerUpgradable>();
                    if (upgradable == null) upgradable = tower.AddComponent<TowerUpgradable>();

                    upgradable.ApplyUpgrade(selected.prefab);
                }
            }

            // Tras colocar o mejorar con éxito:
            if (HandManager.Instance != null && currentUIHighlight != null)
            {
                HandManager.Instance.RemoveCardByHighlight(currentUIHighlight);
            }

           



            ClearSelectionAndUIHighlight();

            
        }

        // Click derecho para cancelar
        if (Input.GetMouseButtonDown(1))
        {
            selected = default;
            UpdatePreviewVisibility();
        }
    }

    private void ClearSelectionAndUIHighlight()
    {
        selected = default;

        // Apaga el highlight del botón que estaba activo (si sigues usando currentUIHighlight)
        if (currentUIHighlight != null)
        {
            currentUIHighlight.SetSelected(false);
            currentUIHighlight = null;
        }

        // --- NUEVO: que HandManager apague CUALQUIER otra carta seleccionada ---
        if (HandManager.Instance != null)
            HandManager.Instance.NotifyPlacementCleared();

        UpdatePreviewVisibility();
    }

    private void UpdateHoverAndPreview()
    {
        if (selected.prefab == null)
        {
            hexPreview.enabled = false;
            hasValidHover = false;
            return;
        }

        // Raycast SOLO al suelo (groundMask), no colisiona con torretas
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 flatPoint = groundHit.point;
            flatPoint.y = 0f;

            Vector2Int axial = HexGrid.WorldToAxial(flatPoint, cellRadius);
            Vector3 center = HexGrid.AxialToWorld(axial, cellRadius);
            center.y = groundHit.point.y + previewYOffset;

            hoveredAxial = axial;

            bool cellHasTower = placedTowers.ContainsKey(axial);
            hasValidHover = selected.isUpgrade ? cellHasTower : !cellHasTower;

            if (hexPreview.positionCount != 7) hexPreview.positionCount = 7;
            Vector3[] corners = HexGrid.GetHexCorners(center, cellRadius);
            hexPreview.enabled = true;
            for (int i = 0; i < 6; i++) hexPreview.SetPosition(i, corners[i]);
            hexPreview.SetPosition(6, corners[0]);

            Color c = hasValidHover ? new Color(0.2f, 0.9f, 0.2f, 1f) : new Color(1f, 0.2f, 0.2f, 0.95f);
            hexPreview.startColor = c;
            hexPreview.endColor = c;
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
}

// Pequeño helper para liberar celda al destruir una torreta
public class TurretCellHandle : MonoBehaviour
{
    public Vector2Int axial;
    private void OnDestroy()
    {
        if (CardPlacer.Instance != null)
            CardPlacer.Instance.FreeCell(axial);
    }
}
