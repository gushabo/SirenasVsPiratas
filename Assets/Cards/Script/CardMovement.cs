using UnityEngine;
using UnityEngine.EventSystems;

public class CardHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Highlight Settings")]
    public GameObject glowEffect;
    public float selectScale = 1.1f;

    [Header("Card")]
    [SerializeField] private GameObject cardRoot;   // debe ser el GameObject que se agregó a cardsInHand
    public GameObject CardRoot => cardRoot;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private bool isSelected = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        if (glowEffect != null) glowEffect.SetActive(false);
        // ¡OJO! ya no uses transform.root aquí, lo vamos a bindear desde HandManager
    }

    // NUEVO: lo llamará HandManager al instanciar la carta
    public void BindRoot(GameObject root)
    {
        cardRoot = root;
    }

    public void SetSelected(bool value)
    {
        isSelected = value;
        if (isSelected) HandleHoverState();
        else HandleNormalState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected) HandleHoverState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected) HandleNormalState();
    }

    private void HandleHoverState()
    {
        if (glowEffect != null) glowEffect.SetActive(true);
        rectTransform.localScale = originalScale * selectScale;
    }

    private void HandleNormalState()
    {
        if (glowEffect != null) glowEffect.SetActive(false);
        rectTransform.localScale = originalScale;
    }
}
