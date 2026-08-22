using UnityEngine;

// A lion-chip drone: it flies in from the right, wobbling and drifting
// toward the player's height. Shooting it shatters its collar, just
// like the lions' ("Those drones have collars too").
public class ChaseDrone : MonoBehaviour, IDamageable
{
    public int health = 2;
    public float speed = 3.5f;

    // How fast it drifts toward the player's height
    public float homing = 0.9f;
    public float wobble = 0.8f;

    private Transform player;
    private HitFlash hitFlash;
    private float phase;

    public bool IsAlive
    {
        get { return health > 0; }
    }

    void Start()
    {
        PlayerHelicopter heli = FindAnyObjectByType<PlayerHelicopter>();

        if (heli != null)
        {
            player = heli.transform;
        }

        hitFlash = GetComponent<HitFlash>();
        phase = Random.value * 10f;
    }

    void Update()
    {
        Vector3 p = transform.localPosition;

        p.x -= speed * Time.deltaTime;

        if (player != null)
        {
            p.y = Mathf.MoveTowards(p.y, player.localPosition.y, homing * Time.deltaTime);
        }

        p.y += Mathf.Sin(Time.time * 4f + phase) * wobble * Time.deltaTime;

        transform.localPosition = p;

        if (p.x < -12f)
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (hitFlash != null)
        {
            hitFlash.Flash();
        }

        if (health <= 0)
        {
            Effects.CollarBreak(transform.position);
            Effects.Explosion(transform.position);
            Destroy(gameObject);
        }
    }
}
