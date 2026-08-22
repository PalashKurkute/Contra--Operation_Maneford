using UnityEngine;

// Loops a few sprite frames, e.g. spinning rotor blades
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrames : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 16f;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0)
            return;

        int frame = (int)(Time.time * framesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[frame];
    }
}
