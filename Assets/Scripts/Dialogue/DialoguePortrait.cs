using UnityEngine;
using UnityEngine.UI;

// One side of the dialogue screen. Shows a speaker's portrait, bobs
// while they talk, dims while they listen, slides in when someone new
// steps up, and shows scanlines over radio calls.
public class DialoguePortrait : MonoBehaviour
{
    public Image image;

    // Scanlines shown over radio speakers (optional)
    public GameObject radioOverlay;

    // Slides in from the left (-1) or from the right (1)
    public float enterFrom = -1f;

    [Header("Motion")]
    // One art pixel at 6x scale, so the bob stays pixel-crisp
    public float bobHeight = 6f;
    public float bobSpeed = 12f;
    public float slideDistance = 90f;
    public float slideTime = 0.2f;

    // Tint while someone else is talking
    public Color listeningColor = new Color(0.42f, 0.42f, 0.5f, 1f);

    public string SpeakerId { get; private set; }

    private RectTransform rect;
    private Vector2 home;
    private bool talking;
    private bool lit;
    private float slide = 1f;
    private float bobTime;

    void Awake()
    {
        rect = (RectTransform)transform;
        home = rect.anchoredPosition;

        Hide();
    }

    public void Show(string speakerId, Sprite sprite, bool radio)
    {
        // A new face slides in; the same face just changes expression
        if (speakerId != SpeakerId)
        {
            slide = 0f;
        }

        SpeakerId = speakerId;

        image.sprite = sprite;
        image.enabled = sprite != null;

        if (radioOverlay != null)
        {
            radioOverlay.SetActive(radio && sprite != null);
        }
    }

    public void Hide()
    {
        SpeakerId = null;
        talking = false;

        image.enabled = false;

        if (radioOverlay != null)
        {
            radioOverlay.SetActive(false);
        }
    }

    public void SetTalking(bool value)
    {
        talking = value;
        bobTime = 0f;
    }

    public void SetLit(bool value)
    {
        lit = value;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        slide = Mathf.MoveTowards(slide, 1f, dt / slideTime);
        float eased = 1f - (1f - slide) * (1f - slide);

        // Hop up one pixel and back while talking
        float bob = 0f;

        if (talking)
        {
            bobTime += dt;
            bob = Mathf.Round(Mathf.Abs(Mathf.Sin(bobTime * bobSpeed))) * bobHeight;
        }

        rect.anchoredPosition = home + new Vector2(
            enterFrom * slideDistance * (1f - eased),
            bob
        );

        Color target = lit ? Color.white : listeningColor;
        Color color = Color.Lerp(image.color, target, dt * 12f);
        color.a = eased;

        image.color = color;
    }
}
