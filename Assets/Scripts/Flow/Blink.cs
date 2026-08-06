using UnityEngine;

// Blinks a UI element on and off, e.g. "PRESS ENTER".
// Uses real time, so it keeps blinking while the game is paused.
public class Blink : MonoBehaviour
{
    public float onTime = 0.6f;
    public float offTime = 0.3f;

    private CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();

        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        float time = Mathf.Repeat(Time.unscaledTime, onTime + offTime);

        group.alpha = time < onTime ? 1f : 0f;
    }
}
