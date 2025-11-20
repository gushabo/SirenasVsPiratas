// Assets/Scripts/Tutorial/TutorialObjectSequence.cs
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
                steps.Sort((a,b)=>string.Compare(a.name,b.name,System.StringComparison.Ordinal));
        }
    }

    void Start()
    {
        if (TutorialProgress.IsCompleted())
        {
          
            HideAll();
            if (hideRootWhenFinished) gameObject.SetActive(false);
            if (autoStartRoundIfCompleted) TryStartRound();
            return;
        }
       

        index = Mathf.Clamp(TutorialProgress.GetStep(), 0, Mathf.Max(0, steps.Count));

        if (!TutorialProgress.IsCompleted())
        {

            index = 0;
        }
        ApplyIndex();
    }

    public void Next()
    {
        if (TutorialProgress.IsCompleted()) return;
        index++;
        SaveAndApply();
    }

    public void Prev()
    {
        if (TutorialProgress.IsCompleted()) return;
        index = Mathf.Max(0, index - 1);
        SaveAndApply();
    }

    public void JumpTo(int newIndex)
    {
        if (TutorialProgress.IsCompleted()) return;
        index = Mathf.Clamp(newIndex, 0, steps.Count);
        SaveAndApply();
    }

    public void Finish() 
    {
        if (TutorialProgress.IsCompleted()) return;
        index = steps.Count;
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
       
        for (int i = 0; i < steps.Count; i++)
            if (steps[i]) steps[i].SetActive(i == index);

        if (index >= steps.Count)
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
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
