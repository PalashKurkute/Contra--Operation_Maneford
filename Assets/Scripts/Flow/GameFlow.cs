using UnityEngine;

// The order the game's scenes are played in, grouped into levels.
// SceneFlow walks this list from top to bottom, so to change the
// story order, edit the asset (Assets/Resources/GameFlow).
[CreateAssetMenu(fileName = "GameFlow", menuName = "Contra/Game Flow")]
public class GameFlow : ScriptableObject
{
    public enum StepType
    {
        Video,
        Dialogue,
        Stage,
        Boss,
        Chase,
        Credits
    }

    [System.Serializable]
    public class Level
    {
        // Shown on the level card, e.g. "THE FOREST"
        public string title;
    }

    [System.Serializable]
    public class Step
    {
        public string sceneName;
        public StepType type;

        // Which level this scene belongs to (1, 2, 3...).
        // 0 = outside the levels (prologue and ending)
        public int level;

        // What happens in this scene. Shown on placeholder
        // screens until the real content exists.
        [TextArea(2, 5)]
        public string summary;
    }

    public string mainMenuScene = "MainMenu";

    public Level[] levels;
    public Step[] steps;

    // Position of a scene in the flow, or -1 if it isn't in it
    public int IndexOf(string sceneName)
    {
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].sceneName == sceneName)
                return i;
        }

        return -1;
    }

    // Position of the first scene of a level, or -1
    public int FirstStepOfLevel(int level)
    {
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].level == level)
                return i;
        }

        return -1;
    }

    public int LevelOf(int index)
    {
        if (index < 0 || index >= steps.Length)
            return 0;

        return steps[index].level;
    }

    public string LevelTitle(int level)
    {
        if (level < 1 || level > levels.Length)
            return "";

        return levels[level - 1].title;
    }

    // "LEVEL 2 - THE KING'S BASE", or "PROLOGUE" / "ENDING"
    // for the scenes before and after the levels
    public string SectionName(int index)
    {
        // Not in the flow (e.g. the main menu)
        if (index < 0 || index >= steps.Length)
            return "";

        int level = LevelOf(index);

        if (level > 0)
            return "LEVEL " + level + " - " + LevelTitle(level);

        return index < FirstStepOfLevel(1) ? "PROLOGUE" : "ENDING";
    }
}
