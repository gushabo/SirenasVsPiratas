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

    [Header("Referencias")]
    public Camera cam;
    [Tooltip("Capa del piso/terreno para el raycast")]
    public LayerMask groundMask = ~0;

    [Header("Grid Hex Flat-Top")]
    public float cellRadius = 1.2f;
    public Vector3 gridOrigin = Vector3.zero;
    [Tooltip("Altura para asentar la torreta sobre el piso")]
    public float buildYOffset = 0.02f;

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

        Vector3 point = hit.point;
        Vector2Int axial = HexGridFlat.WorldToAxial(point, cellRadius, gridOrigin);

        if (selected.isUpgrade)
        {
            // Mejorar requiere que exista una torre en la celda
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
            // Construir solo si la celda está libre
            if (placedBuilds.ContainsKey(axial) && placedBuilds[axial] != null)
            {
                Debug.Log($"[{name}] Celda {axial} ocupada. No se puede construir encima.");
                return;
            }

            Vector3 spawnPos = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, hit.point.y + buildYOffset);
            Quaternion rot = Quaternion.identity;

            var go = Instantiate(selected.prefab, spawnPos, rot);
            placedBuilds[axial] = go;

            // (Opcional) si tu torre necesita saber su celda:
            // var meta = go.GetComponent<TowerCellMeta>(); if (meta) meta.Cell = axial;

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

        Vector2 screenPos = Vector2.zero; // ✅ inicializada
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

        if (!hasPointer) return; // ✅ evita usar screenPos sin valor

        if (!Physics.Raycast(cam.ScreenPointToRay(screenPos), out var hit, 2000f, groundMask))
        {
            if (hexOutline) hexOutline.enabled = false;
            hasHover = false;
            return;
        }

        Vector2Int axial = HexGridFlat.WorldToAxial(hit.point, cellRadius, gridOrigin);
        if (!hasHover || axial != hoveredAxial)
        {
            hoveredAxial = axial;
            hasHover = true;

            if (hexOutline != null)
            {
                Vector3 center = HexGridFlat.AxialToWorld(axial, cellRadius, gridOrigin, hit.point.y + 0.001f);
                var corners = HexGridFlat.GetHexCorners(center, cellRadius);
                hexOutline.enabled = true;
                for (int i = 0; i < 6; i++)
                    hexOutline.SetPosition(i, corners[i]);
                hexOutline.SetPosition(6, corners[0]); // cerrar
            }
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
}
