using UnityEngine;

// The stage goal: defeat enough enemies. Once enough are down it
// opens the way forward (raises the bridge, opens the stage gate),
// stops the spawner and unlocks the stage exit. With clearOnGoal
// the stage ends right there instead, straight into its cutscene.
public class EnemyKillManager : MonoBehaviour
{
    [Header("Kill Settings")]
    public int enemiesRequired = 20;

    // The final kill clears the stage on the spot (slow motion,
    // STAGE CLEAR, then the cutscene) instead of opening the way
    // to the exit
    public bool clearOnGoal = false;

    // HUD label for the counter, e.g. "LIONS FREED"
    public string objectiveLabel = "ENEMIES";

    [Header("Bridge")]
    // Hidden until the goal is reached (optional)
    public GameObject bridge;

    [Header("Enemy Spawner")]
    public EnemySpawner enemySpawner;

    // After this many are down, a big pack charges in at once
    // (0 = no pack). Raises the story event "pack".
    public int packAfterKills = 0;
    public int packSize = 3;

    [Header("Way Forward")]
    // Its gate opens when the goal is reached (optional)
    public StageBounds stageBounds;

    // Unlocks when the goal is reached
    public StageExit stageExit;

    private int enemiesKilled = 0;

    private bool goalReached = false;

    void Start()
    {
        // Close bridge at the beginning
        if (bridge != null)
        {
            bridge.SetActive(false);
        }

        UpdateHud();
    }

    public void EnemyKilled()
    {
        // Don't count anything after the goal is reached
        if (goalReached)
            return;

        enemiesKilled++;

        Debug.Log(
            "🔥 Enemies Killed: " +
            enemiesKilled +
            "/" +
            enemiesRequired
        );

        UpdateHud();

        // Overlay dialogue listens for these (kills_1, kills_2...)
        StoryEvents.Raise("kills_" + enemiesKilled);

        if (enemiesKilled >= enemiesRequired)
        {
            ReachGoal();
        }
        else if (enemiesKilled == packAfterKills && enemySpawner != null)
        {
            enemySpawner.SpawnPack(packSize);

            StoryEvents.Raise("pack");
        }
    }

    void ReachGoal()
    {
        goalReached = true;

        Debug.Log("✅ Stage goal reached!");

        if (clearOnGoal)
        {
            ClearOnTheSpot();
            return;
        }

        // Open bridge
        if (bridge != null)
        {
            bridge.SetActive(true);

            Sfx.Play("bridge");

            Debug.Log(
                "🌉 BRIDGE OPENED!"
            );
        }

        if (stageBounds != null)
        {
            stageBounds.OpenGate();
        }

        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }

        // Lions still around give up and run off
        Enemy[] enemies =
            FindObjectsByType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            enemy.Retreat();
        }

        if (stageExit != null)
        {
            stageExit.Unlock();
        }
        else
        {
            Debug.LogError(
                "❌ Stage Exit is NOT assigned!"
            );
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.ShowGo();
        }

        StoryEvents.Raise("goal");
    }

    void ClearOnTheSpot()
    {
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }

        // The rest of the pack scatters while the last one goes free
        foreach (Enemy enemy in FindObjectsByType<Enemy>())
        {
            enemy.Retreat();
        }

        StoryEvents.Raise("goal");

        if (StageManager.Instance != null)
        {
            StageManager.Instance.StageClear(true);
        }
        else
        {
            SceneFlow.Next();
        }
    }

    void UpdateHud()
    {
        if (StageManager.Instance == null)
            return;

        StageManager.Instance.SetObjective(
            objectiveLabel + "  " +
            Mathf.Min(enemiesKilled, enemiesRequired) +
            "/" +
            enemiesRequired
        );
    }
}
