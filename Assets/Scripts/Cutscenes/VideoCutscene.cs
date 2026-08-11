using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

// Plays a video cutscene full screen with subtitles, then moves
// the game on to the next scene. Hold Enter to skip.
//
// Every cutscene scene uses the same prefab
// (Prefabs/UI/VideoCutscene); only its CutsceneData changes.
// While a cutscene has no clip yet, its placeholder card shows
// for a few seconds instead, with the subtitles on the same timing.
public class VideoCutscene : MonoBehaviour
{
    public CutsceneData cutscene;

    [Header("Video")]
    public VideoPlayer player;
    public RawImage screen;
    public AspectRatioFitter screenFitter;
    public AudioSource audioSource;

    [Header("Placeholder")]
    public GameObject placeholder;
    public Image placeholderImage;
    public TMP_Text placeholderText;

    [Header("Subtitles")]
    public CanvasGroup subtitleGroup;
    public TMP_Text subtitleText;
    public float subtitleFade = 0.15f;

    [Header("Skip")]
    public CanvasGroup skipPrompt;
    public Image skipFill;
    public float holdToSkip = 1f;

    // How long the prompt stays up after a key press
    public float promptTime = 2.5f;

    private RenderTexture target;
    private bool usingVideo;
    private bool finished;

    private float placeholderTime;
    private float holdTime;
    private bool holdStarted;
    private float promptTimer;
    private int shownSubtitle = -1;

    // =========================
    // STARTUP
    // =========================

    void Start()
    {
        subtitleGroup.alpha = 0f;
        skipPrompt.alpha = 0f;

        if (cutscene != null && cutscene.clip != null)
        {
            StartVideo();
        }
        else
        {
            StartPlaceholder();
        }
    }

    void StartVideo()
    {
        usingVideo = true;
        placeholder.SetActive(false);

        VideoClip clip = cutscene.clip;

        target = new RenderTexture(
            (int)clip.width, (int)clip.height, 0
        );
        target.name = "Cutscene Video";

        // Black until the first frame arrives
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = target;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = active;

        screen.texture = target;
        screenFitter.aspectRatio = (float)clip.width / clip.height;

        player.playOnAwake = false;
        player.isLooping = false;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = target;
        player.clip = clip;

        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.controlledAudioTrackCount = 1;
        player.EnableAudioTrack(0, true);
        player.SetTargetAudioSource(0, audioSource);
        audioSource.volume = cutscene.volume;

        player.prepareCompleted += OnPrepared;
        player.loopPointReached += OnVideoEnd;
        player.errorReceived += OnVideoError;

        player.Prepare();
    }

    void StartPlaceholder()
    {
        usingVideo = false;
        screen.gameObject.SetActive(false);
        placeholder.SetActive(true);

        if (cutscene == null)
        {
            placeholderText.text = "No cutscene is set on this scene.";
            return;
        }

        placeholderText.text = cutscene.placeholderText;

        if (cutscene.placeholderImage != null)
        {
            placeholderImage.sprite = cutscene.placeholderImage;
            placeholderImage.color = Color.white;
        }
    }

    void OnPrepared(VideoPlayer source)
    {
        source.Play();
    }

    void OnVideoEnd(VideoPlayer source)
    {
        Finish();
    }

    void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("❌ Cutscene video failed: " + message);

        // Carry on with the placeholder rather than a black screen
        player.Stop();
        StartPlaceholder();
    }

    // Unity pauses a video that starts while the window is in the
    // background; carry on when the window gets focus back
    void OnApplicationFocus(bool focus)
    {
        if (focus && usingVideo && !finished &&
            player.isPrepared && !player.isPlaying)
            player.Play();
    }

    void OnDestroy()
    {
        if (target != null)
        {
            target.Release();
            Destroy(target);
        }
    }

    // =========================
    // UPDATE
    // =========================

    void Update()
    {
        if (finished)
            return;

        float time = CurrentTime();

        UpdateSubtitles(time);

        if (!usingVideo)
        {
            placeholderTime += Time.deltaTime;

            float length = cutscene != null ?
                cutscene.placeholderLength : 5f;

            if (placeholderTime >= length)
            {
                Finish();
                return;
            }
        }

        UpdateSkip();
    }

    float CurrentTime()
    {
        return usingVideo ? (float)player.time : placeholderTime;
    }

    void UpdateSubtitles(float time)
    {
        int index = SubtitleAt(time);

        if (index != shownSubtitle && index >= 0)
        {
            CutsceneData.Subtitle line = cutscene.subtitles[index];

            subtitleText.text = string.IsNullOrEmpty(line.speaker) ?
                line.text :
                "<color=#" +
                ColorUtility.ToHtmlStringRGB(line.speakerColor) +
                ">" + line.speaker + "</color>  " + line.text;
        }

        if (index >= 0)
            shownSubtitle = index;

        float goal = index >= 0 ? 1f : 0f;

        subtitleGroup.alpha = Mathf.MoveTowards(
            subtitleGroup.alpha, goal,
            Time.unscaledDeltaTime / subtitleFade
        );
    }

    int SubtitleAt(float time)
    {
        if (cutscene == null || cutscene.subtitles == null)
            return -1;

        for (int i = 0; i < cutscene.subtitles.Length; i++)
        {
            CutsceneData.Subtitle line = cutscene.subtitles[i];

            if (time >= line.start && time < line.end)
                return i;
        }

        return -1;
    }

    // Any key shows the prompt; holding Enter fills the bar,
    // and a full bar skips the cutscene
    void UpdateSkip()
    {
        if (SceneFlow.IsTransitioning)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            promptTimer = promptTime;

        // Only a hold that starts in this scene counts, so Enter
        // still held from the scene before doesn't skip this one
        if (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
            keyboard.numpadEnterKey.wasPressedThisFrame))
            holdStarted = true;

        if (!MenuInput.SkipHeld())
            holdStarted = false;

        if (holdStarted)
        {
            holdTime += Time.unscaledDeltaTime;
            promptTimer = promptTime;
        }
        else
        {
            // Let go: the bar drains back quickly
            holdTime = Mathf.Max(
                0f, holdTime - Time.unscaledDeltaTime * 3f
            );
        }

        promptTimer -= Time.unscaledDeltaTime;

        skipFill.fillAmount = holdTime / holdToSkip;

        skipPrompt.alpha = Mathf.MoveTowards(
            skipPrompt.alpha, promptTimer > 0f ? 1f : 0f,
            Time.unscaledDeltaTime * 4f
        );

        if (holdTime >= holdToSkip)
        {
            Finish();
        }
    }

    // =========================
    // END
    // =========================

    void Finish()
    {
        if (finished)
            return;

        finished = true;

        skipPrompt.alpha = 0f;
        subtitleGroup.alpha = 0f;

        StartCoroutine(FadeOutAudio());

        SceneFlow.Next();
    }

    // Fades the sound along with SceneFlow's fade to black,
    // so a skip doesn't cut the audio off mid-word
    IEnumerator FadeOutAudio()
    {
        float start = audioSource.volume;
        float time = 0f;
        float length = SceneFlow.Instance != null ?
            SceneFlow.Instance.fadeTime : 0.4f;

        while (time < length)
        {
            time += Time.unscaledDeltaTime;

            audioSource.volume = Mathf.Lerp(start, 0f, time / length);

            yield return null;
        }

        audioSource.volume = 0f;

        if (usingVideo)
            player.Pause();
    }
}
