using UnityEngine;
using System.Collections;

// A lion under the General's mind-control collar. It stalks the
// player; when its health runs out the collar shatters, and the lion
// shakes its head and runs off, free. The lions are victims, so there
// is no explosion.
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    public float moveSpeed = 2f;

    // Stops this close to the player. Keep it under the distance at
    // which the two touch (about 0.65), or the lion stands next to
    // the player and never gets them.
    public float stopDistance = 0.5f;
    public int health = 1;

    [Header("Freed")]
    // The collar on its neck (optional); it vanishes when it breaks
    public Transform collar;
    public float runSpeed = 5f;

    // Removed once this far outside the camera view
    public float despawnMargin = 3f;

    private Transform player;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;

    // Freed or retreating: no longer an enemy
    private bool released = false;
    private bool running = false;
    private float runDirection;
    private Vector3 collarOffset;

    public bool IsAlive
    {
        get { return !released; }
    }

    // In Awake, so a lion that spawns on the same frame the stage
    // goal sends the pack running can already run
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (collar != null)
        {
            collarOffset = collar.localPosition;
        }
    }

    void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        // Already sent running before it got going
        if (released)
            return;

        // Lion naturally faces LEFT
        spriteRenderer.flipX = false;

        // A low growl as it arrives
        Sfx.Play("growl", Random.Range(0.9f, 1.15f));
    }

    void FixedUpdate()
    {
        if (released)
        {
            body.linearVelocity = new Vector2(
                running ? runDirection * runSpeed : 0f,
                body.linearVelocity.y
            );

            return;
        }

        if (player == null)
            return;

        float distance =
            Mathf.Abs(player.position.x - transform.position.x);

        if (distance <= stopDistance)
        {
            body.linearVelocity = new Vector2(
                0f,
                body.linearVelocity.y
            );

            return;
        }

        float direction = 0f;

        if (player.position.x < transform.position.x)
        {
            direction = -1f;
        }
        else if (player.position.x > transform.position.x)
        {
            direction = 1f;
        }

        body.linearVelocity = new Vector2(
            direction * moveSpeed,
            body.linearVelocity.y
        );

        // Lion naturally faces LEFT
        if (direction < 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (direction > 0)
        {
            spriteRenderer.flipX = true;
        }
    }

    void LateUpdate()
    {
        // The collar sits on the neck, which swaps sides when it turns
        if (collar != null)
        {
            Vector3 offset = collarOffset;

            if (spriteRenderer.flipX)
            {
                offset.x = -offset.x;
            }

            collar.localPosition = offset;
        }

        if (running && OutOfView())
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        if (released)
            return;

        health -= damage;

        HitFlash hitFlash = GetComponent<HitFlash>();

        if (hitFlash != null)
        {
            hitFlash.Flash();
        }

        Debug.Log(
            gameObject.name +
            " Health: " +
            health
        );

        if (health <= 0)
        {
            BreakCollar();
        }
    }

    // The stage goal is done: lions still around give up and run off
    public void Retreat()
    {
        if (released)
            return;

        Release();
        RunAway();
    }

    // Defeated: the collar shatters and the lion is free
    void BreakCollar()
    {
        Debug.Log("🦁 Collar broken! " + gameObject.name + " is free.");

        EnemyKillManager killManager =
            FindAnyObjectByType<EnemyKillManager>();

        if (killManager != null)
        {
            killManager.EnemyKilled();
        }
        else
        {
            Debug.LogError(
                "EnemyKillManager not found!"
            );
        }

        Vector3 neck = collar != null ?
            collar.position :
            transform.position + Vector3.up * 0.4f;

        Effects.CollarBreak(neck);

        if (collar != null)
        {
            collar.gameObject.SetActive(false);
        }

        Release();
        StartCoroutine(ShakeThenRun());
    }

    // No longer dangerous: it can't hurt the player, and it doesn't
    // block the player or the other lions
    void Release()
    {
        released = true;
        gameObject.tag = "Untagged";

        Collider2D mine = GetComponent<Collider2D>();

        if (mine == null)
            return;

        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();

            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(mine, playerCollider);
            }
        }

        foreach (Enemy other in FindObjectsByType<Enemy>())
        {
            Collider2D theirs = other.GetComponent<Collider2D>();

            if (other != this && theirs != null)
            {
                Physics2D.IgnoreCollision(mine, theirs);
            }
        }
    }

    IEnumerator ShakeThenRun()
    {
        // It shakes its head: left, right, left, right
        for (int i = 0; i < 4; i++)
        {
            spriteRenderer.flipX = !spriteRenderer.flipX;

            yield return new WaitForSeconds(0.12f);
        }

        RunAway();
    }

    void RunAway()
    {
        // Away from the player
        runDirection =
            player != null && player.position.x > transform.position.x ?
            -1f : 1f;

        // Lion naturally faces LEFT
        spriteRenderer.flipX = runDirection > 0;

        running = true;

        Effects.Dust(transform.position + Vector3.down * 0.6f);

        // Just in case it never leaves the screen
        Destroy(gameObject, 8f);
    }

    bool OutOfView()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return false;

        float halfWidth = cam.orthographicSize * cam.aspect;

        return Mathf.Abs(transform.position.x - cam.transform.position.x) >
            halfWidth + despawnMargin;
    }
}
