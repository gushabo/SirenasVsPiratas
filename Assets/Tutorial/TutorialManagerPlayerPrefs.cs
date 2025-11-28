// Assets/Scripts/Tutorial/TutorialManagerPlayerPrefs.cs
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
        public bool disableNextButtonWhileWaiting = false;
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
            // Llegaste al final
            if (panel) panel.SetActive(false);
            TutorialProgress.SetStep(newIndex);
            // Marca completado si quieres cerrar flujo aquí:
            // TutorialProgress.SetCompleted();
            // LevelManager.GetInstance()?.StartRound();
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

        // Botón Next visible/activo según configuración
        if (nextButton)
        {
            bool show = (s.mode == StepMode.ClickToContinue) || !s.hideNextButtonWhileWaiting;
            nextButton.gameObject.SetActive(show);
            nextButton.interactable = !s.disableNextButtonWhileWaiting || (s.mode == StepMode.ClickToContinue);
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
            int have = 0, need = 1;

            switch (s.mode)
            {
                case StepMode.RequireTower:   have = towers; need = 1; break;
                case StepMode.RequireMine:    have = mines;  need = 1; break;
                case StepMode.RequireUpgrade: have = upgs;   need = 1; break;
                case StepMode.RequireCustomCount:
                    have = s.requiredKind == BuildKind.Tower ? towers :
                           s.requiredKind == BuildKind.Mine  ? mines  :
                                                                upgs;
                    need = Mathf.Max(1, s.requiredCount);
                    break;
            }

            ok = have >= need;

            // Feedback opcional en el mismo TMP
            if (dialogText && s.mode == StepMode.RequireCustomCount)
                dialogText.text = $"{s.text}\n\n({Mathf.Clamp(have,0,need)}/{need})";

            if (ok)
            {
                waiting = false;

                if (nextButton)
                {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                }

                if (s.autoAdvanceWhenSatisfied)
                    GoTo(index + 1);
            }

            yield return null; // cada frame; puedes hacerlo menos frecuente con WaitForSeconds(0.1f)
        }
    }
}
