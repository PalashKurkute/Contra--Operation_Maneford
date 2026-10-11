using UnityEngine;

// An afterimage left behind by the player's dash: a tinted copy of
// the current sprite that fades out where it was dropped.
public class DashGhost : MonoBehaviour
{
    public float fadeTime = 0.2f;

    private SpriteRenderer spriteRenderer;
    private Color startColor;
    private float age;

    public static void Spawn(SpriteRenderer source, Color color)
    {
        if (source == null || source.sprite == null)
            return;

        GameObject ghost = new GameObject("DashGhost");

        ghost.transform.SetPositionAndRotation(
            source.transform.position,
            source.transform.rotation
        );
        ghost.transform.localScale = source.transform.lossyScale;

        SpriteRenderer copy = ghost.AddComponent<SpriteRenderer>();
        copy.sprite = source.sprite;
        copy.flipX = source.flipX;
        copy.flipY = source.flipY;
        copy.color = color;
        copy.sharedMaterial = source.sharedMaterial;
        copy.sortingLayerID = source.sortingLayerID;

        // Just behind the player
        copy.sortingOrder = source.sortingOrder - 1;

        ghost.AddComponent<DashGhost>();
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startColor = spriteRenderer.color;
    }

    void Update()
    {
        age += Time.deltaTime;

        Color color = startColor;
        color.a = Mathf.Lerp(startColor.a, 0f, age / fadeTime);
        spriteRenderer.color = color;

        if (age >= fadeTime)
        {
            Destroy(gameObject);
        }
    }
}
