using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManagerPlayerPrefs : MonoBehaviour
{
    public enum StepMode
    {
        ClickToContinue,   // avanza con botón
        RequireTower,      // requiere colocar 1 torre DESDE este paso
        RequireMine,       // requiere colocar 1 mina DESDE este paso
        RequireUpgrade,    // requiere aplicar 1 mejora DESDE este paso
        RequireCustomCount // requiere N acciones de un tipo DESDE este paso
    }

    [Serializable]
    public class Step
    {
        [TextArea] public string text;
        public StepMode mode = StepMode.ClickToContinue;

        [Header("Sólo para Custom")]
        public BuildKind requiredKind = BuildKind.Tower;
        public int requiredCount = 1;

        [Header("UI opcional al entrar")]
        public GameObject[] showOnEnter;
        public GameObject[] hideOnEnter;

        [Header("Botón Next")]
        public bool hideNextButtonWhileWaiting = true;
        public bool autoAdvanceWhenSatisfied = true;
    }

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI dialogText;
    public Button nextButton;

    [Header("Pasos")]
    public List<Step> steps = new();

    // Baselines al entrar al paso
    private int baseTower, baseMine, baseUpg;
    private int index;
    private bool waiting;

    void Start()
    {
        if (panel) panel.SetActive(true);

        // Cargar paso guardado (persistente)
        index = Mathf.Clamp(TutorialProgress.GetStep(), 0, Mathf.Max(0, steps.Count - 1));
        EnterStep(index);
    }

    public void OnClickNext()
    {
        if (waiting) return; // no saltar mientras se espera una acción
        GoTo(index + 1);
    }

    private void GoTo(int newIndex)
    {
        if (newIndex >= steps.Count)
        {
            if (panel) panel.SetActive(false);
            TutorialProgress.SetStep(newIndex);
            return;
        }
        index = newIndex;
        TutorialProgress.SetStep(index);
        EnterStep(index);
    }

    private void EnterStep(int i)
    {
        if (i < 0 || i >= steps.Count) return;
        var s = steps[i];

        // Texto
        if (dialogText) dialogText.text = s.text;

        // UI opcional
        if (s.showOnEnter != null) foreach (var go in s.showOnEnter) if (go) go.SetActive(true);
        if (s.hideOnEnter != null) foreach (var go in s.hideOnEnter) if (go) go.SetActive(false);

        // Capturar baselines
        baseTower = TutorialProgress.GetCount(BuildKind.Tower);
        baseMine  = TutorialProgress.GetCount(BuildKind.Mine);
        baseUpg   = TutorialProgress.GetCount(BuildKind.Upgrade);

        // Config
        waiting = s.mode != StepMode.ClickToContinue;

        // Botón Next visible/oculto
        if (nextButton)
        {
            bool show = (s.mode == StepMode.ClickToContinue) || !s.hideNextButtonWhileWaiting;
            nextButton.gameObject.SetActive(show);
        }

        // Si espera acción, arrancar chequeo periódico
        StopAllCoroutines();
        if (waiting) StartCoroutine(CheckRequirementLoop());
    }

    private System.Collections.IEnumerator CheckRequirementLoop()
    {
        var s = steps[index];
        while (waiting)
        {
            // progreso acumulado desde que entré al paso
            int towers = TutorialProgress.GetCount(BuildKind.Tower)   - baseTower;
            int mines  = TutorialProgress.GetCount(BuildKind.Mine)    - baseMine;
            int upgs   = TutorialProgress.GetCount(BuildKind.Upgrade) - baseUpg;

            bool ok = false;
            switch (s.mode)
            {
                case StepMode.RequireTower:   ok = towers >= 1; break;
                case StepMode.RequireMine:    ok = mines  >= 1; break;
                case StepMode.RequireUpgrade: ok = upgs   >= 1; break;
                case StepMode.RequireCustomCount:
                    int v = s.requiredKind == BuildKind.Tower ? towers :
                            s.requiredKind == BuildKind.Mine  ? mines  :
                                                                upgs;
                    ok = v >= Mathf.Max(1, s.requiredCount);
                    if (dialogText) dialogText.text = $"{s.text}\n\n({Mathf.Clamp(v,0,s.requiredCount)}/{s.requiredCount})";
                    break;
            }

            if (ok)
            {
                waiting = false;
                if (nextButton) nextButton.gameObject.SetActive(true);
                if (s.autoAdvanceWhenSatisfied) GoTo(index + 1);
            }

            yield return null; // revisa cada frame (puedes cambiar a WaitForSeconds(0.1f))
        }
    }
}
