using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 5f;

    private float direction = 1f;
    private Rigidbody2D body;
    private bool hasHit = false;

    public void SetDirection(float newDirection)
    {
        direction = newDirection;
    }

    void Start()
    {
        body = GetComponent<Rigidbody2D>();

        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (body != null)
        {
            body.linearVelocity = new Vector2(
                direction * speed,
                0f
            );
        }
    }

    // The bullet collider is a trigger, so it flies through
    // other enemies and bullets instead of pushing them
    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (hasHit)
            return;

        // Ignore other bullets / trigger zones and enemies
        if (other.isTrigger || other.CompareTag("Enemy"))
            return;

        hasHit = true;

        Effects.Impact(transform.position);

        // Hit player
        if (other.CompareTag("Player"))
        {
            GameOverManager gameOverManager =
                FindAnyObjectByType<GameOverManager>();

            if (gameOverManager != null)
            {
                gameOverManager.GameOver();
            }
        }

        // Destroy bullet after collision
        Destroy(gameObject);
    }
}