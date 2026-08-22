using UnityEngine;
using System.Collections;

// The General's royal helicopter. It sways up and down on the right of
// the screen and attacks when the ChaseDirector says so: a volley of
// three rockets aimed at the player, or a wave of collared drones.
// Its health (BossHealth) drives the health bar and the story lines.
[RequireComponent(typeof(BossHealth))]
public class GeneralHelicopter : MonoBehaviour
{
    [Header("Flight (camera space)")]
    public Vector2 home = new Vector2(5.8f, 0.8f);
    public float swayHeight = 2.4f;
    public float swaySpeed = 0.8f;
    public float driftWidth = 0.9f;

    [Header("Attacks")]
    // Where rockets and drones go (the camera rig)
    public Transform rig;
    public ChaseRocket rocketPrefab;
    public ChaseDrone dronePrefab;
    public Transform rocketPort;
    public float rocketGap = 0.25f;
    public float warningTime = 0.5f;

    public SpriteRenderer spriteRenderer;

    private BossHealth health;
    private Transform player;
    private bool down;
    private float flightTime;

    private bool said60;
    private bool said30;
    private bool said20;

    public bool IsDown
    {
        get { return down; }
    }

    void Awake()
    {
        health = GetComponent<BossHealth>();
    }

    void Start()
    {
        PlayerHelicopter heli = FindAnyObjectByType<PlayerHelicopter>();

        if (heli != null)
        {
            player = heli.transform;
        }

        health.OnHealthChanged += OnHealthChanged;
    }

    void OnDestroy()
    {
        health.OnHealthChanged -= OnHealthChanged;
    }

    void Update()
    {
        if (down)
            return;

        flightTime += Time.deltaTime;

        transform.localPosition = new Vector3(
            home.x + Mathf.Sin(flightTime * swaySpeed * 0.5f) * driftWidth,
            home.y + Mathf.Sin(flightTime * swaySpeed) * swayHeight,
            transform.localPosition.z
        );
    }

    // The General's lines at 60%, 30% and (Rook's slip) 20% health
    void OnHealthChanged(float fraction)
    {
        if (!said60 && fraction <= 0.6f)
        {
            said60 = true;
            StoryEvents.Raise("general_60");
        }

        if (!said30 && fraction <= 0.3f)
        {
            said30 = true;
            StoryEvents.Raise("general_30");
        }

        if (!said20 && fraction <= 0.2f)
        {
            said20 = true;
            StoryEvents.Raise("general_20");
        }
    }

    public IEnumerator FireRockets(int count)
    {
        // The tell: a flash and a beep
        Sfx.Play("warning");

        for (float t = 0f; t < warningTime; t += Time.deltaTime)
        {
            spriteRenderer.color = Mathf.Repeat(t, 0.12f) < 0.06f ?
                new Color(1f, 0.6f, 0.5f) : Color.white;

            yield return null;
        }

        spriteRenderer.color = Color.white;

        for (int i = 0; i < count && !down; i++)
        {
            ChaseRocket rocket = Instantiate(rocketPrefab, rig);
            rocket.transform.position = rocketPort.position;

            Vector2 aim = player != null ?
                (Vector2)(player.position - rocketPort.position) :
                Vector2.left;

            rocket.Launch(aim);

            Sfx.Play("missile");

            yield return new WaitForSeconds(rocketGap);
        }
    }

    public void LaunchDrones(int count)
    {
        for (int i = 0; i < count; i++)
        {
            ChaseDrone drone = Instantiate(dronePrefab, rig);

            float height = count > 1 ?
                Mathf.Lerp(-3f, 3f, (float)i / (count - 1)) :
                0f;

            drone.transform.localPosition = new Vector3(10f + i * 1.2f, height, 0f);
        }

        Sfx.Play("growl", 1.6f, 0.5f);
    }

    // Out of fuel: spin out and fall off the screen
    public IEnumerator SpinOut()
    {
        down = true;
        health.invulnerable = true;

        ChaseHitbox box = GetComponent<ChaseHitbox>();

        if (box != null)
        {
            box.harmful = false;
            box.shootable = false;
        }

        Sfx.Play("heli_down");

        Vector3 velocity = new Vector3(2.5f, -1.2f, 0f);
        float spin = 0f;

        for (float t = 0f; t < 3f; t += Time.deltaTime)
        {
            velocity.y -= 4f * Time.deltaTime;
            spin += 540f * Time.deltaTime;

            transform.localPosition += velocity * Time.deltaTime;
            transform.localRotation = Quaternion.Euler(0f, 0f, spin);

            if (Mathf.Repeat(t, 0.15f) < Time.deltaTime)
            {
                Effects.Dust(transform.position);
            }

            if (Mathf.Repeat(t, 0.45f) < Time.deltaTime)
            {
                Effects.Explosion(transform.position);
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }
}
