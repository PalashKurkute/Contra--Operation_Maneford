using UnityEngine;
using UnityEngine.UI;

// The player's hits in the boss fight. Everywhere else one touch ends
// the game; against the Mecha-Lion the player can take a few, shown as
// medals top-left. After a hit the player blinks and can't be hurt for
// a moment.
//
// Boss attacks call PlayerHits.Hurt(). With no PlayerHits in the scene
// that's game over at once, as before.
public class PlayerHits : MonoBehaviour
{
    public int hits = 3;

    // How long the player blinks and can't be hurt after a hit
    public float safeTime = 1.6f;

    // One per hit, left to right; lost ones go dark
    public Image[] medals;

    private static PlayerHits instance;

    private int hitsLeft;
    private float safeUntil;
    private bool blinking;
    private Transform player;
    private SpriteRenderer playerSprite;
    private CameraFollow cameraFollow;

    void Awake()
    {
        instance = this;
        hitsLeft = hits;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerSprite = playerObject.GetComponent<SpriteRenderer>();
        }

        cameraFollow = FindAnyObjectByType<CameraFollow>();

        ShowMedals();
    }

    // Returns false if the player couldn't be hurt (still blinking),
    // so a missile can fly on through
    public static bool Hurt()
    {
        if (instance == null)
        {
            GameOver();
            return true;
        }

        return instance.TakeHit();
    }

    bool TakeHit()
    {
        if (hitsLeft <= 0 || Time.time < safeUntil || StageManager.IsCleared)
            return false;

        hitsLeft--;
        ShowMedals();

        if (hitsLeft == 0)
        {
            GameOver();
            return true;
        }

        safeUntil = Time.time + safeTime;

        Sfx.Play("hit", 0.7f);

        if (player != null)
        {
            Effects.Impact(player.position);
        }

        if (cameraFollow != null)
        {
            cameraFollow.Shake(0.25f, 0.3f);
        }

        return true;
    }

    void Update()
    {
        if (playerSprite == null)
            return;

        bool safe = Time.time < safeUntil;

        if (safe)
        {
            // Classic NES blink
            playerSprite.enabled = Mathf.Repeat(Time.time, 0.12f) < 0.07f;
            blinking = true;
        }
        else if (blinking)
        {
            playerSprite.enabled = true;
            blinking = false;
        }
    }

    void ShowMedals()
    {
        for (int i = 0; i < medals.Length; i++)
        {
            if (medals[i] == null)
                continue;

            medals[i].gameObject.SetActive(i < hits);
            medals[i].color = i < hitsLeft ?
                Color.white :
                new Color(0.25f, 0.25f, 0.3f, 0.6f);
        }
    }

    static void GameOver()
    {
        GameOverManager gameOverManager = FindAnyObjectByType<GameOverManager>();

        if (gameOverManager != null)
        {
            gameOverManager.GameOver();
        }
    }
}
