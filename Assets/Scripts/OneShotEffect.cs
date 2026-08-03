using UnityEngine;
using UnityEngine.Rendering.Universal;

// A short-lived effect: plays its sprite frames once, fades
// its light out, can stick to a moving target, then removes itself
public class OneShotEffect : MonoBehaviour
{
    public Sprite[] frames;
    public float frameTime = 0.05f;

    // How long the light takes to fade out
    public float glowTime = 0.1f;

    // When to destroy the object (leave time for particles)
    public float lifetime = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Light2D glow;
    private float glowStart;
    private float age;

    private Transform follow;
    private Vector3 followOffset;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        glow = GetComponentInChildren<Light2D>();

        if (glow != null)
        {
            glowStart = glow.intensity;
        }

        Destroy(gameObject, lifetime);
    }

    // Keep the same offset from a target (e.g. the gun)
    public void Follow(Transform target)
    {
        follow = target;
        followOffset = transform.position - target.position;
    }

    void Update()
    {
        age += Time.deltaTime;

        if (spriteRenderer != null && frames.Length > 0)
        {
            int frame = Mathf.FloorToInt(age / frameTime);

            if (frame < frames.Length)
            {
                spriteRenderer.sprite = frames[frame];
            }
            else
            {
                spriteRenderer.enabled = false;
            }
        }

        if (glow != null)
        {
            glow.intensity = glowStart *
                Mathf.Clamp01(1f - age / glowTime);
        }
    }

    void LateUpdate()
    {
        if (follow != null)
        {
            transform.position = follow.position + followOffset;
        }
    }
}
