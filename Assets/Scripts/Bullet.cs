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

        // Lions, soldiers, the boss...
        IDamageable target =
            other.GetComponentInParent<IDamageable>();

        // Already defeated (e.g. a freed lion running off): fly past
        if (target != null && !target.IsAlive)
            return;

        hasHit = true;

        Effects.Impact(transform.position);

        Debug.Log(
            "Player bullet hit: " +
            other.gameObject.name
        );

        if (target != null)
        {
            Sfx.Play("hit");

            target.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}