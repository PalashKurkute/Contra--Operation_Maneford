using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

// A lion's mind-control collar breaking: a white flash, the two halves
// of the collar spinning away, and bright shards that fall and fade.
// Spawned through Effects.CollarBreak(position).
public class CollarBreakEffect : MonoBehaviour
{
    public Sprite halfSprite;
    public Sprite shardSprite;
    public Sprite flashSprite;

    // Unlit, so the sparks glow at night too
    public Material glowMaterial;

    public int shards = 10;
    public float lifetime = 0.9f;
    public float gravity = 14f;

    public string sortingLayer = "Player";
    public int sortingOrder = 20;

    class Piece
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector2 velocity;
        public float spin;
    }

    private List<Piece> pieces = new List<Piece>();
    private SpriteRenderer flash;
    private Light2D flashLight;
    private float age;

    void Start()
    {
        // The two halves fly apart
        AddPiece(halfSprite, new Vector2(-2.6f, 5f), 720f, false, Color.white);
        AddPiece(halfSprite, new Vector2(2.6f, 5.5f), -720f, true, Color.white);

        // Sparks and shards
        Color[] colors =
        {
            Color.white,
            new Color(1f, 0.9f, 0.4f),
            new Color(0.5f, 0.95f, 1f)
        };

        for (int i = 0; i < shards; i++)
        {
            float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
            float speed = Random.Range(3f, 6.5f);

            AddPiece(
                shardSprite,
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                Random.Range(-400f, 400f),
                false,
                colors[i % colors.Length]
            ).renderer.sharedMaterial = glowMaterial;
        }

        // Quick white flash with a burst of light
        GameObject flashObject = new GameObject("Flash");
        flashObject.transform.SetParent(transform, false);

        flash = flashObject.AddComponent<SpriteRenderer>();
        flash.sprite = flashSprite;
        flash.sharedMaterial = glowMaterial;
        flash.sortingLayerName = sortingLayer;
        flash.sortingOrder = sortingOrder + 1;

        flashLight = flashObject.AddComponent<Light2D>();
        flashLight.lightType = Light2D.LightType.Point;
        flashLight.color = new Color(0.8f, 0.95f, 1f);
        flashLight.intensity = 2f;
        flashLight.pointLightOuterRadius = 2.5f;
        flashLight.pointLightInnerRadius = 0f;

        Destroy(gameObject, lifetime);
    }

    Piece AddPiece(Sprite sprite, Vector2 velocity, float spin, bool flip, Color color)
    {
        GameObject pieceObject = new GameObject("Piece");
        pieceObject.transform.SetParent(transform, false);

        SpriteRenderer pieceRenderer = pieceObject.AddComponent<SpriteRenderer>();
        pieceRenderer.sprite = sprite;
        pieceRenderer.flipX = flip;
        pieceRenderer.color = color;
        pieceRenderer.sortingLayerName = sortingLayer;
        pieceRenderer.sortingOrder = sortingOrder;

        Piece piece = new Piece();
        piece.transform = pieceObject.transform;
        piece.renderer = pieceRenderer;
        piece.velocity = velocity;
        piece.spin = spin;

        pieces.Add(piece);

        return piece;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        // Fade everything out over the last 40% of its life
        float fade = Mathf.Clamp01((lifetime - age) / (lifetime * 0.4f));

        foreach (Piece piece in pieces)
        {
            piece.velocity.y -= gravity * dt;
            piece.transform.localPosition += (Vector3)(piece.velocity * dt);
            piece.transform.Rotate(0f, 0f, piece.spin * dt);

            Color color = piece.renderer.color;
            color.a = fade;
            piece.renderer.color = color;
        }

        // The flash swells and vanishes in a fifth of a second
        float flashTime = Mathf.Clamp01(age / 0.2f);

        flash.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2f, flashTime);
        flash.color = new Color(1f, 1f, 1f, 1f - flashTime);
        flashLight.intensity = 2f * (1f - flashTime);
    }
}
