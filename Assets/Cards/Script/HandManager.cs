using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    public static HandManager Instance; // <--- NUEVO

    [Header("Referencias")]
    public DeckManager deckManager;

    [Header("Prefabs/UI")]
    public GameObject cardPrefab;
    public Transform handTransform;

    [Header("Layout")]
    public float fanSpread = 6f;
    public float cardSpacing = -120f;
    public float verticalSpacing = 40f;

    public int cardsMaxInHand = 8;

    public List<GameObject> cardsInHand = new List<GameObject>();

    // --- NUEVO: selección actual ---
    private CardHighlight currentSelected;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // if (deckManager != null) deckManager.DrawCards(this, 6);
        UpdateHandVisuals();
    }

 
    

    public void AddCardToHand(GameObject cardPrefabToUse)
    {
        if (cardPrefabToUse == null || handTransform == null) return;

       

        GameObject newCard = Instantiate(
            cardPrefabToUse,
            handTransform.position,
            Quaternion.identity,
            handTransform
        );

       
        var hl = newCard.GetComponentInChildren<CardHighlight>(true);
        if (hl != null) hl.BindRoot(newCard);

        cardsInHand.Add(newCard);
         UpdateHandVisuals();
    }




    public void SelectBuild(GameObject buildPrefab, CardHighlight highlight)
    {
        DeselectAll();
        currentSelected = highlight;
        if (currentSelected != null) currentSelected.SetSelected(true);

        // Reenvía al colocador hex con referencia al highlight
        if (HexGridCardPlacer.Instance != null)
            HexGridCardPlacer.Instance.SelectBuild(buildPrefab, currentSelected);
    }


    public void SelectUpgrade(GameObject upgradePrefab, CardHighlight highlight)
    {
        DeselectAll();
        currentSelected = highlight;
        if (currentSelected != null) currentSelected.SetSelected(true);

        // Reenvía al colocador hex con referencia al highlight
        if (HexGridCardPlacer.Instance != null)
            HexGridCardPlacer.Instance.SelectUpgrade(upgradePrefab, currentSelected);
    }

    public void DeselectAll()
    {
        if (handTransform == null) return;

        // Apaga TODO CardHighlight que esté bajo el contenedor de la mano
        var all = handTransform.GetComponentsInChildren<CardHighlight>(true);
        foreach (var hl in all)
            hl.SetSelected(false);

        currentSelected = null;
    }


    // Lo llama CardPlacer cuando coloca o cancelas
    public void NotifyPlacementCleared()
    {
        DeselectAll();
    }

    // ---------- (lo demás igual) ----------
    void UpdateHandVisuals()
    {
        int cardCount = cardsInHand.Count;
        if (cardCount == 0) return;

        if (cardCount == 1)
        {
            cardsInHand[0].transform.localRotation = Quaternion.identity;
            cardsInHand[0].transform.localPosition = Vector3.zero;
            return;
        }

        for (int i = 0; i < cardCount; i++)
        {
            float rotationAngle = fanSpread * (i - (cardCount - 1) / 2f);
            Transform t = cardsInHand[i].transform;
            t.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);

            float horizontalOffSet = cardSpacing * (i - (cardCount - 1) / 2f);
            float normalized = (2f * i / (cardCount - 1) - 1f);
            float verticalOffSet = verticalSpacing * (1 - normalized * normalized);

            t.localPosition = new Vector3(horizontalOffSet, verticalOffSet, 0f);
        }
    }

    public void RemoveCard(GameObject cardGO)
    {
        if (cardGO == null) return;

        // 1) ¿Está tal cual en la lista?
        if (cardsInHand.Contains(cardGO))
        {
            cardsInHand.Remove(cardGO);
            Destroy(cardGO);
            UpdateHandVisuals();
            return;
        }

        // 2) ¿Es un hijo de alguno de los roots en la mano?
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

        // Opcional: log para depurar
        Debug.LogWarning($"RemoveCard: no encontré {cardGO.name} en cardsInHand (¿pasaste el objeto correcto?).");
    }
    public void RemoveCardByHighlight(CardHighlight hl)
    {
        if (hl == null) return;

        GameObject target = hl.CardRoot != null ? hl.CardRoot : hl.gameObject;

        // 1) ¿Está tal cual en la lista?
        if (cardsInHand.Contains(target))
        {
            cardsInHand.Remove(target);
            Destroy(target);
            UpdateHandVisuals();
            return;
        }

        // 2) ¿Es hijo de alguna carta en mano?
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

    




}
