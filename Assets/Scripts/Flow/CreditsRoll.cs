using UnityEngine;

// Scrolls the credits up the screen until the closing "thank you"
// sits mid-screen, holds it, then returns to the main menu
// (the next step in the game flow). Hold Enter, Space or J to speed
// it up; Escape skips to the end.
public class CreditsRoll : MonoBehaviour
{
    // The text block that scrolls (anchored to the bottom of the screen)
    public RectTransform content;

    public float speed = 70f;
    public float fastSpeed = 360f;

    // The roll stops once the last line is this far up the screen
    // (0 = bottom, 1 = top), and holds there for endPause
    public float stopHeight = 0.42f;
    public float endPause = 4f;

    private float startY;
    private float endTimer;
    private bool done;

    void Start()
    {
        startY = content.anchoredPosition.y;
    }

    void Update()
    {
        if (done || SceneFlow.IsTransitioning)
            return;

        UnityEngine.InputSystem.Keyboard keys = UnityEngine.InputSystem.Keyboard.current;

        bool fast = keys != null &&
            (keys.enterKey.isPressed || keys.spaceKey.isPressed || keys.jKey.isPressed);

        float travel = content.rect.height +
            ((RectTransform)content.parent).rect.height * stopHeight;

        Vector2 p = content.anchoredPosition;
        p.y = Mathf.Min(p.y + (fast ? fastSpeed : speed) * Time.deltaTime, startY + travel);
        content.anchoredPosition = p;

        bool finished = p.y >= startY + travel;

        if (finished)
        {
            endTimer += Time.deltaTime;
        }

        if (MenuInput.Back() || (finished && endTimer >= endPause))
        {
            done = true;
            SceneFlow.Next();
        }
    }
}
