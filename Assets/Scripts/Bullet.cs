using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;
    public float lifetime = 3f;

    private Vector2 direction = Vector2.right;
    private Rigidbody2D body;
    private bool hasHit = false;

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
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
            body.linearVelocity = direction * speed;
        }
    }

    // The bullet collider is a trigger, so it never pushes
    // enemies around and bullets fly through each other
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only hit once, even if we overlap two things this step
        if (hasHit)
            return;

        // Ignore other bullets / trigger zones and the player
        if (other.isTrigger || other.CompareTag("Player"))
            return;

        hasHit = true;

        Effects.Impact(transform.position);

        Debug.Log(
            "Player bullet hit: " +
            other.gameObject.name
        );

        // Level 2 enemy
        Level2Enemy level2Enemy =
            other.GetComponentInParent<Level2Enemy>();

        if (level2Enemy != null)
        {
            Debug.Log("🔴 Player bullet hit Level 2 Enemy!");

            Sfx.Play("hit");

            level2Enemy.TakeDamage(damage);

            Destroy(gameObject);

            return;
        }

        // Level 1 enemy
        Enemy enemy =
            other.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            Debug.Log("🟢 Player bullet hit Level 1 Enemy!");

            Sfx.Play("hit");

            enemy.TakeDamage(damage);

            Destroy(gameObject);

            return;
        }

        // Bullet hit something else
        Destroy(gameObject);
    }
}