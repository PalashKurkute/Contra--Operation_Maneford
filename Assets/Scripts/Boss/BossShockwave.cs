using UnityEngine;

// A wave of energy that rolls along the ground after the Mecha-Lion
// lands. It's low, so the player can jump over it.
[RequireComponent(typeof(Rigidbody2D))]
public class BossShockwave : MonoBehaviour
{
    private Rigidbody2D body;
    private float direction = 1f;
    private float speed = 8f;
    private float minX = -1000f;
    private float maxX = 1000f;

    public void Launch(float moveDirection, float waveSpeed, float leftEdge, float rightEdge)
    {
        direction = Mathf.Sign(moveDirection);
        speed = waveSpeed;
        minX = leftEdge;
        maxX = rightEdge;

        // The sprite rolls to the right
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0f;
        }
    }

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        Destroy(gameObject, 6f);
    }

    void FixedUpdate()
    {
        body.MovePosition(body.position + Vector2.right * direction * speed * Time.fixedDeltaTime);

        if (body.position.x < minX || body.position.x > maxX)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        GameOverManager gameOverManager =
            FindAnyObjectByType<GameOverManager>();

        if (gameOverManager != null)
        {
            gameOverManager.GameOver();
        }
    }
}
