using UnityEngine;

// A piece that flies off, spins, falls and fades away
// (armour plates blown off the Mecha-Lion, wreckage...)
[RequireComponent(typeof(SpriteRenderer))]
public class Debris : MonoBehaviour
{
    public Vector2 velocity;
    public float spin;
    public float gravity = 16f;
    public float lifetime = 1.4f;

    private SpriteRenderer spriteRenderer;
    private float age;

    // Throw a sprite out from a point
    public static Debris Throw(
        Sprite sprite,
        Vector3 position,
        Vector2 velocity,
        float spin,
        string sortingLayer,
        int sortingOrder
    )
    {
        GameObject piece = new GameObject("Debris");
        piece.transform.position = position;

        SpriteRenderer pieceRenderer = piece.AddComponent<SpriteRenderer>();
        pieceRenderer.sprite = sprite;
        pieceRenderer.sortingLayerName = sortingLayer;
        pieceRenderer.sortingOrder = sortingOrder;

        Debris debris = piece.AddComponent<Debris>();
        debris.velocity = velocity;
        debris.spin = spin;

        return debris;
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        velocity.y -= gravity * dt;
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0f, 0f, spin * dt);

        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01((lifetime - age) / (lifetime * 0.4f));
        spriteRenderer.color = color;
    }
}
