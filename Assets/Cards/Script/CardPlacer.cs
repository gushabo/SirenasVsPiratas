using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class CardPlacer : MonoBehaviour
{
    public static CardPlacer Instance;

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

    private GameObject selectedPrefab;

    // Control de ocupación por celda axial
    private HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    // Vista previa del hex
    private LineRenderer hexPreview;
    private Vector2Int hoveredAxial;
    private bool hasValidHover;

   

    private void Awake()
    {
        Instance = this;

        if (cam == null)
            cam = Camera.main;

        // Crear el line renderer para el hex gris
        GameObject lrObj = new GameObject("HexPreview");
        lrObj.transform.SetParent(transform, false);
        hexPreview = lrObj.AddComponent<LineRenderer>();
        hexPreview.positionCount = 7; // 6 vértices + el primero para cerrar el loop
        hexPreview.useWorldSpace = true;
        hexPreview.widthMultiplier = lineWidth;
        hexPreview.loop = false;
        hexPreview.material = new Material(Shader.Find("Sprites/Default")); // simple, sin iluminación
        hexPreview.startColor = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        hexPreview.endColor = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        hexPreview.enabled = false;
    }

    public void SetSelectedCard(GameObject prefab)
    {
        selectedPrefab = prefab;
        UpdatePreviewVisibility();
    }

    private void Update()
    {
        UpdateHoverAndPreview();

        if (selectedPrefab != null && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!hasValidHover) return;

            // Ya validamos que no está ocupada
            Vector3 basePos = HexGrid.AxialToWorld(hoveredAxial, cellRadius);
            Vector3 spawnPos = basePos;

            // Ajustar altura con el raycast real al piso para respetar terreno
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
                spawnPos.y = hit.point.y;

            // Levantar la torreta la mitad de su altura (si tiene Renderer)
            float lift = 0f;
            var rend = selectedPrefab.GetComponentInChildren<Renderer>();
            if (rend != null) lift = rend.bounds.size.y * 0.5f;
            spawnPos.y += lift;

            Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
            occupied.Add(hoveredAxial);

            // Limpiar selección después de colocar
            selectedPrefab = null;
            UpdatePreviewVisibility();
        }

        // Cancelar selección (opcional con click derecho)
        if (Input.GetMouseButtonDown(1))
        {
            selectedPrefab = null;
            UpdatePreviewVisibility();
        }
    }

    private void UpdateHoverAndPreview()
    {
        if (selectedPrefab == null)
        {
            hexPreview.enabled = false;
            hasValidHover = false;
            return;
        }

        //if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        //{
        //    hexPreview.enabled = false;
        //    hasValidHover = false;
        //    return;
        //}

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
        {
            // Calcular celda axial
            Vector3 flatPoint = hit.point;
            flatPoint.y = 0f; // proyección al plano XZ para el cálculo

            Vector2Int axial = HexGrid.WorldToAxial(flatPoint, cellRadius);
            Vector3 center = HexGrid.AxialToWorld(axial, cellRadius);
            center.y = hit.point.y + previewYOffset;

            hoveredAxial = axial;
            bool isFree = !occupied.Contains(axial);
            hasValidHover = isFree;

            // Dibujar hex
            Vector3[] corners = HexGrid.GetHexCorners(new Vector3(center.x, center.y, center.z), cellRadius);
            hexPreview.enabled = true;
            for (int i = 0; i < 6; i++)
                hexPreview.SetPosition(i, corners[i]);
            hexPreview.SetPosition(6, corners[0]); // cerrar

            // Color: gris si libre, rojo si ocupado
            Color c = isFree ? new Color(0.7f, 0.7f, 0.7f, 0.9f) : new Color(1f, 0.2f, 0.2f, 0.9f);
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
        hexPreview.enabled = (selectedPrefab != null);
    }

    // (Opcional) Si luego quieres liberar una celda cuando destruyas una torreta:
    public void FreeCell(Vector2Int axial)
    {
        occupied.Remove(axial);
    }
}
