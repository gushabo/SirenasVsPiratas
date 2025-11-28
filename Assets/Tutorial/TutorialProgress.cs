// Assets/Scripts/Tutorial/TutorialProgress.cs
using UnityEngine;

public static class TutorialProgress
{
    // Paso actual y flag de completado
    public const string PREF_TUTORIAL_STEP     = "tut_object_index";
    public const string PREF_TUTORIAL_DONE     = "tut_done";

    // NUEVO: flag de sesión en curso (para detectar abandono)
    public const string PREF_TUTORIAL_INPROG   = "tut_in_progress";

    // Contadores
    public const string PREF_TOWER_COUNT       = "tut_count_tower";
    public const string PREF_MINE_COUNT        = "tut_count_mine";
    public const string PREF_UPG_COUNT         = "tut_count_upgrade";

    // ==== Get/Set básicos ====
    public static int  GetStep()                 => PlayerPrefs.GetInt(PREF_TUTORIAL_STEP, 0);
    public static void SetStep(int step)         { PlayerPrefs.SetInt(PREF_TUTORIAL_STEP, step); PlayerPrefs.Save(); }

    public static bool IsCompleted()             => PlayerPrefs.GetInt(PREF_TUTORIAL_DONE, 0) == 1;
    public static void SetCompleted()            { PlayerPrefs.SetInt(PREF_TUTORIAL_DONE, 1); PlayerPrefs.Save(); }

    public static void Increment(BuildKind kind)
    {
        var key = KeyFor(kind);
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);
        PlayerPrefs.Save();
    }

    public static int GetCount(BuildKind kind)   => PlayerPrefs.GetInt(KeyFor(kind), 0);

    // ==== Control de sesión (reinicio si quedó a medias) ====
    public static void BeginSession()
    {
        PlayerPrefs.SetInt(PREF_TUTORIAL_INPROG, 1);
        PlayerPrefs.Save();
    }

    // Llamar cuando el tutorial se COMPLETA
    public static void EndSessionCompleted()
    {
        SetCompleted();
        PlayerPrefs.SetInt(PREF_TUTORIAL_INPROG, 0);
        PlayerPrefs.Save();
    }

    public static bool WasInProgress() => PlayerPrefs.GetInt(PREF_TUTORIAL_INPROG, 0) == 1;

    // Reinicia sólo si detecta que había una sesión a medias y NO estaba completado
    public static void ResetIfAbandoned()
    {
        if (WasInProgress() && !IsCompleted())
        {
            ResetProgressOnly(true); // reinicia paso y contadores
        }
    }

    // ==== Resets ====
    // Reset TOTAL (incluye 'done')
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(PREF_TUTORIAL_STEP);
        PlayerPrefs.DeleteKey(PREF_TUTORIAL_DONE);
        PlayerPrefs.DeleteKey(PREF_TUTORIAL_INPROG);
        PlayerPrefs.DeleteKey(PREF_TOWER_COUNT);
        PlayerPrefs.DeleteKey(PREF_MINE_COUNT);
        PlayerPrefs.DeleteKey(PREF_UPG_COUNT);
        PlayerPrefs.Save();
    }

    // Reset para empezar de cero SIN tocar el flag de completado (útil si quieres forzar reintento)
    public static void ResetProgressOnly(bool alsoCounts)
    {
        PlayerPrefs.DeleteKey(PREF_TUTORIAL_STEP);
        PlayerPrefs.SetInt(PREF_TUTORIAL_INPROG, 0);
        if (alsoCounts)
        {
            PlayerPrefs.DeleteKey(PREF_TOWER_COUNT);
            PlayerPrefs.DeleteKey(PREF_MINE_COUNT);
            PlayerPrefs.DeleteKey(PREF_UPG_COUNT);
        }
        PlayerPrefs.Save();
    }

    private static string KeyFor(BuildKind k) =>
        k == BuildKind.Tower ? PREF_TOWER_COUNT :
        k == BuildKind.Mine  ? PREF_MINE_COUNT  :
                               PREF_UPG_COUNT;
}
