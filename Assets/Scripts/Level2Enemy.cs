using UnityEngine;

public class Level2Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float stopDistance = 5f;
    public int health = 2;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootInterval = 2f;

    private Transform player;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;

    private float shootTimer = 0f;

    void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Mathf.Abs(
            player.position.x -
            transform.position.x
        );

        shootTimer += Time.deltaTime;

        if (distance <= stopDistance &&
            shootTimer >= shootInterval)
        {
            Shoot();

            shootTimer = 0f;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        float distance = Mathf.Abs(
            player.position.x -
            transform.position.x
        );

        float direction = 0f;

        if (distance > stopDistance)
        {
            if (player.position.x < transform.position.x)
            {
                direction = -1f;
            }
            else if (player.position.x > transform.position.x)
            {
                direction = 1f;
            }
        }

        body.linearVelocity = new Vector2(
            direction * moveSpeed,
            body.linearVelocity.y
        );

        // Sprite naturally faces RIGHT
        if (direction < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (direction > 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning(
                "Level2Enemy: Bullet Prefab missing!"
            );
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning(
                "Level2Enemy: FirePoint missing!"
            );
            return;
        }

        float direction = 1f;

        if (player.position.x < transform.position.x)
        {
            direction = -1f;
        }

        // The fire point sits on the right; mirror it when
        // shooting left so the shot leaves the gun, not our back
        Vector3 muzzleLocal = firePoint.localPosition;
        muzzleLocal.x *= direction;

        Vector3 muzzle = transform.TransformPoint(muzzleLocal);

        GameObject bullet = Instantiate(
            bulletPrefab,
            muzzle,
            Quaternion.identity
        );

        Effects.MuzzleFlash(
            muzzle,
            new Vector2(direction, 0f),
            transform
        );

        EnemyBullet enemyBullet =
            bullet.GetComponent<EnemyBullet>();

        if (enemyBullet != null)
        {
            enemyBullet.SetDirection(direction);
        }

        Collider2D bulletCollider =
            bullet.GetComponent<Collider2D>();

        Collider2D enemyCollider =
            GetComponent<Collider2D>();

        if (bulletCollider != null &&
            enemyCollider != null)
        {
            Physics2D.IgnoreCollision(
                bulletCollider,
                enemyCollider
            );
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        HitFlash hitFlash = GetComponent<HitFlash>();

        if (hitFlash != null)
        {
            hitFlash.Flash();
        }

        Debug.Log(
            "🔴 Level 2 Enemy took damage! Health = " +
            health
        );

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(
            "💀 Level 2 Enemy DIED!"
        );

        EnemyKillManager killManager =
            FindAnyObjectByType<EnemyKillManager>();

        if (killManager != null)
        {
            killManager.EnemyKilled();
        }

        Effects.Explosion(transform.position);

        Destroy(gameObject);
    }
}