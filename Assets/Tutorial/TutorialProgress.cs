// Assets/Scripts/Tutorial/TutorialProgress.cs
using UnityEngine;

public static class TutorialProgress
{
    // Paso actual (objeto activo) y flag de completado
    public const string PREF_TUTORIAL_STEP     = "tut_object_index";
    public const string PREF_TUTORIAL_DONE     = "tut_done";

    // Contadores (para StepRequirement si lo usas)
    public const string PREF_TOWER_COUNT       = "tut_count_tower";
    public const string PREF_MINE_COUNT        = "tut_count_mine";
    public const string PREF_UPG_COUNT         = "tut_count_upgrade";

    public static int  GetStep()                 => PlayerPrefs.GetInt(PREF_TUTORIAL_STEP, 0);
    public static void SetStep(int step)         { PlayerPrefs.SetInt(PREF_TUTORIAL_STEP, step); PlayerPrefs.Save(); }

    public static bool IsCompleted()             => PlayerPrefs.GetInt(PREF_TUTORIAL_DONE, 0) == 1;
    public static void SetCompleted()            { PlayerPrefs.SetInt(PREF_TUTORIAL_DONE, 1); PlayerPrefs.Save(); }

    public static void Increment(BuildKind kind)
    {
        var key = KeyFor(kind);
        var v = PlayerPrefs.GetInt(key, 0) + 1;
        PlayerPrefs.SetInt(key, v);
        PlayerPrefs.Save();
    }

    public static int GetCount(BuildKind kind)   => PlayerPrefs.GetInt(KeyFor(kind), 0);

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(PREF_TUTORIAL_STEP);
        PlayerPrefs.DeleteKey(PREF_TUTORIAL_DONE);
        PlayerPrefs.DeleteKey(PREF_TOWER_COUNT);
        PlayerPrefs.DeleteKey(PREF_MINE_COUNT);
        PlayerPrefs.DeleteKey(PREF_UPG_COUNT);
        PlayerPrefs.Save();
    }

    private static string KeyFor(BuildKind k) =>
        k == BuildKind.Tower ? PREF_TOWER_COUNT :
        k == BuildKind.Mine  ? PREF_MINE_COUNT  :
        PREF_UPG_COUNT;
}