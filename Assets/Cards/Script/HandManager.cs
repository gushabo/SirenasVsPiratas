using System;
using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance;

    [Header("Referencias")]
    public DeckManager deckManager;

    [Header("Prefabs/UI")]
    public GameObject cardPrefab;
    public Transform handTransform;

    // ====== LAYOUT ======
    public enum HandLayout { HorizontalFan, VerticalColumnRight, VerticalArcRight }
    [Header("Layout")]
    public HandLayout layout = HandLayout.VerticalArcRight;

    [Tooltip("Máx. cartas con las que se calcula compresión de espacio")]
    public int cardsMaxInHand = 8;

    // --- Horizontal (como lo tenías) ---
    [Header("Horizontal Fan")]
    public float fanSpread = 6f;
    public float cardSpacing = -120f;
    public float verticalSpacing = 40f;

    // --- Vertical recto ---
    [Header("Vertical Column")]
    public float vSpacing = -180f;        // negativo = hacia abajo
    public float vTilt = -3f;             // tilt general

    // --- Vertical con arco (recomendado) ---
    [Header("Vertical Arc Right")]
    public float vArcSpacing = -170f;     // distancia entre cartas
    public float vArcCurveX = 80f;        // cuánto “se mete” al centro (curvatura en X)
    public float vArcTilt = -6f;          // inclinación por carta
    public float vArcSpread = 8f;         // abanico adicional por índice

    public List<GameObject> cardsInHand = new List<GameObject>();

    private CardHighlight currentSelected;

    void Awake() => Instance = this;

    void Start() => UpdateHandVisuals();


    private void Update()
    {
        UpdateHandVisuals();
    }

    // ===== API =====
    public void AddCardToHand(GameObject cardPrefabToUse)
    {
        if (!cardPrefabToUse || !handTransform) return;

        var newCard = Instantiate(cardPrefabToUse, handTransform.position, Quaternion.identity, handTransform);

        var hl = newCard.GetComponentInChildren<CardHighlight>(true);
        if (hl) hl.BindRoot(newCard);

        cardsInHand.Add(newCard);
        UpdateHandVisuals();
    }

    public void RemoveCard(GameObject cardGO)
    {
        if (!cardGO) return;

        if (cardsInHand.Contains(cardGO))
        {
            cardsInHand.Remove(cardGO);
            Destroy(cardGO);
            UpdateHandVisuals();
            return;
        }

        for (int i = cardsInHand.Count - 1; i >= 0; i--)
        {
            var root = cardsInHand[i];
            if (root == null) { cardsInHand.RemoveAt(i); continue; }

            if (cardGO.transform == root.transform || cardGO.transform.IsChildOf(root.transform))
            {
                cardsInHand.RemoveAt(i);
                Destroy(root);
                UpdateHandVisuals();
                return;
            }
        }

        Debug.LogWarning($"RemoveCard: no encontré {cardGO.name} en cardsInHand.");
    }

    public void RemoveCardByHighlight(CardHighlight hl)
    {
        if (!hl) return;
        GameObject target = hl.CardRoot ? hl.CardRoot : hl.gameObject;

        if (cardsInHand.Contains(target))
        {
            cardsInHand.Remove(target);
            Destroy(target);
            UpdateHandVisuals();
            return;
        }

        for (int i = cardsInHand.Count - 1; i >= 0; i--)
        {
            var root = cardsInHand[i];
            if (root == null) { cardsInHand.RemoveAt(i); continue; }

            if (hl.transform == root.transform || hl.transform.IsChildOf(root.transform))
            {
                cardsInHand.RemoveAt(i);
                Destroy(root);
                UpdateHandVisuals();
                return;
            }
        }

        Debug.LogWarning($"RemoveCardByHighlight: no encontré {target.name} en cardsInHand.");
    }

    public void SelectBuild(GameObject buildPrefab, CardHighlight highlight)
    {
        DeselectAll();
        currentSelected = highlight;
        if (currentSelected) currentSelected.SetSelected(true);
        if (HexGridCardPlacer.Instance)
            HexGridCardPlacer.Instance.SelectBuild(buildPrefab, currentSelected);
    }

    public void SelectUpgrade(GameObject upgradePrefab, CardHighlight highlight, AudioClip sfx)
    {
        DeselectAll();
        currentSelected = highlight;
        if (currentSelected) currentSelected.SetSelected(true);
        if (HexGridCardPlacer.Instance)
            HexGridCardPlacer.Instance.SelectUpgrade(upgradePrefab, currentSelected, sfx);
    }

    public void DeselectAll()
    {
        if (!handTransform) return;
        var all = handTransform.GetComponentsInChildren<CardHighlight>(true);
        foreach (var hl in all) hl.SetSelected(false);
        currentSelected = null;
    }

    public void NotifyPlacementCleared() => DeselectAll();

    // ===== LAYOUT CORE =====
    void UpdateHandVisuals()
    {
        int n = cardsInHand.Count;
        if (n == 0) return;

        // compresión suave cuando hay muchas cartas
        float density = Mathf.InverseLerp(1f, Mathf.Max(2, cardsMaxInHand), n);

        for (int i = 0; i < n; i++)
        {
            Transform t = cardsInHand[i].transform;

            // que el último agregado quede “encima”
            t.SetSiblingIndex(i);

            switch (layout)
            {
                case HandLayout.HorizontalFan:
                {
                    float rot = fanSpread * (i - (n - 1) / 2f);
                    float x = Mathf.Lerp(cardSpacing, cardSpacing * 0.6f, density) * (i - (n - 1) / 2f);
                    float norm = (2f * i / (n - 1) - 1f);
                    float y = Mathf.Lerp(verticalSpacing, verticalSpacing * 0.5f, density) * (1 - norm * norm);

                    t.localRotation = Quaternion.Euler(0, 0, rot);
                    t.localPosition = new Vector3(x, y, 0);
                    break;
                }

                case HandLayout.VerticalColumnRight:
                {
                    float y = Mathf.Lerp(vSpacing, vSpacing * 0.7f, density) * (i - (n - 1) / 2f);
                    t.localRotation = Quaternion.Euler(0, 0, vTilt);
                    t.localPosition = new Vector3(0f, y, 0f);
                    break;
                }

                case HandLayout.VerticalArcRight:
                default:
                {
                    // columna vertical con curvatura hacia la izquierda (X) y pequeño abanico
                    float y = Mathf.Lerp(vArcSpacing, vArcSpacing * 0.7f, density) * (i - (n - 1) / 2f);
                    float norm = (n == 1) ? 0f : (2f * i / (n - 1) - 1f); // -1..1
                    float x = vArcCurveX * (1f - norm * norm);           // curva tipo U hacia adentro
                    float rot = vArcTilt + vArcSpread * norm;            // ligero abanico

                    t.localRotation = Quaternion.Euler(0, 0, rot);
                    t.localPosition = new Vector3(-x, y, 0f);            // “-x” si tu mano está a la derecha
                    break;
                }
            }
        }
    }
}
