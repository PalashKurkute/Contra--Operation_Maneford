using UnityEngine;
using System.Collections;

// Runs the helicopter chase: scrolls the world, makes the General
// alternate rocket volleys and drone waves (faster as he weakens),
// shows the player's health, and when the General's health runs out
// plays the finale: slow motion, he spins out, STAGE CLEAR.
//
// The camera follows cameraTarget, which this moves at scrollSpeed,
// so the parallax forest rushes past underneath.
public class ChaseDirector : MonoBehaviour
{
    public Transform cameraTarget;
    public float scrollSpeed = 4f;

    public PlayerHelicopter player;
    public GeneralHelicopter general;
    public BossHealth generalHealth;
    public BossHealthBar healthBar;

    [Header("Attacks")]
    public float firstAttackDelay = 4f;

    // Gap between attacks at full health; it shrinks to 60% near the end
    public float attackGap = 3.5f;
    public int rockets = 3;
    public int minDrones = 2;
    public int maxDrones = 3;

    [Header("Finale")]
    public float slowMotionScale = 0.3f;
    public float slowMotionTime = 1.1f;

    private bool over;

    IEnumerator Start()
    {
        player.OnHealthChanged += UpdateHud;
        generalHealth.OnDeath += OnGeneralDown;
        generalHealth.invulnerable = true;

        UpdateHud(player.Health);

        while (SceneFlow.IsTransitioning)
            yield return null;

        if (healthBar != null)
        {
            healthBar.Show();
        }

        generalHealth.invulnerable = false;

        yield return new WaitForSeconds(firstAttackDelay);

        bool rocketTurn = true;
        bool firstDrones = true;

        while (!over)
        {
            if (rocketTurn)
            {
                yield return general.FireRockets(rockets);
            }
            else
            {
                if (firstDrones)
                {
                    firstDrones = false;

                    // "Drones! Six o'clock!"
                    StoryEvents.Raise("chase_drones");
                }

                general.LaunchDrones(Random.Range(minDrones, maxDrones + 1));
            }

            rocketTurn = !rocketTurn;

            float gap = Mathf.Lerp(attackGap * 0.6f, attackGap, generalHealth.Fraction);

            yield return new WaitForSeconds(gap);
        }
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnHealthChanged -= UpdateHud;
        }

        if (generalHealth != null)
        {
            generalHealth.OnDeath -= OnGeneralDown;
        }
    }

    void Update()
    {
        if (cameraTarget != null)
        {
            cameraTarget.position += Vector3.right * scrollSpeed * Time.deltaTime;
        }
    }

    void UpdateHud(int health)
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.SetObjective(
                "HELI  " + new string('♥', Mathf.Max(0, health))
            );
        }
    }

    void OnGeneralDown()
    {
        if (over)
            return;

        over = true;
        StartCoroutine(Finale());
    }

    IEnumerator Finale()
    {
        player.LockControls();

        // Clear the sky
        foreach (ChaseRocket rocket in FindObjectsByType<ChaseRocket>())
        {
            rocket.TakeDamage(99);
        }

        foreach (ChaseDrone drone in FindObjectsByType<ChaseDrone>())
        {
            drone.TakeDamage(99);
        }

        // "The fuel gauge is BROKEN! This helicopter was a RENTAL!"
        StoryEvents.Raise("general_down");

        StartCoroutine(general.SpinOut());

        // A moment of slow motion as he goes down
        Time.timeScale = slowMotionScale;

        yield return new WaitForSecondsRealtime(slowMotionTime);

        Time.timeScale = 1f;

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
}
