using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy")]
    public GameObject enemyPrefab;

    [Header("Spawn Points")]
    // A random one is picked each time (never one that is
    // too close to the player)
    public Transform[] spawnPoints;

    [Header("Spawn Settings")]
    public float spawnDelay = 5f;
    public int maxEnemies = 5;
    public float minimumSpawnDistance = 8f;

    // Spawn points closer than this to the screen edge (or on
    // screen) are skipped, so enemies never pop in where the
    // player can see them
    public float offscreenMargin = 1.5f;

    private float timer = 0f;
    private bool stopped = false;

    // Enemies from this spawner that are still alive
    private List<GameObject> alive = new List<GameObject>();

    void Update()
    {
        if (stopped)
            return;

        timer += Time.deltaTime;

        if (timer < spawnDelay)
            return;

        timer = 0f;

        SpawnEnemy();
    }

    // A big wave all at once, from several spawn points
    // (it ignores maxEnemies)
    public void SpawnPack(int count)
    {
        Debug.Log("🦁 A pack of " + count + " is coming!");

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(true);
        }
    }

    void SpawnEnemy(bool ignoreLimit = false)
    {
        // Forget enemies that have been destroyed
        alive.RemoveAll(enemy => enemy == null);

        if (!ignoreLimit && alive.Count >= maxEnemies)
            return;

        // Check prefab
        if (enemyPrefab == null)
        {
            Debug.LogError(
                "❌ Enemy prefab is NOT assigned!"
            );

            return;
        }

        // Find player
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogError(
                "❌ Player with tag 'Player' not found!"
            );

            return;
        }

        // Only spawn points far enough from the player
        List<Transform> choices = new List<Transform>();

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            float distance = Vector2.Distance(
                point.position,
                playerObject.transform.position
            );

            if (distance >= minimumSpawnDistance && OffScreen(point.position))
            {
                choices.Add(point);
            }
        }

        if (choices.Count == 0)
        {
            Debug.Log(
                "⚠️ Every spawn point is too close to the player or on screen"
            );

            return;
        }

        Transform spawnPoint =
            choices[Random.Range(0, choices.Count)];

        // Spread a pack out so they don't stack on one spot
        // (outwards, so none of them lands on screen)
        Vector3 position = spawnPoint.position;

        if (ignoreLimit)
        {
            position.x += Random.Range(0f, 1.2f) * SideOfScreen(position);
        }

        // Spawn
        GameObject newEnemy = Instantiate(
            enemyPrefab,
            position,
            Quaternion.identity
        );

        alive.Add(newEnemy);

        Debug.Log(
            "🟢 ENEMY SPAWNED: " +
            newEnemy.name +
            " at " +
            spawnPoint.name
        );
    }

    bool OffScreen(Vector3 position)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return true;

        float halfWidth = cam.orthographicSize * cam.aspect;

        return Mathf.Abs(position.x - cam.transform.position.x) >=
            halfWidth + offscreenMargin;
    }

    // -1 left of the camera, 1 right of it
    float SideOfScreen(Vector3 position)
    {
        Camera cam = Camera.main;

        if (cam == null)
            return 1f;

        return position.x < cam.transform.position.x ? -1f : 1f;
    }

    public void StopSpawning()
    {
        if (stopped)
            return;

        stopped = true;

        Debug.Log(
            "🛑 Enemy spawning stopped."
        );
    }
}
