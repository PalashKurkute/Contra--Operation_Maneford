using UnityEngine;
using TMPro;

// Title screen: start the game, jump to a level the player has
// already reached, or quit. Up/Down (or W/S) to choose, Enter or
// J to confirm.
public class MainMenu : MonoBehaviour
{
    public TMP_Text menuText;

    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.84f, 0.29f);
    public Color lockedColor = new Color(0.45f, 0.45f, 0.45f);

    // Items: START, one per level, QUIT
    private int selected = 0;
    private int levelCount;

    int ItemCount
    {
        get { return levelCount + 2; }
    }

    void Start()
    {
        levelCount = SceneFlow.LevelCount;

        Refresh();
    }

    void Update()
    {
        if (SceneFlow.IsTransitioning)
            return;

        if (MenuInput.Up())
        {
            Move(-1);
        }
        else if (MenuInput.Down())
        {
            Move(1);
        }
        else if (MenuInput.Confirm())
        {
            Choose();
        }
    }

    void Move(int direction)
    {
        // Skip levels that are still locked
        do
        {
            selected = (selected + direction + ItemCount) % ItemCount;
        }
        while (IsLocked(selected));

        Sfx.Play("menu_move");

        Refresh();
    }

    bool IsLevel(int item)
    {
        return item >= 1 && item <= levelCount;
    }

    bool IsLocked(int item)
    {
        return IsLevel(item) && item > SceneFlow.LevelReached;
    }

    void Choose()
    {
        Sfx.Play("menu_select");

        if (selected == 0)
        {
            SceneFlow.StartGame();
        }
        else if (IsLevel(selected))
        {
            SceneFlow.StartLevel(selected);
        }
        else
        {
            Quit();
        }
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Refresh()
    {
        string text = "";

        for (int item = 0; item < ItemCount; item++)
        {
            string label;

            if (item == 0)
            {
                label = "START GAME";
            }
            else if (IsLevel(item))
            {
                label = "LEVEL " + item + "  " +
                    (IsLocked(item) ? "LOCKED" : SceneFlow.LevelTitle(item));
            }
            else
            {
                label = "QUIT";
            }

            Color color = item == selected ? selectedColor :
                IsLocked(item) ? lockedColor :
                normalColor;

            string cursor = item == selected ? "> " : "  ";

            text += "<color=#" + ColorUtility.ToHtmlStringRGB(color) +
                ">" + cursor + label + "</color>\n";
        }

        menuText.text = text;
    }
}
