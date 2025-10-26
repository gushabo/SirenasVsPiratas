using UnityEngine;
using UnityEngine.UI;

public class DraftSelectableUI : MonoBehaviour
{
    [Header("UI")]
    public Image artImage;
    public GameObject checkMark;

    [HideInInspector] public GameObject PrefabRef; // el prefab REAL de tu mazo

    private DraftPicker _owner;
    private bool _isSelected;
    public bool IsSelected => _isSelected;

    public void Init(DraftPicker owner, GameObject prefabRef, Sprite art)
    {
        _owner = owner;
        PrefabRef = prefabRef;

        if (artImage != null)
        {
            artImage.sprite = art;
            artImage.preserveAspect = true;
            var c = artImage.color; c.a = 1f; artImage.color = c;  // por si quedó transparente
            artImage.enabled = (art != null);
        }

        SetSelected(false);

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => _owner.ToggleSelection(this));
        }
    }


    public void SetSelected(bool value)
    {
        _isSelected = value;
        if (checkMark != null) checkMark.SetActive(value);
    }
}
