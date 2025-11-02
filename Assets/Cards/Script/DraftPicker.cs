using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DraftPicker : MonoBehaviour
{
    public event Action OnConfirmed;   // <- avisa cuando confirman

    [Header("Referencias")]
    public DeckManager deckManager;
    public HandManager handManager;

    [Header("UI Draft")]
    public Transform draftContainer;
    public GameObject cardChoicePrefab;
    public Button confirmButton;
    public Button rerollButton;
    public GameObject overlayPanel;
    public TextMeshProUGUI counterText;

    [Header("Parámetros")]
    public int choicesCount = 5;
    public int picksAllowed = 3;
    public bool allowDuplicates = false;

    [Header("Tamaño de carta")]
    public Vector2 cardSize = new Vector2(370f, 800f);  // <-- puedes editar desde el Inspector
    public float artPadding = 24f;

    [Header("UIEliminar")] // <-- margen interior para el arte
    public GameObject pausaGO;

    public GameObject salirGO;

    private readonly List<DraftSelectableUI> _choices = new();
    private int _selectedCount;

    // ===== Reroll 1 vez por sesión =====
    private bool _rerollUsed = false;

    void Awake()
    {
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmSelection);
        if (rerollButton) rerollButton.onClick.AddListener(Reroll);
        HideDraftUI();
    }

    public bool IsOpen() => overlayPanel && overlayPanel.activeSelf;

    public void StartDraft()
    {
        // ¿La UI estaba cerrada? Si sí, viene un draft "nuevo" → resetear flag
        bool openingNow = !IsOpen();
        if (openingNow)
        {
            ResetDraftSession();                  // _rerollUsed = false
            SetRerollButtonState(true);           // prende el botón visualmente
        }

        pausaGO.SetActive(false);
        salirGO.SetActive(false);
        if (!deckManager || !handManager || !draftContainer || !cardChoicePrefab) return;

        _selectedCount = 0;
        UpdateCounter();

        ShowDraftUI();
        ClearChoices();

        var sample = deckManager.GetRandomSample(choicesCount, allowDuplicates);
        foreach (var prefabReal in sample)
            _choices.Add(CreateDraftCardUI(prefabReal));

        RefreshButtons();                         // respeta _rerollUsed para interactable

        var rt = draftContainer as RectTransform;
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }


    private DraftSelectableUI CreateDraftCardUI(GameObject prefabReal)
    {
        var uiGO = Instantiate(cardChoicePrefab, draftContainer);
        var ui = uiGO.GetComponent<DraftSelectableUI>();
        if (!ui) ui = uiGO.AddComponent<DraftSelectableUI>();

        float W = cardSize.x;
        float H = cardSize.y;

        // === Forzar tamaño del Layout ===
        var le = uiGO.GetComponent<LayoutElement>() ?? uiGO.AddComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = W;
        le.minHeight = le.preferredHeight = H;
        le.flexibleWidth = le.flexibleHeight = 0;

        // Desactiva ContentSizeFitter si existe
        var fitter = uiGO.GetComponent<ContentSizeFitter>();
        if (fitter) fitter.enabled = false;

        // Ajusta el RectTransform raíz
        var rt = uiGO.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(W, H);
        }

        // Forzar tamaño del fondo de la carta (Image del CardChoiceUI)
        var img = uiGO.GetComponent<Image>();
        if (img != null)
        {
            var imgRT = img.GetComponent<RectTransform>();
            if (imgRT) imgRT.sizeDelta = new Vector2(W, H);
        }

        // Asignar sprite principal
        var art = ExtractSprite(prefabReal);
        ui.Init(this, prefabReal, art);

        // === Escalar también el hijo "Art" ===
        var artRT = uiGO.transform.Find("Art")?.GetComponent<RectTransform>();
        if (artRT != null)
        {
            artRT.anchorMin = Vector2.zero;
            artRT.anchorMax = Vector2.one;
            artRT.pivot = new Vector2(0.5f, 0.5f);
            artRT.offsetMin = Vector2.zero;
            artRT.offsetMax = Vector2.zero;
        }

        var artImg = uiGO.transform.Find("Art")?.GetComponent<Image>();
        if (artImg != null)
        {
            var arf = artImg.GetComponent<AspectRatioFitter>();
            if (arf) Destroy(arf); // quitarlo

            artImg.type = Image.Type.Simple;
            artImg.preserveAspect = false; // se estira exacto al tamaño del padre
        }

        return ui;
    }

    private Sprite ExtractSprite(GameObject prefabReal)
    {
        var data = prefabReal.GetComponentInChildren<CardPreviewData>(true);
        if (data != null && data.art != null) return data.art;
        return null;
    }

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
            handManager.AddCardToHand(c.PrefabRef);
        }

        ClearChoices();
        HideDraftUI();
        pausaGO.SetActive(false);
        salirGO.SetActive(false);
        handManager.DeselectAll();

        OnConfirmed?.Invoke();  // <- ¡dispara el callback!
    }

    public IEnumerator ShowAndWait()
    {
        bool done = false;
        void Handler() { done = true; OnConfirmed -= Handler; }

        OnConfirmed += Handler;

        ResetDraftSession();   // <- resetea el derecho a 1 reroll por sesión
        StartDraft();

        while (!done)
            yield return null;
    }

    // ====== Reroll (solo 1 vez) ======
    private void Reroll()
    {
        if (_rerollUsed) return;

        _rerollUsed = true;

     
        SetRerollButtonState(false);

        StartDraft();     
      
    }


    private void ResetDraftSession()
    {
        _rerollUsed = false;
    }

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

    public void HideDraft()
    {
        ClearChoices();
        HideDraftUI();
        handManager.DeselectAll();
    }

    private void RefreshButtons()
    {
        if (confirmButton) confirmButton.interactable = (_selectedCount == picksAllowed);
        if (rerollButton) rerollButton.interactable = !_rerollUsed; // habilitado solo si no se usó
    }

    private void UpdateCounter()
    {
        if (counterText) counterText.text = $"{_selectedCount}/{picksAllowed}";
    }
    
    
    private void SetRerollButtonState(bool enabled)
    {
        if (!rerollButton) return;

        rerollButton.interactable = enabled;

        // (Opcional) feedback visual
        var txt = rerollButton.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = enabled ? "Reroll" : "Reroll usado";

      
       
    }

}
