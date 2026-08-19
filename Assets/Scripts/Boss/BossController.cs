using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// The Mecha-Lion. A state machine (Intro, Phase1, Phase2, Dead) that
// picks telegraphed attacks at random, never the same one more than
// twice in a row:
//   Pounce    crouch (the tell), leap onto the player, shockwaves on landing
//   Missiles  a fan of missiles from the mane
//   Charge    (phase two) flashes for a second, then leaps across the
//             whole arena and lands with a big shockwave
// At half health the armour blows off and it gets faster. When it dies:
// explosions, a one-second freeze, the defeat line, then STAGE CLEAR.
//
// The sprites face left. The root sits at the boss's feet.
[RequireComponent(typeof(BossHealth))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    public enum State
    {
        Intro,
        Phase1,
        Phase2,
        Dead
    }

    enum Attack
    {
        Pounce,
        Missiles,
        Charge
    }

    [Header("Parts")]
    // Holds the sprites; squashes when crouching (pivot at the feet)
    public Transform visual;
    public SpriteRenderer head;
    public Sprite headNormal;
    public Sprite headRoar;
    public SpriteRenderer[] parts;
    public Transform missileMouth;
    public BossHealthBar healthBar;

    [Header("Arena")]
    // How far left and right the boss's feet may go
    public float leftX = 45.5f;
    public float rightX = 54.5f;
    public float groundY = -0.07f;

    // Shockwaves die beyond these
    public float arenaLeft = 40f;
    public float arenaRight = 60f;

    [Header("Prefabs")]
    public BossMissile missilePrefab;
    public BossShockwave shockwavePrefab;
    public Sprite armorSprite;

    [Header("Pounce")]
    public float crouchTime = 0.7f;
    public float pounceTime = 0.9f;
    public float pounceHeight = 4.5f;

    [Header("Missiles")]
    public int missiles = 3;
    public int missilesPhaseTwo = 5;
    public float missileSpread = 40f;
    public float missileSpeed = 7f;

    [Header("Charge")]
    public float chargeWarning = 1f;
    public float chargeTime = 1.3f;
    public float chargeHeight = 6.5f;

    [Header("Shockwave")]
    public float shockwaveSpeed = 8f;

    // Half the boss's width, where shockwaves start
    public float halfWidth = 2.5f;

    [Header("Pacing")]
    // Rest between attacks
    public float restTime = 1.4f;

    // Phase two runs this much faster
    public float phaseTwoSpeed = 1.3f;

    public State CurrentState { get; private set; }

    private BossHealth health;
    private Rigidbody2D body;
    private Transform player;
    private CameraFollow cameraFollow;

    private Vector3 visualScale;
    private bool telegraphing;

    private Attack lastAttack;
    private int repeats;
    private bool phaseTwoPending;
    private bool firstMissiles = true;
    private bool firstCharge = true;
    private Coroutine brain;

    float Speed
    {
        get { return CurrentState == State.Phase2 ? phaseTwoSpeed : 1f; }
    }

    // =========================
    // SETUP
    // =========================

    void Awake()
    {
        health = GetComponent<BossHealth>();
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;

        visualScale = visual.localScale;
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        cameraFollow = FindAnyObjectByType<CameraFollow>();

        health.OnPhaseChange += OnPhaseChange;
        health.OnDeath += OnDeath;

        brain = StartCoroutine(Run());
    }

    void OnDestroy()
    {
        health.OnPhaseChange -= OnPhaseChange;
        health.OnDeath -= OnDeath;
    }

    // =========================
    // THE FIGHT
    // =========================

    IEnumerator Run()
    {
        CurrentState = State.Intro;
        health.invulnerable = true;

        // Wait out the fade-in
        while (SceneFlow.IsTransitioning)
            yield return null;

        // Stomp in from the right
        Vector2 home = new Vector2(rightX, groundY);
        body.position = home + Vector2.right * 9f;
        Face(-1f);

        yield return Walk(home, 1.8f);

        // "Roar, my beauty! ROAR!" (the line plays the roar sound)
        StoryEvents.Raise("boss_start");

        yield return new WaitForSeconds(1.3f);
        yield return Roar(1.2f);

        if (healthBar != null)
        {
            healthBar.Show();
        }

        health.invulnerable = false;
        CurrentState = State.Phase1;

        while (CurrentState != State.Dead)
        {
            if (phaseTwoPending)
            {
                phaseTwoPending = false;
                yield return Roar(1f);
            }

            Attack attack = ChooseAttack();

            if (attack == Attack.Pounce)
            {
                yield return Pounce();
            }
            else if (attack == Attack.Missiles)
            {
                yield return Missiles();
            }
            else
            {
                yield return Charge();
            }

            yield return Rest(restTime / Speed);
        }
    }

    Attack ChooseAttack()
    {
        List<Attack> choices = new List<Attack> { Attack.Pounce, Attack.Missiles };

        if (CurrentState == State.Phase2)
        {
            choices.Add(Attack.Charge);
        }

        // Never the same attack three times in a row
        if (repeats >= 2)
        {
            choices.Remove(lastAttack);
        }

        Attack attack = choices[Random.Range(0, choices.Count)];

        repeats = attack == lastAttack ? repeats + 1 : 1;
        lastAttack = attack;

        return attack;
    }

    // =========================
    // ATTACKS
    // =========================

    IEnumerator Pounce()
    {
        FacePlayer();
        Sfx.Play("warning");

        // The tell: a low crouch
        yield return Crouch(crouchTime / Speed);

        float targetX = player != null ?
            Mathf.Clamp(player.position.x, leftX, rightX) :
            body.position.x;

        Sfx.Play("whoosh", 1.2f, 0.6f);

        yield return Leap(targetX, pounceTime / Speed, pounceHeight);
        yield return Land(1f);
    }

    IEnumerator Missiles()
    {
        FacePlayer();

        if (firstMissiles)
        {
            firstMissiles = false;

            // "Behold the Royal Missile Mane!"
            StoryEvents.Raise("boss_missiles");
        }

        // The tell: the mane flashes
        head.sprite = headRoar;
        yield return Telegraph(0.5f / Speed, false);

        int count = CurrentState == State.Phase2 ? missilesPhaseTwo : missiles;
        float spread = CurrentState == State.Phase2 ? missileSpread * 1.4f : missileSpread;

        Vector3 target = player != null ?
            player.position + Vector3.up * 0.6f :
            missileMouth.position + Vector3.left * 5f;

        Vector2 aim = target - missileMouth.position;
        float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) - 0.5f : 0f;
            float angle = (baseAngle + spread * t) * Mathf.Deg2Rad;

            BossMissile missile = Instantiate(
                missilePrefab,
                missileMouth.position,
                Quaternion.identity
            );

            missile.Launch(
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                missileSpeed * Speed,
                groundY
            );

            Sfx.Play("missile");

            yield return new WaitForSeconds(0.07f);
        }

        yield return new WaitForSeconds(0.4f);

        head.sprite = headNormal;
    }

    IEnumerator Charge()
    {
        if (firstCharge)
        {
            firstCharge = false;

            // "It's charging! Jump the shockwave!"
            StoryEvents.Raise("boss_charge");
        }

        // Head for the far side of the arena
        bool onRight = body.position.x > (leftX + rightX) * 0.5f;
        float targetX = onRight ? leftX : rightX;

        Face(onRight ? -1f : 1f);
        head.sprite = headRoar;

        // The tell: a full second of flashing
        yield return Telegraph(chargeWarning, true);

        Sfx.Play("whoosh");
        Sfx.Play("growl", 0.7f);

        yield return Leap(targetX, chargeTime / Speed, chargeHeight);
        yield return Land(1.4f);

        head.sprite = headNormal;
    }

    // =========================
    // PHASES AND DEATH
    // =========================

    void OnPhaseChange(int phase)
    {
        CurrentState = State.Phase2;
        phaseTwoPending = true;

        // "Not the mane! I just had it polished!"
        StoryEvents.Raise("boss_phase2");

        Sfx.Play("alarm");
        Sfx.Play("explosion");
        Shake(0.45f, 0.6f);

        // Armour plates blow off, sparks fly
        for (int i = 0; i < 6; i++)
        {
            Vector3 from = transform.position +
                new Vector3(Random.Range(-halfWidth, halfWidth), Random.Range(1f, 3.2f), 0f);

            Debris.Throw(
                armorSprite,
                from,
                new Vector2(Random.Range(-5f, 5f), Random.Range(5f, 9f)),
                Random.Range(-600f, 600f),
                "Player",
                30
            );

            Effects.Impact(from);
        }
    }

    void OnDeath()
    {
        if (CurrentState == State.Dead)
            return;

        CurrentState = State.Dead;

        if (brain != null)
        {
            StopCoroutine(brain);
        }

        StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        health.invulnerable = true;
        telegraphing = false;
        head.sprite = headRoar;
        visual.localScale = visualScale;

        // It can't hurt anyone any more
        foreach (Collider2D part in GetComponentsInChildren<Collider2D>())
        {
            part.gameObject.tag = "Untagged";
        }

        foreach (BossMissile missile in FindObjectsByType<BossMissile>())
        {
            Destroy(missile.gameObject);
        }

        foreach (BossShockwave wave in FindObjectsByType<BossShockwave>())
        {
            Destroy(wave.gameObject);
        }

        Sfx.Play("big_explosion");

        // A chain of explosions over the body
        for (int i = 0; i < 8; i++)
        {
            Effects.Explosion(transform.position + new Vector3(
                Random.Range(-halfWidth, halfWidth),
                Random.Range(0.5f, 3.5f),
                0f
            ));

            yield return new WaitForSeconds(0.16f);
        }

        // One second of frozen time
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(1f);

        Time.timeScale = 1f;

        // A smoking wreck
        foreach (SpriteRenderer part in parts)
        {
            part.color = new Color(0.35f, 0.32f, 0.32f);
        }

        // "NOOOO! Four defense budgets!"
        StoryEvents.Raise("boss_defeated");

        yield return new WaitForSeconds(4f);

        if (StageManager.Instance != null)
        {
            StageManager.Instance.StageClear();
        }
        else
        {
            SceneFlow.Next();
        }
    }

    // =========================
    // MOVES
    // =========================

    IEnumerator Walk(Vector2 to, float seconds)
    {
        Vector2 from = body.position;

        for (float t = 0f; t < seconds; t += Time.fixedDeltaTime)
        {
            body.MovePosition(Vector2.Lerp(from, to, t / seconds));

            // Heavy steps
            float step = Mathf.Abs(Mathf.Sin(t * 7f));
            visual.localScale = Vector3.Scale(visualScale, new Vector3(1f, 1f - 0.04f * step, 1f));

            if (Mathf.Repeat(t, Mathf.PI / 7f) < Time.fixedDeltaTime)
            {
                Sfx.Play("thud", 1.3f, 0.5f);
                Shake(0.08f, 0.15f);
            }

            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(to);
        visual.localScale = visualScale;
    }

    IEnumerator Leap(float targetX, float seconds, float height)
    {
        Vector2 from = body.position;
        Vector2 to = new Vector2(targetX, groundY);

        for (float t = 0f; t < seconds; t += Time.fixedDeltaTime)
        {
            float k = t / seconds;
            Vector2 position = Vector2.Lerp(from, to, k);
            position.y += 4f * height * k * (1f - k);

            body.MovePosition(position);

            // Stretch while rising
            visual.localScale = Vector3.Scale(visualScale, new Vector3(1f, k < 0.5f ? 1.08f : 1f, 1f));

            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(to);
    }

    IEnumerator Land(float strength)
    {
        Sfx.Play("thud");
        Shake(0.35f * strength, 0.4f);

        Vector3 feet = new Vector3(body.position.x, groundY, 0f);

        Effects.Dust(feet + Vector3.left * halfWidth);
        Effects.Dust(feet + Vector3.right * halfWidth);

        // Shockwaves roll out both ways
        SpawnShockwave(feet + Vector3.left * halfWidth, -1f, shockwaveSpeed * strength);
        SpawnShockwave(feet + Vector3.right * halfWidth, 1f, shockwaveSpeed * strength);

        // Squash on impact
        visual.localScale = Vector3.Scale(visualScale, new Vector3(1.05f, 0.8f, 1f));

        yield return new WaitForSeconds(0.15f);

        visual.localScale = visualScale;

        FacePlayer();
    }

    void SpawnShockwave(Vector3 position, float direction, float speed)
    {
        BossShockwave wave = Instantiate(shockwavePrefab, position, Quaternion.identity);
        wave.Launch(direction, speed * Speed, arenaLeft, arenaRight);
    }

    IEnumerator Crouch(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float squash = Mathf.Lerp(1f, 0.78f, Mathf.Clamp01(t / 0.2f));

            // Trembling, about to spring
            float tremble = Mathf.Sin(t * 60f) * 0.02f;

            visual.localScale = Vector3.Scale(visualScale, new Vector3(1f + tremble, squash, 1f));

            yield return null;
        }
    }

    IEnumerator Roar(float seconds)
    {
        head.sprite = headRoar;
        Shake(0.3f, seconds);

        yield return new WaitForSeconds(seconds);

        head.sprite = headNormal;
    }

    IEnumerator Rest(float seconds)
    {
        FacePlayer();

        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            // Breathing
            float breath = Mathf.Sin(t * 4f) * 0.02f;
            visual.localScale = Vector3.Scale(visualScale, new Vector3(1f, 1f + breath, 1f));

            yield return null;
        }

        visual.localScale = visualScale;
    }

    // Flashes the parts (red for the charge) so the attack can be read
    IEnumerator Telegraph(float seconds, bool danger)
    {
        telegraphing = true;

        Color flash = danger ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 0.95f, 0.6f);
        float beep = 0f;

        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            bool on = Mathf.Repeat(t, 0.16f) < 0.08f;

            foreach (SpriteRenderer part in parts)
            {
                part.color = on ? flash : Color.white;
            }

            if (danger && t >= beep)
            {
                Sfx.Play("warning");
                beep += 0.5f;
            }

            yield return null;
        }

        telegraphing = false;
    }

    // =========================
    // HELPERS
    // =========================

    void Update()
    {
        if (telegraphing || CurrentState == State.Dead || parts == null)
            return;

        // Phase two: the mane is on fire
        Color tint = Color.white;

        if (CurrentState == State.Phase2)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
            tint = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.35f), pulse);
        }

        head.color = tint;

        foreach (SpriteRenderer part in parts)
        {
            if (part != head)
            {
                part.color = Color.white;
            }
        }
    }

    void FacePlayer()
    {
        if (player != null)
        {
            Face(player.position.x < body.position.x ? -1f : 1f);
        }
    }

    // -1 = facing left (as drawn), 1 = facing right (mirrored)
    void Face(float direction)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (direction < 0f ? 1f : -1f);
        transform.localScale = scale;
    }

    void Shake(float amount, float seconds)
    {
        if (cameraFollow != null)
        {
            cameraFollow.Shake(amount, seconds);
        }
    }
}
