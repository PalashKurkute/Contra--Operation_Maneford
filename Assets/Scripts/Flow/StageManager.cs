using UnityEngine;
using System.Collections;
using TMPro;

// Runs a playable stage's HUD and ending: shows the objective,
// points the way once the goal is done, and when the player
// reaches the exit shows STAGE CLEAR, then moves on to the next
// scene in the game flow. A stage can also clear on the final hit
// itself (EnemyKillManager.clearOnGoal), with no exit to reach.
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("HUD")]
    public TMP_Text levelText;
    public TMP_Text objectiveText;
    public GameObject goPrompt;
    public GameObject stageClearBanner;

    [Header("Stage Clear")]
    // How long STAGE CLEAR stays up before the next scene
    public float clearTime = 3f;

    [Header("Clear On The Final Hit")]
    // A stage that ends the moment its goal is done (no exit to
    // walk to) holds STAGE CLEAR this long, in real seconds, then
    // goes straight on to its cutscene
    public float finalHitClearTime = 1.8f;

    // The final hit plays in slow motion for a moment
    public float finalHitSlowMotion = 0.3f;
    public float finalHitSlowTime = 0.7f;

    private bool cleared = false;
    private float normalFixedDeltaTime;

    // Once the stage is cleared the player can't die any more
    public static bool IsCleared
    {
        get { return Instance != null && Instance.cleared; }
    }

    void Awake()
    {
        Instance = this;
        normalFixedDeltaTime = Time.fixedDeltaTime;
    }

    // Leaving mid slow motion (a skip) must not keep physics slow
    void OnDestroy()
    {
        Time.fixedDeltaTime = normalFixedDeltaTime;
    }

    void Start()
    {
        int level = SceneFlow.CurrentLevel;

        levelText.text = level > 0 ?
            "LEVEL " + level + "  " + SceneFlow.LevelTitle(level) :
            "";

        goPrompt.SetActive(false);
        stageClearBanner.SetActive(false);
    }

    public void SetObjective(string text)
    {
        objectiveText.text = text;
    }

    // Blinking "GO" arrow: the way forward is open
    public void ShowGo()
    {
        if (!cleared)
        {
            goPrompt.SetActive(true);

            Sfx.Play("go");
        }
    }

    // The player reached the exit
    public void StageClear()
    {
        StageClear(false);
    }

    // finalHit: the goal itself ended the stage, so the final hit
    // gets a moment of slow motion and the cutscene follows sooner
    public void StageClear(bool finalHit)
    {
        if (cleared)
            return;

        cleared = true;

        Debug.Log("🏁 STAGE CLEAR!");

        goPrompt.SetActive(false);
        stageClearBanner.SetActive(true);

        Sfx.Play("stage_clear");

        PlayerMovement player =
            FindAnyObjectByType<PlayerMovement>();

        if (player != null)
        {
            player.LockControls();
        }

        EnemySpawner spawner =
            FindAnyObjectByType<EnemySpawner>();

        if (spawner != null)
        {
            spawner.StopSpawning();
        }

        if (finalHit)
        {
            StartCoroutine(LeaveOnFinalHit());
        }
        else
        {
            StartCoroutine(LeaveStage());
        }
    }

    IEnumerator LeaveStage()
    {
        yield return new WaitForSeconds(clearTime);

        SceneFlow.Next();
    }

    IEnumerator LeaveOnFinalHit()
    {
        SetTimeScale(finalHitSlowMotion);

        yield return new WaitForSecondsRealtime(finalHitSlowTime);

        // Ease back to full speed
        float ease = 0.3f;

        for (float t = 0f; t < ease; t += Time.unscaledDeltaTime)
        {
            SetTimeScale(Mathf.Lerp(finalHitSlowMotion, 1f, t / ease));

            yield return null;
        }

        SetTimeScale(1f);

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, finalHitClearTime - finalHitSlowTime - ease)
        );

        SceneFlow.Next();
    }

    // Physics steps scale with it, so slow motion stays smooth
    void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = normalFixedDeltaTime * scale;
    }
}
