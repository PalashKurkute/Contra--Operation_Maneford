using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

// Moves the game from scene to scene in the order set by the
// GameFlow asset. Fades through black between scenes and shows a
// "LEVEL n" card whenever a new level begins.
//
// It creates itself (from Resources/SceneFlow) before the first
// scene loads, so every scene can call SceneFlow.Next(), even
// when you press Play on that scene directly.
public class SceneFlow : MonoBehaviour
{
    public static SceneFlow Instance { get; private set; }

    public GameFlow flow;

    [Header("Screen")]
    public CanvasGroup fade;
    public CanvasGroup levelCard;
    public TMP_Text levelNumberText;
    public TMP_Text levelTitleText;

    [Header("Timing")]
    public float fadeTime = 0.4f;
    public float levelCardTime = 2.5f;

    // Highest level reached, which unlocks it in the menu
    const string LevelReachedKey = "LevelReached";

    private bool transitioning;

    // =========================
    // STARTUP
    // =========================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    static void CreateInstance()
    {
        Instance = null;

        SceneFlow prefab = Resources.Load<SceneFlow>("SceneFlow");

        if (prefab == null)
        {
            Debug.LogError(
                "❌ Resources/SceneFlow prefab is missing!"
            );

            return;
        }

        Instance = Instantiate(prefab);
        Instance.name = "SceneFlow";

        DontDestroyOnLoad(Instance.gameObject);
    }

    void Start()
    {
        // Fade into whichever scene the game started in, with its
        // level card if that scene is where its level begins
        int index = CurrentIndex();
        int level = flow.LevelOf(index);

        bool showCard = level > 0 &&
            index == flow.FirstStepOfLevel(level);

        StartCoroutine(Reveal(level, showCard));
    }

    void Update()
    {
        // Debug: F9 skips to the next scene (Editor and
        // development builds only)
        if (Debug.isDebugBuild &&
            Keyboard.current != null &&
            Keyboard.current.f9Key.wasPressedThisFrame)
        {
            Debug.Log("⏭️ Skipping to the next scene (F9)");

            Next();
        }
    }

    // =========================
    // PUBLIC API
    // =========================

    // True while the screen fades or shows a level card.
    // Scenes should ignore input until it is over.
    public static bool IsTransitioning
    {
        get { return Instance != null && Instance.transitioning; }
    }

    // This scene's entry in the flow (null in the main menu)
    public static GameFlow.Step CurrentStep
    {
        get
        {
            if (Instance == null)
                return null;

            int index = Instance.CurrentIndex();

            return index >= 0 ? Instance.flow.steps[index] : null;
        }
    }

    // "LEVEL 1 - THE FOREST", "PROLOGUE" or "ENDING"
    public static string CurrentSectionName
    {
        get
        {
            if (Instance == null)
                return "";

            return Instance.flow.SectionName(Instance.CurrentIndex());
        }
    }

    // Level of this scene (0 = prologue, ending or menu)
    public static int CurrentLevel
    {
        get
        {
            if (Instance == null)
                return 0;

            return Instance.flow.LevelOf(Instance.CurrentIndex());
        }
    }

    public static int LevelCount
    {
        get { return Instance != null ? Instance.flow.levels.Length : 0; }
    }

    // Highest level the player has reached so far
    public static int LevelReached
    {
        get { return PlayerPrefs.GetInt(LevelReachedKey, 1); }
    }

    public static string LevelTitle(int level)
    {
        return Instance != null ? Instance.flow.LevelTitle(level) : "";
    }

    // Go to the scene after this one
    public static void Next()
    {
        if (Instance == null)
            return;

        Instance.GoTo(Instance.CurrentIndex() + 1);
    }

    // Play this scene again from the start
    public static void Restart()
    {
        if (Instance == null)
        {
            Time.timeScale = 1f;

            SceneManager.LoadScene(
                SceneManager.GetActiveScene().buildIndex
            );

            return;
        }

        Instance.Load(SceneManager.GetActiveScene().name);
    }

    public static void StartGame()
    {
        if (Instance == null)
            return;

        Instance.GoTo(0);
    }

    public static void StartLevel(int level)
    {
        if (Instance == null)
            return;

        Instance.GoTo(Instance.flow.FirstStepOfLevel(level));
    }

    public static void LoadMainMenu()
    {
        if (Instance == null)
            return;

        Instance.Load(Instance.flow.mainMenuScene);
    }

    // =========================
    // LOADING
    // =========================

    int CurrentIndex()
    {
        return flow.IndexOf(SceneManager.GetActiveScene().name);
    }

    // Load a step of the flow; past the end means the main menu
    void GoTo(int index)
    {
        if (index >= 0 && index < flow.steps.Length)
        {
            Load(flow.steps[index].sceneName);
        }
        else
        {
            Load(flow.mainMenuScene);
        }
    }

    void Load(string sceneName)
    {
        if (transitioning)
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "❌ Scene '" + sceneName +
                "' is not in the Build Settings!"
            );

            return;
        }

        StartCoroutine(Transition(sceneName));
    }

    IEnumerator Transition(string sceneName)
    {
        transitioning = true;

        int fromLevel = CurrentLevel;

        yield return FadeGroup(fade, 1f);

        yield return SceneManager.LoadSceneAsync(sceneName);

        int level = CurrentLevel;

        // A level card only when we enter a different level
        // (not when retrying a stage)
        yield return Reveal(level, level > 0 && level != fromLevel);
    }

    // Starts from a black screen, shows the level card if
    // asked, then fades the scene in
    IEnumerator Reveal(int level, bool showCard)
    {
        transitioning = true;
        fade.alpha = 1f;

        if (showCard)
        {
            // Keep the new scene still behind the card
            Time.timeScale = 0f;

            if (level > LevelReached)
            {
                PlayerPrefs.SetInt(LevelReachedKey, level);
                PlayerPrefs.Save();
            }

            levelNumberText.text = "LEVEL " + level;
            levelTitleText.text = flow.LevelTitle(level);

            yield return FadeGroup(levelCard, 1f);

            yield return new WaitForSecondsRealtime(levelCardTime);

            yield return FadeGroup(levelCard, 0f);
        }

        Time.timeScale = 1f;

        yield return FadeGroup(fade, 0f);

        transitioning = false;
    }

    // Fades in real time, so it works while the game is paused
    IEnumerator FadeGroup(CanvasGroup group, float target)
    {
        float start = group.alpha;
        float time = 0f;

        while (time < fadeTime)
        {
            // Cap the step so a loading hitch doesn't skip the fade
            time += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

            group.alpha = Mathf.Lerp(start, target, time / fadeTime);

            yield return null;
        }

        group.alpha = target;
    }
}
