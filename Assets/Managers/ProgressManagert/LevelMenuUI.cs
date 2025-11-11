using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelMenuUI : MonoBehaviour
{
    [System.Serializable]
    public class LevelButton
    {
        public Button button;
        public GameObject lockIcon; // opcional
        public int levelNumber = 1; // 1-based
    }

    [Header("Configuración")]
   
    public LevelButton[] levelButtons;

    public GameObject levelsGO;

    void OnEnable()
    {
        int highest = ProgressManager.GetHighestUnlocked();

        foreach (var lb in levelButtons)
        {
            bool unlocked = lb.levelNumber <= highest;
            if (lb.button != null)
            {
                lb.button.interactable = unlocked;
                lb.button.onClick.RemoveAllListeners();
                int ln = lb.levelNumber;
                lb.button.onClick.AddListener(() => PlayLevel(ln));
            }
            if (lb.lockIcon != null)
                lb.lockIcon.SetActive(!unlocked);
        }
    }


    private void Start()
    {
        levelsGO.SetActive(false);
    }
    private void PlayLevel(int levelNumber)
    {
      
        PlayerPrefs.SetInt("selectedLevelToPlay", levelNumber);
        PlayerPrefs.Save();

        SceneTransition.CargarConPantallaNegra("Enemy");
    }


    public void ResetProgress()
    {
        ProgressManager.ResetProgress();
        OnEnable(); 
    }
}
