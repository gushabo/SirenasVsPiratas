using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    // ----- SingleTon ---------
    #region Singleton
    public static LevelManager instance { get; private set; }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    public static LevelManager GetInstance() => instance;

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (gm != null) gm.onChangeGameState -= OnChangeGameStateCallback;

        // Por si nos suscribimos en algún flujo/evento del placer en otro momento:
        // (no hace daño si no existe)
        var placer = HexGridCardPlacer.Instance;
        if (placer != null)
        {
            // Si tu versión del placer expone un evento, aquí lo desuscribes.
            // placer.OnFirstTowerPlaced -= HandleFirstTowerPlaced_StartRound;
        }
    }
    #endregion
    // ------ Fin del singleton  ---------

    private const string PREF_FIRST_TOWER_PLACED = "first_tower_placed"; // 0 = nunca; 1 = ya colocó alguna vez

    [Header("Draft")]
    [SerializeField] private DraftPicker draftPicker;   // <- arrástralo en el Inspector

    [Header("Jerarquía")]
    [SerializeField] private Transform levelsRoot;   // padre "Levels"

    [Header("Delays (segundos)")]
    [SerializeField] private float delayBetweenRounds = 2f;

    // Lista bidimensional para guardar rondas y niveles
    private readonly List<List<EnemySpawner>> levelsEnemySpawner = new();
    private readonly List<List<GameObject>> levelsGO = new();

    // Rondas y niveles
    private int levelIndex;
    private int roundIndex;

    private int maxLevels = 3;

    public int enemiesLeft;

    // Spawner
    private EnemySpawner currentSpawner;

    // Timers
    private float timer;      // para delays
    private bool waveDone;    // set por el evento del spawner

    // Game Manager
    private GameManager gm;

    public bool isPaused;

    void Start()
    {
        gm = GameManager.GetInstance();
        gm.onChangeGameState += OnChangeGameStateCallback;
        if (gm.gameState == GameState.Pause)
            isPaused = true;

        // Leer el nivel elegido desde el menú (1-based)
        int selected = PlayerPrefs.GetInt("selectedLevelToPlay", 1);

        // Construir los niveles y fijar índices (sin iniciar la ronda todavía)
        StartFromLevel(selected);   // <-- ya no llama StartRound por dentro

        bool alreadyPlacedOnce = PlayerPrefs.GetInt(PREF_FIRST_TOWER_PLACED, 0) == 1;

        if (alreadyPlacedOnce)
        {
            // Jugador ya colocó alguna torreta en la vida del juego → arranca normal
            StartRound();
        }
    }

    public void OnChangeGameStateCallback(GameState newState)
    {
        isPaused = newState != GameState.Play;
    }

    // Llama esto para activar la ronda actual (nivelIndex/roundIndex)
    public void StartRound()
    {
        if (levelIndex < 0 || levelIndex >= levelsGO.Count) { Debug.LogError("levelIndex fuera de rango"); return; }
        if (roundIndex < 0 || roundIndex >= levelsGO[levelIndex].Count) { Debug.LogError("roundIndex fuera de rango"); return; }

        levelsGO[levelIndex][roundIndex].SetActive(true);
        UiManager.GetInstance().UpdateRoundLevelText(roundIndex, levelIndex);
    }

    public void CheckForEnemies()
    {
        if (gm.Lose) { gm.GameOver(); return; }

        if (enemiesLeft == 0)
        {
            StartCoroutine(CambioDeRonda());
        }
    }

    public IEnumerator CambioDeRonda()
    {
        //UiManager.GetInstance().CambioDeRonda(roundIndex);
        if (draftPicker != null)
            if(levelIndex < maxLevels && roundIndex != 2)
                yield return draftPicker.ShowAndWait();
        
        float counter = 0f;
        while (counter < delayBetweenRounds)
        {
            if (!isPaused) counter += Time.deltaTime;
            yield return null;
        }

        //UiManager.GetInstance().ApagarCambioRondas();

        if (roundIndex >= levelsGO[levelIndex].Count - 1)
        {
            int nextLevelNumber = levelIndex + 2; // desbloquea el siguiente
            ProgressManager.SetHighestUnlocked(nextLevelNumber);

            roundIndex = 0;
            levelIndex++;

            if (levelIndex >= maxLevels)
            {
                gm.Win();
                ProgressManager.SetHighestUnlocked(maxLevels);
            }
            else
            {
                UiManager.GetInstance().CambiarDeNivel();
                // Aquí podrías llamar StartRound() si quieres iniciar de inmediato la primera ronda del siguiente nivel
                // StartRound();
            }
        }
        else if (!gm.Lose)
        {
            roundIndex++;
            StartRound();
        }
    }

    public void BuildLevels()
    {
        levelsEnemySpawner.Clear();
        levelsGO.Clear();

        if (levelsRoot == null) return;

        for (int i = 0; i < levelsRoot.childCount; i++)
        {
            Transform levelT = levelsRoot.GetChild(i);

            var spawnerList = new List<EnemySpawner>();
            var goList = new List<GameObject>();

            // Sacar todos los hijos del item del transform
            var children = new List<Transform>(levelT.childCount);
            for (int j = 0; j < levelT.childCount; j++)
                children.Add(levelT.GetChild(j));

            children.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // El for each para ir rellenando las listas
            foreach (var child in children)
            {
                var go = child.gameObject;

                // Filtrado de items con el componente del spawner
                if (!go.TryGetComponent<EnemySpawner>(out var sp)) continue;

                go.SetActive(false);

                spawnerList.Add(sp);
                goList.Add(go);
            }

            if (spawnerList.Count > 0)
            {
                levelsEnemySpawner.Add(spawnerList);
                levelsGO.Add(goList);
            }
        }

        // las 2 listas deben de tener el mismo tamaño
        if (levelsEnemySpawner.Count != levelsGO.Count)
        {
            Debug.LogWarning($"Desfase en niveles: EnemySpawner={levelsEnemySpawner.Count} vs GO={levelsGO.Count}");
        }

        maxLevels = levelsGO.Count;
    }

    // Imprime TODA la estructura con índices, nombres y rutas en jerarquía
    [ContextMenu("Print Levels")]
    public void PrintLevels()
    {
        if (levelsGO.Count == 0)
        {
            Debug.LogWarning("Levels vacío. ¿Llamaste BuildLevels() y asignaste levelsRoot?");
            return;
        }

        var sb = new StringBuilder(512);
        sb.AppendLine("=== LEVELS DUMP ===");

        for (int lvl = 0; lvl < levelsGO.Count; lvl++)
        {
            var list = levelsGO[lvl];
            sb.AppendLine($"Level {lvl}  (Rounds: {list.Count})");

            for (int r = 0; r < list.Count; r++)
            {
                var sp = list[r];
                string path = GetHierarchyPath(sp.transform);
                sb.AppendLine($"  [{lvl}:{r}]  {sp.name}  | Active:{sp.gameObject.activeSelf}  | Path:{path}");
            }
        }

        sb.AppendLine("====================");
        Debug.Log(sb.ToString());
    }

    // Util para ver la ruta completa en jerarquía (Levels/Level_1/Spawner_Round1)
    private static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "<null>";
        var stack = new Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }

    // ===== Inicio controlado por "primera colocación" =====
    public void StartFromLevel(int levelNumber)
    {
        levelIndex = Mathf.Clamp(levelNumber - 1, 0, Mathf.Max(0, maxLevels - 1));

        // Asegura que la estructura está cargada
        if (levelsGO.Count == 0) BuildLevels();

        roundIndex = 0;
    }
    
    private void HandleFirstTowerPlaced_StartRound()
    {
        StartRound();
    }
}
