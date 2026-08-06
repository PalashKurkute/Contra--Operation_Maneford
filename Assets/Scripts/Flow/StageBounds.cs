using UnityEngine;

// Keeps the player between the stage's left and right edges,
// like the edges of the screen in Contra. Until the stage goal is
// reached the right edge stops at the gate (e.g. the river bank
// before the bridge is up). Enemies and bullets are not affected.
//
// Runs after PlayerMovement so it can correct this step's velocity.
[DefaultExecutionOrder(50)]
public class StageBounds : MonoBehaviour
{
    public float minX = -7f;
    public float maxX = 66f;

    // Right edge until the goal is reached.
    // Set it to maxX (or more) for no gate.
    public float gateX = 66f;

    private Rigidbody2D player;
    private bool gateOpen = false;

    void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.GetComponent<Rigidbody2D>();
        }
    }

    public void OpenGate()
    {
        gateOpen = true;
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        float rightEdge = gateOpen ? maxX : Mathf.Min(gateX, maxX);

        Vector2 velocity = player.linearVelocity;

        float nextX = player.position.x +
            velocity.x * Time.fixedDeltaTime;

        float clampedX = Mathf.Clamp(nextX, minX, rightEdge);

        if (clampedX != nextX)
        {
            // Stop exactly at the edge instead of walking past it
            velocity.x = (clampedX - player.position.x) /
                Time.fixedDeltaTime;

            player.linearVelocity = velocity;
        }
    }

    // Draw the edges (cyan) and the gate (yellow) in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        DrawEdge(minX);
        DrawEdge(maxX);

        if (gateX < maxX)
        {
            Gizmos.color = Color.yellow;
            DrawEdge(gateX);
        }
    }

    void DrawEdge(float x)
    {
        Gizmos.DrawLine(
            new Vector3(x, -6f, 0f),
            new Vector3(x, 8f, 0f)
        );
    }
}
