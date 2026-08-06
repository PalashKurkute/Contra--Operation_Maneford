using UnityEngine;

// The end of a stage (a trigger collider). It stays locked until
// the stage goal is reached; after that, walking into it clears
// the stage.
[RequireComponent(typeof(Collider2D))]
public class StageExit : MonoBehaviour
{
    private bool unlocked = false;

    public void Unlock()
    {
        unlocked = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryClear(other);
    }

    // Also catches a player who was already standing in the
    // exit when it unlocked
    private void OnTriggerStay2D(Collider2D other)
    {
        TryClear(other);
    }

    void TryClear(Collider2D other)
    {
        if (!unlocked || !other.CompareTag("Player"))
            return;

        if (StageManager.Instance != null)
        {
            StageManager.Instance.StageClear();
        }
    }
}
