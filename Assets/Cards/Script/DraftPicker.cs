using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DraftPicker : MonoBehaviour
{
    [Header("Referencias")]
    public DeckManager deckManager;
    public HandManager handManager;

    [Header("UI Draft")]
    public Transform draftContainer;     // contenedor con HorizontalLayoutGroup
    public GameObject cardChoicePrefab;  // ← TU CardChoiceUI
    public Button confirmButton;
    public Button rerollButton;          // opcional
    public GameObject overlayPanel;      // panel negro que bloquea fondo
    public Text counterText;             // "0/3" (UI.Text normal)

    [Header("Parámetros")]
    public int choicesCount = 5;
    public int picksAllowed = 3;
    public bool allowDuplicates = false;

    private readonly List<DraftSelectableUI> _choices = new();
    private int _selectedCount;

    void Awake()
    {
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmSelection);
        if (rerollButton) rerollButton.onClick.AddListener(Reroll);
        HideDraftUI();
    }

    public bool IsOpen() => overlayPanel && overlayPanel.activeSelf;

    public void StartDraft()
    {
        if (!deckManager || !handManager || !draftContainer || !cardChoicePrefab) return;

        _selectedCount = 0;
        UpdateCounter();

        ShowDraftUI();
        ClearChoices();

        var sample = deckManager.GetRandomSample(choicesCount, allowDuplicates);
        foreach (var prefabReal in sample)
            _choices.Add(CreateDraftCardUI(prefabReal));

        RefreshButtons();

        // Fuerza relayout por si tarda en refrescar
        var rt = draftContainer as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private DraftSelectableUI CreateDraftCardUI(GameObject prefabReal)
    {
        // Instancia el UI
        var uiGO = Instantiate(cardChoicePrefab, draftContainer);
        var ui = uiGO.GetComponent<DraftSelectableUI>();
        if (!ui) ui = uiGO.AddComponent<DraftSelectableUI>();

   
        var le = uiGO.GetComponent<LayoutElement>() ?? uiGO.AddComponent<LayoutElement>();
        if (le.preferredWidth <= 0) le.preferredWidth = 400;
        if (le.preferredHeight <= 0) le.preferredHeight = 680;

        
        var art = ExtractSprite(prefabReal);

      
        ui.Init(this, prefabReal, art);

        return ui;
    }

    private Sprite ExtractSprite(GameObject prefabReal)
    {
       

        // 1) PreviewData en cualquier hijo (tu caso: CardCanvas/CardImage)
        var data = prefabReal.GetComponentInChildren<CardPreviewData>(true);
        if (data != null && data.art != null) return data.art;

       

     
        return null;
    }


    // === Selección ===
    public void ToggleSelection(DraftSelectableUI view)
    {
        if (!view) return;
        if (!view.IsSelected && _selectedCount >= picksAllowed) return;

        view.SetSelected(!view.IsSelected);
        _selectedCount += view.IsSelected ? 1 : -1;
        _selectedCount = Mathf.Clamp(_selectedCount, 0, picksAllowed);

        UpdateCounter();
        RefreshButtons();
    }

    private void ConfirmSelection()
    {
        if (_selectedCount != picksAllowed) return;

        foreach (var c in _choices)
        {
            if (!c.IsSelected) continue;
            // Añade a la mano el PREFAB REAL (no el UI)
            handManager.AddCardToHand(c.PrefabRef);
        }

        ClearChoices();
        HideDraftUI();
        handManager.DeselectAll();
    }

    private void Reroll() => StartDraft();

    // === Helpers UI ===
    private void ShowDraftUI()
    {
        if (overlayPanel) overlayPanel.SetActive(true);
        if (draftContainer && !draftContainer.gameObject.activeSelf)
            draftContainer.gameObject.SetActive(true);
    }

    private void HideDraftUI()
    {
        if (overlayPanel) overlayPanel.SetActive(false);
        if (draftContainer && draftContainer.gameObject.activeSelf)
            draftContainer.gameObject.SetActive(false);
    }

    private void ClearChoices()
    {
        if (draftContainer)
            for (int i = draftContainer.childCount - 1; i >= 0; i--)
                Destroy(draftContainer.GetChild(i).gameObject);

        _choices.Clear();
        _selectedCount = 0;
        UpdateCounter();
    }

    private void RefreshButtons()
    {
        if (confirmButton) confirmButton.interactable = (_selectedCount == picksAllowed);
        if (rerollButton) rerollButton.interactable = true;
    }

    private void UpdateCounter()
    {
        if (counterText) counterText.text = $"{_selectedCount}/{picksAllowed}";
    }
}
