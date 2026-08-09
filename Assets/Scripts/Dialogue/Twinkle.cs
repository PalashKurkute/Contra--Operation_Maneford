using UnityEngine;

// Makes a small sprite glint: it pulses in size and brightness
// (e.g. the broken collar glinting on the ground)
[RequireComponent(typeof(SpriteRenderer))]
public class Twinkle : MonoBehaviour
{
    public float speed = 3f;
    public float minScale = 0.4f;
    public float maxScale = 1.2f;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private float phase;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        phase = Random.value * 10f;
    }

    void Update()
    {
        // Sharp peaks: mostly small, briefly bright
        float wave = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Time.time * speed + phase), 4f);

        transform.localScale = baseScale * Mathf.Lerp(minScale, maxScale, wave);

        Color color = spriteRenderer.color;
        color.a = Mathf.Lerp(0.2f, 1f, wave);
        spriteRenderer.color = color;
    }
}
