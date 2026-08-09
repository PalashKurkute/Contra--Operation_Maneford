using UnityEngine;

// Shakes a UI element for a moment, e.g. the dialogue screen
// on a {shake} command. Uses real time, like the rest of the UI.
public class UIShake : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 home;
    private float amount;
    private float duration;
    private float timeLeft;

    void Awake()
    {
        rect = (RectTransform)transform;
        home = rect.anchoredPosition;
    }

    public void Shake(float strength, float seconds)
    {
        amount = strength;
        duration = seconds;
        timeLeft = seconds;
    }

    void Update()
    {
        if (timeLeft <= 0f)
            return;

        timeLeft -= Time.unscaledDeltaTime;

        float fade = Mathf.Max(timeLeft, 0f) / duration;
        Vector2 jitter = Random.insideUnitCircle * amount * fade;

        // Whole pixels, so the pixel art doesn't blur
        rect.anchoredPosition = home + new Vector2(
            Mathf.Round(jitter.x),
            Mathf.Round(jitter.y)
        );
    }
}
