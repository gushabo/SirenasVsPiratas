
using UnityEngine;

public static class ProgressManager
{
    private const string KEY = "highestLevelUnlocked";

    public static int GetHighestUnlocked()
    {
        int v = PlayerPrefs.GetInt(KEY, 1);
        if (v < 1) v = 1;
        return v;
    }

    public static void SetHighestUnlocked(int levelNumber)
    {
        int current = GetHighestUnlocked();
        if (levelNumber > current)
        {
            PlayerPrefs.SetInt(KEY, levelNumber);
            PlayerPrefs.Save();
        }
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}
