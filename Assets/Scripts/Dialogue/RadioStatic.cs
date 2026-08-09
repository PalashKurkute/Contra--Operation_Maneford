using UnityEngine;
using UnityEngine.UI;

// Animated static on a RawImage: jumps the noise texture around every
// frame and flickers its strength. Burst() makes it strong for a
// moment (when a radio call starts). With a scroll speed it slides
// instead of jumping, which suits scanlines.
//
// The texture's Wrap Mode must be Repeat.
[RequireComponent(typeof(RawImage))]
public class RadioStatic : MonoBehaviour
{
    public float minAlpha = 0.03f;
    public float maxAlpha = 0.09f;
    public float burstAlpha = 0.35f;

    // Size of one texture repeat on screen
    public float tileSize = 256f;

    // Repeats per second; 0 = random jumps (noise)
    public float scrollSpeed = 0f;

    private RawImage image;
    private RectTransform rect;
    private float burstLeft;
    private float offset;

    void Awake()
    {
        image = GetComponent<RawImage>();
        rect = (RectTransform)transform;
    }

    public void Burst(float seconds)
    {
        burstLeft = seconds;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        Vector2 position;

        if (scrollSpeed != 0f)
        {
            offset = Mathf.Repeat(offset + scrollSpeed * dt, 1f);
            position = new Vector2(0f, offset);
        }
        else
        {
            position = new Vector2(Random.value, Random.value);
        }

        image.uvRect = new Rect(position, rect.rect.size / tileSize);

        float alpha = burstLeft > 0f ?
            burstAlpha * Random.Range(0.6f, 1f) :
            Random.Range(minAlpha, maxAlpha);

        burstLeft -= dt;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
