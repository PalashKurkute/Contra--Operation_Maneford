using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;

    // Ignore keys for a moment so a shot fired while dying
    // doesn't restart the stage straight away
    public float inputDelay = 0.75f;

    private bool isGameOver = false;
    private float gameOverTime;

    void Start()
    {
        gameOverPanel.SetActive(false);
    }

    public void GameOver()
    {
        // Already over, or the stage was cleared
        if (isGameOver || StageManager.IsCleared)
            return;

        isGameOver = true;
        gameOverTime = Time.unscaledTime;

        gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    void Update()
    {
        if (!isGameOver || SceneFlow.IsTransitioning)
            return;

        if (Time.unscaledTime < gameOverTime + inputDelay)
            return;

        // Enter retries the stage, Escape quits to the menu
        if (MenuInput.Confirm())
        {
            RestartGame();
        }
        else if (MenuInput.Back())
        {
            SceneFlow.LoadMainMenu();
        }
    }

    public void RestartGame()
    {
        SceneFlow.Restart();
    }
}
