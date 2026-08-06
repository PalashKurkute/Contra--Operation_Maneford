using UnityEngine;

// The stage goal: defeat enough enemies. Once enough are down it
// opens the way forward (raises the bridge, opens the stage gate),
// stops the spawner and unlocks the stage exit.
public class EnemyKillManager : MonoBehaviour
{
    [Header("Kill Settings")]
    public int enemiesRequired = 20;

    // HUD label for the counter, e.g. "LIONS FREED"
    public string objectiveLabel = "ENEMIES";

    [Header("Bridge")]
    // Hidden until the goal is reached (optional)
    public GameObject bridge;

    [Header("Enemy Spawner")]
    public EnemySpawner enemySpawner;

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

        if (enemiesKilled >= enemiesRequired)
        {
            ReachGoal();
        }
    }

    void ReachGoal()
    {
        goalReached = true;

        Debug.Log("✅ Stage goal reached!");

        // Open bridge
        if (bridge != null)
        {
            bridge.SetActive(true);

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

        // Lions still on screen stop chasing
        Enemy[] enemies =
            FindObjectsByType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            enemy.StopEnemy();
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
