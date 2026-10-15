using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;
    public float lifetime = 3f;

    // Removed once this far outside the camera view, so it can't
    // hit enemies the player can't see yet
    public float offscreenMargin = 0.5f;

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

        if (OutOfView())
        {
            Destroy(gameObject);
        }
    }

    bool OutOfView()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return false;

        float halfHeight = cam.orthographicSize + offscreenMargin;
        float halfWidth = cam.orthographicSize * cam.aspect + offscreenMargin;

        Vector3 offset = transform.position - cam.transform.position;

        return Mathf.Abs(offset.x) > halfWidth ||
            Mathf.Abs(offset.y) > halfHeight;
    }

    // The bullet collider is a trigger, so it never pushes
    // enemies around and bullets fly through each other
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only hit once, even if we overlap two things this step
        if (hasHit || OutOfView())
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