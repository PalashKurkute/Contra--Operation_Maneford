using UnityEngine;

// Blinks a sprite on and off, like an aircraft's navigation light
[RequireComponent(typeof(SpriteRenderer))]
public class BlinkingLight : MonoBehaviour
{
    public float onTime = 0.15f;
    public float offTime = 0.85f;

    // Start part-way through the cycle (0..1), so lights don't sync
    [Range(0f, 1f)]
    public float offset = 0f;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float cycle = onTime + offTime;
        float time = Mathf.Repeat(Time.time + offset * cycle, cycle);

        spriteRenderer.enabled = time < onTime;
    }
}
