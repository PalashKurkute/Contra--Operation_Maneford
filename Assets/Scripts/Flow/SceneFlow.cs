using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

// Moves the game from scene to scene in the order set by the
// GameFlow asset. Letterbox bars close over the old scene and open
// on the new one (the sound fades with them), with a "LEVEL n" card
// on the black whenever a new level begins.
//
// It creates itself (from Resources/SceneFlow) before the first
// scene loads, so every scene can call SceneFlow.Next(), even
// when you press Play on that scene directly.
public class SceneFlow : MonoBehaviour
{
    public static SceneFlow Instance { get; private set; }

    public GameFlow flow;

    [Header("Screen")]
    public RectTransform shutterTop;
    public RectTransform shutterBottom;
    public CanvasGroup levelCard;
    public TMP_Text levelNumberText;
    public RectTransform levelLine;
    public TMP_Text levelTitleText;

    [Header("Timing")]
    public float closeTime = 0.45f;
    public float blackTime = 0.15f;
    public float openTime = 0.6f;
    public float levelCardTime = 2.5f;

    // Highest level reached, which unlocks it in the menu
    const string LevelReachedKey = "LevelReached";

    private bool transitioning;

    // 0 = bars open, 1 = closed (black screen)
    private float shut;

    private Vector2 numberPosition;

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
        numberPosition = levelNumberText.rectTransform.anchoredPosition;

        // Open on whichever scene the game started in, with its
        // level card if that scene is where its level begins
        int index = CurrentIndex();
        int level = flow.LevelOf(index);

        bool showCard = level > 0 &&
            index == flow.FirstStepOfLevel(level);

        StartCoroutine(Reveal(level, showCard));
    }

    // Don't leave the game muted if it stops mid-transition
    void OnDestroy()
    {
        AudioListener.volume = 1f;
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

    // True while the bars move or a level card shows.
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

    // Jump to a scene by name (it must be in the Build Settings).
    // The flow then carries on from wherever that scene is.
    public static void LoadScene(string sceneName)
    {
        if (Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Instance.Load(sceneName);
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

        float startShut = shut;
        float startVolume = AudioListener.volume;

        yield return Animate(closeTime, t =>
        {
            SetShutter(Mathf.Lerp(startShut, 1f, t));
            AudioListener.volume = Mathf.Lerp(startVolume, 0f, t);
        });

        yield return SceneManager.LoadSceneAsync(sceneName);

        int level = CurrentLevel;

        // A level card only when we enter a different level
        // (not when retrying a stage)
        yield return Reveal(level, level > 0 && level != fromLevel);
    }

    // Starts from a black, silent screen, shows the level card if
    // asked, then opens the bars on the scene
    IEnumerator Reveal(int level, bool showCard)
    {
        transitioning = true;
        SetShutter(1f);
        AudioListener.volume = 0f;

        yield return new WaitForSecondsRealtime(blackTime);

        if (showCard)
        {
            // Keep the new scene still behind the card
            Time.timeScale = 0f;

            if (level > LevelReached)
            {
                PlayerPrefs.SetInt(LevelReachedKey, level);
                PlayerPrefs.Save();
            }

            // The level's music comes up under the card
            AudioListener.volume = 1f;

            yield return LevelCard(level);
        }

        Time.timeScale = 1f;

        float startVolume = AudioListener.volume;

        yield return Animate(openTime, t =>
        {
            SetShutter(1f - t);
            AudioListener.volume = Mathf.Lerp(startVolume, 1f, t);
        });

        transitioning = false;
    }

    // The gold line draws out from the middle, "LEVEL n" rises
    // above it, the title types on below, then it all fades
    IEnumerator LevelCard(int level)
    {
        levelNumberText.text = "LEVEL " + level;
        levelTitleText.text = flow.LevelTitle(level);

        RectTransform number = levelNumberText.rectTransform;

        levelLine.localScale = new Vector3(0f, 1f, 1f);
        levelNumberText.alpha = 0f;
        levelTitleText.maxVisibleCharacters = 0;
        levelCard.alpha = 1f;

        Sfx.Play("level_start");

        float shown = 0f;

        yield return Animate(0.3f, t =>
            levelLine.localScale = new Vector3(t, 1f, 1f));

        yield return Animate(0.3f, t =>
        {
            levelNumberText.alpha = t;
            number.anchoredPosition = numberPosition + Vector2.down * 24f * (1f - t);
        });

        shown += 0.6f;

        int letters = levelTitleText.text.Length;

        for (int i = 1; i <= letters; i++)
        {
            levelTitleText.maxVisibleCharacters = i;

            if (levelTitleText.text[i - 1] != ' ')
                Sfx.Play("blip", 2f, 0.4f);

            yield return new WaitForSecondsRealtime(0.05f);
        }

        shown += letters * 0.05f;

        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, levelCardTime - shown));

        yield return Animate(0.3f, t => levelCard.alpha = 1f - t);

        levelTitleText.maxVisibleCharacters = 99999;
    }

    // Each bar covers just over half the screen when shut, so
    // they overlap with no seam
    void SetShutter(float amount)
    {
        shut = amount;

        float height = amount * 0.505f;

        shutterTop.anchorMin = new Vector2(0f, 1f - height);
        shutterBottom.anchorMax = new Vector2(1f, height);
    }

    // Runs step(t) with t eased from 0 to 1 over the duration, in
    // real time, so it works while the game is paused
    IEnumerator Animate(float duration, System.Action<float> step)
    {
        float time = 0f;

        while (time < duration)
        {
            // Cap the step so a loading hitch doesn't skip it
            time += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

            step(Ease(time / duration));

            yield return null;
        }

        step(1f);
    }

    // Cubic ease in and out
    static float Ease(float t)
    {
        t = Mathf.Clamp01(t);

        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
