using UnityEngine;

// A rocket from the General's helicopter. Flies straight at where the
// player was. It can be shot down.
public class ChaseRocket : MonoBehaviour, IDamageable
{
    public float speed = 7f;
    public int health = 1;

    private Vector2 velocity;

    public bool IsAlive
    {
        get { return health > 0; }
    }

    public void Launch(Vector2 direction)
    {
        velocity = direction.normalized * speed;

        // The sprite points left
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + 180f;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.localPosition += (Vector3)(velocity * Time.deltaTime);

        Vector3 p = transform.localPosition;

        if (Mathf.Abs(p.x) > 12f || Mathf.Abs(p.y) > 8f)
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Effects.Explosion(transform.position);
            Destroy(gameObject);
        }
    }
}
