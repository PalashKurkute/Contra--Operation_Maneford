using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    // Values of the Animator "pose" parameter
    enum Pose
    {
        Idle,
        Run,
        Jump,
        AimUp,
        RunAimUp,
        RunAimDown,
        Prone
    }

    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    public GameObject bulletPrefab;

    // Where bullets spawn for each pose, in local units
    // while facing right (mirrored when facing left)
    [Header("Muzzle Offsets")]
    public Vector2 muzzleStand = new Vector2(0.13f, 0.025f);
    public Vector2 muzzleRun = new Vector2(0.13f, 0.045f);
    public Vector2 muzzleUp = new Vector2(0.025f, 0.205f);
    public Vector2 muzzleRunUp = new Vector2(0.085f, 0.145f);
    public Vector2 muzzleRunDown = new Vector2(0.085f, -0.055f);
    public Vector2 muzzleProne = new Vector2(0.115f, -0.055f);
    public float muzzleAirDistance = 0.12f;

    [Header("Prone / Drop")]
    // Hitbox height while kneeling (local units)
    public float proneHeight = 0.2f;

    // How far below us another platform must be
    // before Down + Jump lets us drop
    public float dropCheckDistance = 10f;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private BoxCollider2D hitbox;

    private Vector2 standSize;
    private Vector2 standOffset;

    private bool isGrounded;
    private bool isDropping;
    private bool isProne;

    // Set when the stage is cleared: the player stops obeying keys
    private bool controlsLocked;

    private float moveInput;
    private float facing = 1f;
    private Vector2 aim = Vector2.right;
    private Pose pose;

    private ContactFilter2D groundFilter;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        hitbox = GetComponent<BoxCollider2D>();

        standSize = hitbox.size;
        standOffset = hitbox.offset;

        facing = spriteRenderer.flipX ? -1f : 1f;

        // Ground = a solid contact whose normal points up
        groundFilter.useNormalAngle = true;
        groundFilter.minNormalAngle = 45f;
        groundFilter.maxNormalAngle = 135f;
    }

    void Update()
    {
        bool wasGrounded = isGrounded;

        // Ground check (re-evaluated every frame, so walking
        // off a ledge clears it)
        isGrounded = !isDropping &&
            body.linearVelocity.y <= 0.1f &&
            body.IsTouching(groundFilter);

        // Landing puff
        if (isGrounded && !wasGrounded)
        {
            Effects.Dust(FeetPosition());
            Sfx.Play("land");
        }

        float move = 0f;

        // Movement
        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            move = -1f;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            move = 1f;
        }

        bool up =
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed;

        bool down =
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed;

        // No control while the game is paused (game over, level
        // card) or after the stage is cleared
        bool hasControl = !controlsLocked && Time.timeScale > 0f;

        if (!hasControl)
        {
            move = 0f;
            up = false;
            down = false;
        }

        moveInput = move;

        // Flip
        if (move != 0)
        {
            facing = move;
            spriteRenderer.flipX = facing < 0;
        }

        // Jump, or Down + Jump to drop through the platform
        if (hasControl &&
            Keyboard.current.spaceKey.wasPressedThisFrame &&
            isGrounded)
        {
            if (down)
            {
                TryDropDown();
            }
            else
            {
                body.linearVelocity = new Vector2(
                    body.linearVelocity.x,
                    jumpForce
                );

                isGrounded = false;

                Effects.Dust(FeetPosition());
                Sfx.Play("jump");
            }
        }

        UpdateAim(move, up, down);

        SetProne(pose == Pose.Prone);

        animator.SetInteger("pose", (int)pose);

        // Shoot
        if (hasControl &&
            Keyboard.current.jKey.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    public void LockControls()
    {
        controlsLocked = true;
    }

    void FixedUpdate()
    {
        if (isDropping)
            return;

        body.linearVelocity = new Vector2(
            moveInput * moveSpeed,
            body.linearVelocity.y
        );
    }

    // =========================
    // AIM
    // =========================

    // NES Contra rules: on the ground, Up alone aims straight
    // up, Down alone lies prone, and Up/Down while running aims
    // diagonally. In the air all 8 directions work.
    void UpdateAim(float move, bool up, bool down)
    {
        float vertical = up ? 1f : down ? -1f : 0f;

        if (!isGrounded)
        {
            pose = Pose.Jump;

            aim = new Vector2(move, vertical);

            if (aim == Vector2.zero)
            {
                aim.x = facing;
            }
        }
        else if (move == 0)
        {
            pose = up ? Pose.AimUp :
                down ? Pose.Prone :
                Pose.Idle;

            aim = up ? Vector2.up : new Vector2(facing, 0f);
        }
        else
        {
            pose = up ? Pose.RunAimUp :
                down ? Pose.RunAimDown :
                Pose.Run;

            aim = new Vector2(facing, vertical);
        }

        aim.Normalize();
    }

    // Muzzle position for the current pose, facing right
    Vector2 MuzzleOffset()
    {
        switch (pose)
        {
            case Pose.Run: return muzzleRun;
            case Pose.AimUp: return muzzleUp;
            case Pose.RunAimUp: return muzzleRunUp;
            case Pose.RunAimDown: return muzzleRunDown;
            case Pose.Prone: return muzzleProne;

            // Somersaulting: fire from the middle of the ball
            case Pose.Jump:
                return new Vector2(Mathf.Abs(aim.x), aim.y) *
                    muzzleAirDistance;

            default: return muzzleStand;
        }
    }

    // Bottom-centre of the standing hitbox, in world space
    Vector3 FeetPosition()
    {
        return transform.TransformPoint(
            new Vector2(0f, standOffset.y - standSize.y / 2f)
        );
    }

    // =========================
    // PRONE
    // =========================

    // Lying down shrinks the hitbox (feet stay put) so
    // bullets aimed at a standing player fly overhead
    void SetProne(bool prone)
    {
        if (prone == isProne)
            return;

        isProne = prone;

        if (prone)
        {
            float bottom = standOffset.y - standSize.y / 2f;

            hitbox.size = new Vector2(standSize.x, proneHeight);
            hitbox.offset = new Vector2(
                standOffset.x,
                bottom + proneHeight / 2f
            );
        }
        else
        {
            hitbox.size = standSize;
            hitbox.offset = standOffset;
        }
    }

    // =========================
    // DROP DOWN
    // =========================

    void TryDropDown()
    {
        Collider2D platform = FindPlatformUnderFeet();

        // Never drop through the lowest ground into the void
        if (platform == null || !HasPlatformBelow(platform))
            return;

        StartCoroutine(DropDown(platform));
    }

    Collider2D FindPlatformUnderFeet()
    {
        Collider2D[] allColliders =
            FindObjectsByType<Collider2D>();

        Collider2D platform = null;

        float closestDistance = Mathf.Infinity;

        foreach (Collider2D col in allColliders)
        {
            if (!col.CompareTag("Platform"))
                continue;

            // Check if this platform is underneath the player
            if (col.bounds.max.y <= transform.position.y + 0.2f &&
                col.bounds.max.y >= transform.position.y - 1f)
            {
                float distance = Mathf.Abs(
                    col.bounds.max.y - transform.position.y
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    platform = col;
                }
            }
        }

        return platform;
    }

    bool HasPlatformBelow(Collider2D platform)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            transform.position,
            Vector2.down,
            dropCheckDistance
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != platform &&
                hit.collider.CompareTag("Platform"))
            {
                return true;
            }
        }

        return false;
    }

    IEnumerator DropDown(Collider2D platform)
    {
        isDropping = true;
        isGrounded = false;

        // Ignore ONLY the platform we're standing on
        Physics2D.IgnoreCollision(hitbox, platform, true);

        // Move downward
        body.linearVelocity = new Vector2(
            body.linearVelocity.x,
            -5f
        );

        // Wait until player has moved below the platform
        // (give up after a second if something stops us)
        float startY = transform.position.y;
        float giveUpTime = Time.time + 1f;

        while (transform.position.y > startY - 1f &&
            Time.time < giveUpTime)
        {
            yield return null;
        }

        // Wait a little extra so we are completely below it
        yield return new WaitForSeconds(0.1f);

        // Re-enable collision
        Physics2D.IgnoreCollision(hitbox, platform, false);

        isDropping = false;
    }

    // =========================
    // SHOOT
    // =========================

    void Shoot()
    {
        if (bulletPrefab == null)
            return;

        Vector2 offset = MuzzleOffset();
        offset.x *= facing;

        Vector3 muzzle = transform.TransformPoint(offset);

        GameObject bullet = Instantiate(
            bulletPrefab,
            muzzle,
            Quaternion.identity
        );

        Effects.MuzzleFlash(muzzle, aim, transform);
        Sfx.Play("shoot");

        Bullet bulletScript =
            bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.SetDirection(aim);
        }

        Collider2D bulletCollider =
            bullet.GetComponent<Collider2D>();

        if (bulletCollider != null)
        {
            Physics2D.IgnoreCollision(
                bulletCollider,
                hitbox
            );
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            GameOverManager gameOverManager =
                FindAnyObjectByType<GameOverManager>();

            if (gameOverManager != null)
            {
                gameOverManager.GameOver();
            }
        }
    }
}
