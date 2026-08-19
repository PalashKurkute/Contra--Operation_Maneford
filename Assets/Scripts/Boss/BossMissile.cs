using UnityEngine;

// A missile from the Mecha-Lion's mane. Flies straight; explodes on the
// player or when it reaches the ground.
[RequireComponent(typeof(Rigidbody2D))]
public class BossMissile : MonoBehaviour
{
    public float speed = 7f;
    public float lifetime = 4f;

    private Rigidbody2D body;
    private Vector2 velocity;
    private float groundY = -100f;
    private bool exploded;

    // direction: where it flies; ground: the floor height to explode on
    public void Launch(Vector2 direction, float missileSpeed, float ground)
    {
        velocity = direction.normalized * missileSpeed;
        groundY = ground;

        // The sprite points right
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        body.MovePosition(body.position + velocity * Time.fixedDeltaTime);

        if (body.position.y <= groundY)
        {
            Explode();
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

        Explode();
    }

    void Explode()
    {
        if (exploded)
            return;

        exploded = true;

        Effects.Impact(transform.position);
        Effects.Dust(transform.position);
        Sfx.Play("hit", 0.6f);

        Destroy(gameObject);
    }
}
