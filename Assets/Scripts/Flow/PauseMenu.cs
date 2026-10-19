using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

// Escape pauses the game anywhere in the story (not the main menu
// or the credits): RESUME, RESTART (in playable scenes), MAIN MENU
// and QUIT, with the controls listed underneath. It also shows the
// controls across the top of the screen the first time the player
// gets control in a session.
//
// Like SceneFlow it creates itself before the first scene loads
// and builds its own UI, borrowing SceneFlow's pixel font.
public class PauseMenu : MonoBehaviour
{
    const string OnFootHint =
        "A/D MOVE   W/S AIM   SPACE JUMP   J FIRE   SHIFT DASH   ESC PAUSE";

    const string FlyingHint =
        "WASD FLY   HOLD J FIRE   ESC PAUSE";

    const string OnFootControls =
        "MOVE    A D  OR ARROWS\n" +
        "AIM     W S  (8 WAYS)\n" +
        "JUMP    SPACE\n" +
        "DROP    DOWN + SPACE\n" +
        "FIRE    J\n" +
        "DASH    SHIFT\n" +
        "DUCK    S  STANDING STILL\n" +
        "SKIP    HOLD ENTER";

    const string FlyingControls =
        "FLY     WASD OR ARROWS\n" +
        "FIRE    HOLD J";

    const string StoryControls =
        "NEXT    ENTER  SPACE  J\n" +
        "SKIP    HOLD ENTER";

    static readonly Color NormalColor = Color.white;
    static readonly Color SelectedColor = new Color(1f, 0.84f, 0.29f);
    static readonly Color DimColor = new Color(0.6f, 0.6f, 0.6f);

    // How long the controls hint stays up, in real seconds
    const float HintTime = 7f;
    const float HintFade = 0.4f;

    private static PauseMenu instance;

    private GameObject panel;
    private TMP_Text menuText;
    private TMP_Text controlsText;
    private CanvasGroup hint;
    private TMP_Text hintText;

    private bool paused;
    private int closedFrame = -1;

    private List<string> items = new List<string>();
    private int selected;

    private List<VideoPlayer> pausedVideos = new List<VideoPlayer>();

    // Hints already shown this session (on foot, flying)
    private HashSet<string> hintsShown = new HashSet<string>();
    private string pendingHint;
    private float hintAge = -1f;

    // True while the menu is open, and on the frame it closes, so
    // the key that closed it doesn't also act in the game
    public static bool IsPaused
    {
        get
        {
            return instance != null &&
                (instance.paused || Time.frameCount == instance.closedFrame);
        }
    }

    // =========================
    // STARTUP
    // =========================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    static void CreateInstance()
    {
        instance = null;

        GameObject host = new GameObject("PauseMenu");
        DontDestroyOnLoad(host);

        instance = host.AddComponent<PauseMenu>();
    }

    // Built in Start, once SceneFlow (and its font) exists
    void Start()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Over the HUD and dialogue, under SceneFlow's shutter
        canvas.sortingOrder = 900;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        TMP_FontAsset font = SceneFlow.Instance != null ?
            SceneFlow.Instance.levelTitleText.font : null;

        // The pause screen: dark veil, title, menu, controls. (UI
        // blends in linear colour, so it takes a high alpha to look
        // properly dark.)
        panel = MakeImage("Panel", transform, new Color(0f, 0f, 0f, 0.9f));
        Stretch(panel.GetComponent<RectTransform>(), 0f, 1f);

        TMP_Text title = MakeText("Title", panel.transform, font, 72f, SelectedColor);
        title.text = "PAUSED";
        Place(title.rectTransform, 0.5f, 0.78f);

        menuText = MakeText("Menu", panel.transform, font, 36f, NormalColor);
        menuText.alignment = TextAlignmentOptions.Left;
        menuText.lineSpacing = 30f;
        Place(menuText.rectTransform, 0.5f, 0.58f);
        menuText.rectTransform.sizeDelta = new Vector2(420f, 300f);

        controlsText = MakeText("Controls", panel.transform, font, 22f, DimColor);
        controlsText.alignment = TextAlignmentOptions.TopLeft;
        controlsText.lineSpacing = 50f;
        Place(controlsText.rectTransform, 0.5f, 0.2f);
        controlsText.rectTransform.sizeDelta = new Vector2(620f, 360f);

        panel.SetActive(false);

        // The controls hint: a dark strip under the HUD (the in-level
        // dialogue box has the bottom of the screen)
        GameObject strip = MakeImage("Hint", transform, new Color(0f, 0f, 0f, 0.6f));
        RectTransform stripRect = strip.GetComponent<RectTransform>();
        stripRect.anchorMin = new Vector2(0f, 1f);
        stripRect.anchorMax = new Vector2(1f, 1f);
        stripRect.pivot = new Vector2(0.5f, 1f);
        stripRect.anchoredPosition = new Vector2(0f, -110f);
        stripRect.sizeDelta = new Vector2(0f, 56f);

        hint = strip.AddComponent<CanvasGroup>();
        hint.alpha = 0f;

        hintText = MakeText("Text", strip.transform, font, 22f, NormalColor);
        Stretch(hintText.rectTransform, 0f, 1f);

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Leaving through the menu kept the sound paused until now
        AudioListener.pause = false;
        pausedVideos.Clear();

        hint.alpha = 0f;
        hintAge = -1f;

        // The controls the first time the player gets control
        GameFlow.StepType type;

        pendingHint = PlayableType(out type) && !hintsShown.Contains(HintFor(type)) ?
            HintFor(type) : null;
    }

    // =========================
    // UPDATE
    // =========================

    void Update()
    {
        if (panel == null)
            return;

        if (paused)
        {
            UpdateMenu();
        }
        else if (Pressed(Keyboard.current != null ? Keyboard.current.escapeKey : null) &&
            CanPause())
        {
            Open();
        }

        UpdateHint();
    }

    // Hit-stop and slow-motion coroutines run in real time and may
    // reset the time scale while we're paused; keep it frozen
    void LateUpdate()
    {
        if (paused)
            Time.timeScale = 0f;
    }

    bool CanPause()
    {
        GameFlow.Step step = SceneFlow.CurrentStep;

        // Not in the main menu (no step) or the credits (Escape
        // skips them), nor while the game is already stopped:
        // game over, a level card, a hit-stop
        return step != null &&
            step.type != GameFlow.StepType.Credits &&
            !SceneFlow.IsTransitioning &&
            !StageManager.IsCleared &&
            Time.timeScale > 0f;
    }

    // Is this scene one the player plays, and what kind
    bool PlayableType(out GameFlow.StepType type)
    {
        GameFlow.Step step = SceneFlow.CurrentStep;

        type = step != null ? step.type : GameFlow.StepType.Video;

        return step != null &&
            (type == GameFlow.StepType.Stage ||
            type == GameFlow.StepType.Boss ||
            type == GameFlow.StepType.Chase);
    }

    string HintFor(GameFlow.StepType type)
    {
        return type == GameFlow.StepType.Chase ? FlyingHint : OnFootHint;
    }

    // =========================
    // THE MENU
    // =========================

    void Open()
    {
        paused = true;
        selected = 0;

        GameFlow.StepType type;
        bool playable = PlayableType(out type);

        items.Clear();
        items.Add("RESUME");

        if (playable)
            items.Add("RESTART");

        items.Add("MAIN MENU");

#if !UNITY_WEBGL
        items.Add("QUIT");
#endif

        controlsText.text = !playable ? StoryControls :
            type == GameFlow.StepType.Chase ? FlyingControls :
            OnFootControls;

        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Videos don't follow the time scale
        pausedVideos.Clear();

        foreach (VideoPlayer video in FindObjectsByType<VideoPlayer>())
        {
            if (video.isPlaying)
            {
                video.Pause();
                pausedVideos.Add(video);
            }
        }

        panel.SetActive(true);
        Refresh();

        Sfx.Play("menu_move");
    }

    void UpdateMenu()
    {
        Keyboard keys = Keyboard.current;

        if (keys == null)
            return;

        if (Pressed(keys.escapeKey))
        {
            Resume();
        }
        else if (Pressed(keys.wKey) || Pressed(keys.upArrowKey))
        {
            Move(-1);
        }
        else if (Pressed(keys.sKey) || Pressed(keys.downArrowKey))
        {
            Move(1);
        }
        else if (Pressed(keys.enterKey) || Pressed(keys.numpadEnterKey) ||
            Pressed(keys.spaceKey) || Pressed(keys.jKey))
        {
            Choose();
        }
    }

    void Move(int direction)
    {
        selected = (selected + direction + items.Count) % items.Count;

        Sfx.Play("menu_move");
        Refresh();
    }

    void Choose()
    {
        Sfx.Play("menu_select");

        switch (items[selected])
        {
            case "RESUME":
                Resume();
                break;

            case "RESTART":
                Leave();
                SceneFlow.Restart();
                break;

            case "MAIN MENU":
                Leave();
                SceneFlow.LoadMainMenu();
                break;

            case "QUIT":
                Quit();
                break;
        }
    }

    void Resume()
    {
        Close();

        Time.timeScale = 1f;
        AudioListener.pause = false;

        foreach (VideoPlayer video in pausedVideos)
        {
            if (video != null)
                video.Play();
        }

        pausedVideos.Clear();
    }

    // Off to another scene: the game stays frozen behind the
    // shutter, and the sound comes back with the next scene
    void Leave()
    {
        Close();
    }

    void Close()
    {
        paused = false;
        closedFrame = Time.frameCount;

        panel.SetActive(false);
    }

    void Quit()
    {
#if UNITY_EDITOR
        Resume();
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Refresh()
    {
        string text = "";

        for (int i = 0; i < items.Count; i++)
        {
            Color color = i == selected ? SelectedColor : NormalColor;
            string cursor = i == selected ? "> " : "  ";

            text += "<color=#" + ColorUtility.ToHtmlStringRGB(color) +
                ">" + cursor + items[i] + "</color>\n";
        }

        menuText.text = text;
    }

    // =========================
    // CONTROLS HINT
    // =========================

    void UpdateHint()
    {
        // Wait for the shutter to open on the stage
        if (pendingHint != null && !SceneFlow.IsTransitioning)
        {
            hintText.text = pendingHint;
            hintsShown.Add(pendingHint);
            pendingHint = null;
            hintAge = 0f;
        }

        if (hintAge < 0f)
            return;

        // Hidden while paused (the menu lists the controls)
        if (paused)
        {
            hint.alpha = 0f;
            return;
        }

        hintAge += Time.unscaledDeltaTime;

        float fadeIn = hintAge / HintFade;
        float fadeOut = (HintTime - hintAge) / HintFade;

        hint.alpha = Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut));

        if (hintAge >= HintTime)
            hintAge = -1f;
    }

    // =========================
    // UI HELPERS
    // =========================

    static bool Pressed(KeyControl key)
    {
        return key != null && key.wasPressedThisFrame;
    }

    static GameObject MakeImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return go;
    }

    static TMP_Text MakeText(string name, Transform parent, TMP_FontAsset font,
        float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        return text;
    }

    // Fill the parent between two heights (0 = bottom, 1 = top)
    static void Stretch(RectTransform rect, float bottom, float top)
    {
        rect.anchorMin = new Vector2(0f, bottom);
        rect.anchorMax = new Vector2(1f, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Centre on a point of the screen (0..1 each way)
    static void Place(RectTransform rect, float x, float y)
    {
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x, y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1200f, 100f);
    }
}
