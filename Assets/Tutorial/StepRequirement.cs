// Assets/Scripts/Tutorial/StepRequirement.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StepRequirement : MonoBehaviour
{

    public DeckManager deckManager;
    public HandManager handManager;
    public enum Mode
    {
        None,            // No requiere nada: el botón Next está habilitado
        RequireTower,    // Colocar 1 torreta desde que entré a este paso
        RequireMine,     // Colocar 1 mina desde que entré a este paso
        RequireUpgrade,  // Aplicar 1 mejora desde que entré a este paso
        RequireCustom    // Requiere N de un tipo desde que entré a este paso
    }

    [Header("Requisito")]
    public Mode mode = Mode.None;
    public BuildKind customKind = BuildKind.Tower;
    public int customCount = 1;
    private bool alreadycard;
    
    

    [Header("UI")]
    public Button nextButton;                     // botón “Siguiente” de este paso
    public bool hideNextWhileWaiting = true;      // ocultar mientras no se cumple
    public bool disableNextWhileWaiting = true;   // o solo deshabilitar
    public TextMeshProUGUI progressText;          // opcional: “(0/1)”

    [Header("Avance")]
    public bool autoAdvanceWhenSatisfied = true;  // avanzar al siguiente al cumplirse

    private TutorialObjectSequence seq;
    private int baseTower, baseMine, baseUpg;
    private bool satisfied;

    void OnEnable()
    {
        seq = GetComponentInParent<TutorialObjectSequence>();

        baseTower = TutorialProgress.GetCount(BuildKind.Tower);
        baseMine  = TutorialProgress.GetCount(BuildKind.Mine);
        baseUpg   = TutorialProgress.GetCount(BuildKind.Upgrade);

        satisfied = (mode == Mode.None);
        ApplyNextButtonState();

       
        UpdateProgressUI(0, Needed());
    }

    void Update()
    {
        if (satisfied || mode == Mode.None) return;

        int towers = TutorialProgress.GetCount(BuildKind.Tower)   - baseTower;
        int mines  = TutorialProgress.GetCount(BuildKind.Mine)    - baseMine;
        int upgs   = TutorialProgress.GetCount(BuildKind.Upgrade) - baseUpg;

        int have = 0, need = 1;

        switch (mode)
        {
            case Mode.RequireTower:   have = towers; need = 1;

                if(!alreadycard)
                {
                    deckManager.DrawCard(handManager);

                }
                alreadycard = true;
                break;
            case Mode.RequireMine:    have = mines;  need = 1;
                if (!alreadycard)
                {
                    deckManager.DrawCard(handManager);

                }
                alreadycard = true;
                break; ;
            case Mode.RequireUpgrade: have = upgs;   need = 1;
                if (!alreadycard)
                {
                    deckManager.DrawCard(handManager);

                }
                alreadycard = true;
                break; ;
            case Mode.RequireCustom:
                have = customKind == BuildKind.Tower ? towers :
                       customKind == BuildKind.Mine  ? mines  :
                                                       upgs;
                need = Mathf.Max(1, customCount);
                break;
        }

        UpdateProgressUI(have, need);

        if (have >= need)
        {
            satisfied = true;
            ApplyNextButtonState();

            if (autoAdvanceWhenSatisfied && seq != null)
                seq.Next();
        }
    }

    private void ApplyNextButtonState()
    {
        if (!nextButton) return;

        if (!satisfied)
        {
            if (hideNextWhileWaiting) nextButton.gameObject.SetActive(false);
            
        }
        else
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    private int Needed()
    {
        if (mode == Mode.RequireCustom) return Mathf.Max(1, customCount);
        return (mode == Mode.None) ? 0 : 1;
    }

    private void UpdateProgressUI(int have, int need)
    {
        if (!progressText) return;
        if (need <= 0) { progressText.text = ""; return; }
        progressText.text = $"({Mathf.Clamp(have,0,need)}/{need})";
    }

    // Si prefieres avanzar por botón aunque ya esté satisfecho:
    public void OnClickNext()
    {
        if (seq != null) seq.Next();
    }
}
