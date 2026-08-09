using UnityEngine;
using System.Collections;
using TMPro;

// Runs a playable stage's HUD and ending: shows the objective,
// points the way once the goal is done, and when the player
// reaches the exit shows STAGE CLEAR, then moves on to the next
// scene in the game flow.
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

    private bool cleared = false;

    // Once the stage is cleared the player can't die any more
    public static bool IsCleared
    {
        get { return Instance != null && Instance.cleared; }
    }

    void Awake()
    {
        Instance = this;
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

    public void StageClear()
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

        StartCoroutine(LeaveStage());
    }

    IEnumerator LeaveStage()
    {
        yield return new WaitForSeconds(clearTime);

        SceneFlow.Next();
    }
}
