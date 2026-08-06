using UnityEngine;

// A trigger under the level (pits, the river). The player dies if
// they fall in; enemies and bullets that fall in are removed.
public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameOverManager gameOverManager =
                FindAnyObjectByType<GameOverManager>();

            if (gameOverManager != null)
            {
                gameOverManager.GameOver();
            }

            return;
        }

        if (other.attachedRigidbody != null)
        {
            Destroy(other.attachedRigidbody.gameObject);
        }
    }
}
