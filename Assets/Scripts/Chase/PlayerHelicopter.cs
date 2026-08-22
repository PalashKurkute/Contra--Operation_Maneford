using UnityEngine;
using UnityEngine.InputSystem;
using System;

// The player's (borrowed) helicopter in the chase. Moves anywhere on
// the left part of the screen with WASD / arrows and fires its
// machine gun while J is held. Lives in the camera's space: it's a
// child of the camera rig, so it keeps up with the scrolling.
public class PlayerHelicopter : MonoBehaviour
{
    public float speed = 6f;

    // Movement limits, in camera space
    public Vector2 minPosition = new Vector2(-8f, -4.2f);
    public Vector2 maxPosition = new Vector2(2.5f, 3.6f);

    [Header("Gun")]
    public ChaseBullet bulletPrefab;
    public Transform muzzle;
    public float fireInterval = 0.11f;

    [Header("Health")]
    public int maxHealth = 5;
    public float invulnerableTime = 1.5f;
    public float radius = 0.55f;

    public SpriteRenderer spriteRenderer;

    public event Action<int> OnHealthChanged;

    public int Health { get; private set; }

    private float fireTimer;
    private float invulnerableLeft;
    private bool dead;
    private bool controlsLocked;

    void Awake()
    {
        Health = maxHealth;
    }

    void Update()
    {
        if (dead)
            return;

        bool hasControl = !controlsLocked && Time.timeScale > 0f;
        Keyboard keys = Keyboard.current;

        Vector2 input = Vector2.zero;

        if (hasControl && keys != null)
        {
            if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) input.x -= 1f;
            if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) input.x += 1f;
            if (keys.sKey.isPressed || keys.downArrowKey.isPressed) input.y -= 1f;
            if (keys.wKey.isPressed || keys.upArrowKey.isPressed) input.y += 1f;
        }

        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 p = transform.localPosition + (Vector3)(input * speed * Time.deltaTime);
        p.x = Mathf.Clamp(p.x, minPosition.x, maxPosition.x);
        p.y = Mathf.Clamp(p.y, minPosition.y, maxPosition.y);
        transform.localPosition = p;

        // Lean into the movement
        transform.localRotation = Quaternion.Euler(0f, 0f, -input.x * 8f);

        // Machine gun while J is held
        fireTimer -= Time.deltaTime;

        if (hasControl && keys != null && keys.jKey.isPressed && fireTimer <= 0f)
        {
            Fire();
            fireTimer = fireInterval;
        }

        // Blink while invulnerable (not after the chase is won)
        if (controlsLocked)
        {
            spriteRenderer.enabled = true;
        }
        else if (invulnerableLeft > 0f)
        {
            invulnerableLeft -= Time.deltaTime;
            spriteRenderer.enabled = invulnerableLeft <= 0f || Mathf.Repeat(Time.time, 0.12f) < 0.06f;
        }
        else
        {
            CheckHits();
        }
    }

    void Fire()
    {
        ChaseBullet bullet = Instantiate(bulletPrefab, transform.parent);
        bullet.transform.position = muzzle.position;

        Effects.MuzzleFlash(muzzle.position, Vector2.right, transform);
        Sfx.Play("mg");
    }

    void CheckHits()
    {
        Vector2 me = transform.position;

        for (int i = ChaseHitbox.All.Count - 1; i >= 0; i--)
        {
            ChaseHitbox box = ChaseHitbox.All[i];

            if (!box.harmful || !box.Overlaps(me, radius))
                continue;

            if (box.destroyOnContact)
            {
                Effects.Explosion(box.Center);
                Destroy(box.gameObject);
            }

            TakeHit();
            return;
        }
    }

    public void TakeHit()
    {
        if (dead || controlsLocked || invulnerableLeft > 0f)
            return;

        Health--;

        Sfx.Play("explosion", 1.3f, 0.7f);
        Effects.Impact(transform.position);

        CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();

        if (cameraFollow != null)
        {
            cameraFollow.Shake(0.25f, 0.3f);
        }

        if (OnHealthChanged != null)
        {
            OnHealthChanged(Health);
        }

        if (Health <= 0)
        {
            dead = true;
            spriteRenderer.enabled = false;

            Effects.Explosion(transform.position);

            GameOverManager gameOverManager = FindAnyObjectByType<GameOverManager>();

            if (gameOverManager != null)
            {
                gameOverManager.GameOver();
            }

            return;
        }

        invulnerableLeft = invulnerableTime;
    }

    // The chase is won: stop flying and shooting
    public void LockControls()
    {
        controlsLocked = true;
        spriteRenderer.enabled = true;
    }
}
