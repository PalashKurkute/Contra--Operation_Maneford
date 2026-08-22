using UnityEngine;

// A machine-gun round from the player's helicopter. Flies right (in the
// camera's space) and hits the first shootable ChaseHitbox it touches.
public class ChaseBullet : MonoBehaviour
{
    public float speed = 18f;
    public int damage = 1;

    // Gone once it's this far right of the camera
    public float maxX = 10f;

    void Update()
    {
        transform.localPosition += Vector3.right * speed * Time.deltaTime;

        if (transform.localPosition.x > maxX)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 point = transform.position;

        for (int i = ChaseHitbox.All.Count - 1; i >= 0; i--)
        {
            ChaseHitbox box = ChaseHitbox.All[i];

            if (!box.shootable || !box.Overlaps(point, 0.08f))
                continue;

            IDamageable target = box.GetComponentInParent<IDamageable>();

            if (target == null || !target.IsAlive)
                continue;

            Effects.Impact(point);
            Sfx.Play("hit", 1f, 0.5f);

            target.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }
    }
}
