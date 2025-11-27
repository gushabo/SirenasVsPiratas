using System.Collections.Generic;
using UnityEngine;

public class TutorialObjectSequence : MonoBehaviour
{
    public List<GameObject> steps = new List<GameObject>();

    [Header("Comportamiento")]
    public bool autoCollectChildren = true;
    public bool sortByName = true;
    public bool hideRootWhenFinished = true;
    public bool autoStartRoundIfCompleted = true; // si ya estaba completado al entrar

    private int index;

    void Awake()
    {
        if (autoCollectChildren)
        {
            steps.Clear();
            for (int i = 0; i < transform.childCount; i++)
                steps.Add(transform.GetChild(i).gameObject);

            if (sortByName)
                steps.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        }
    }

    void Start()
    {
        int savedStep = TutorialProgress.GetStep();

        // Si el índice guardado ya está al final, no mostramos nada
        if (savedStep >= steps.Count)
        {
            HideAll();
            if (hideRootWhenFinished) gameObject.SetActive(false);
            if (autoStartRoundIfCompleted) TryStartRound();
            return;
        }

        // Clampeamos al rango válido de pasos visibles
        index = Mathf.Clamp(savedStep, 0, Mathf.Max(0, steps.Count - 1));
        ApplyIndex();
    }

    public void Next()
    {
        Debug.Log($"TutorialObjectSequence.Next() index={index}");
        index++;
        SaveAndApply();
    }

    public void Prev()
    {
        index = Mathf.Max(0, index - 1);
        SaveAndApply();
    }

    public void JumpTo(int newIndex)
    {
        // Permitimos llegar hasta steps.Count (estado "terminado")
        index = Mathf.Clamp(newIndex, 0, steps.Count);
        SaveAndApply();
    }

    public void Finish()
    {
        index = steps.Count;   // estado "terminado"
        SaveAndApply();
        CompleteTutorial();
    }

    private void SaveAndApply()
    {
        PlayerPrefs.SetInt(TutorialProgress.PREF_TUTORIAL_STEP, index);
        PlayerPrefs.Save();
        ApplyIndex();
    }

    private void ApplyIndex()
    {
        // Activa solo el paso actual mientras index esté dentro del rango
        for (int i = 0; i < steps.Count; i++)
            if (steps[i]) steps[i].SetActive(i == index);

        // Si index apunta más allá del último, damos por terminado
        if (index >= steps.Count)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        // Si quieres, sigue marcando como completado (por compatibilidad)
        TutorialProgress.SetCompleted();

        HideAll();
        if (hideRootWhenFinished) gameObject.SetActive(false);
        TryStartRound();
    }

    private void HideAll()
    {
        for (int i = 0; i < steps.Count; i++)
            if (steps[i]) steps[i].SetActive(false);
    }

    private void TryStartRound()
    {
        var lm = LevelManager.GetInstance();
        if (lm != null) lm.StartRound();
    }
}
