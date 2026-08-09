using UnityEngine;
using TMPro;

// Stand-in for a scene whose real content isn't built yet
// (video cutscenes, dialogue, boss fight, chase, credits).
// Shows what will happen here, then continues the game on Enter.
public class PlaceholderScene : MonoBehaviour
{
    public TMP_Text typeText;
    public TMP_Text sectionText;
    public TMP_Text titleText;
    public TMP_Text summaryText;

    void Start()
    {
        GameFlow.Step step = SceneFlow.CurrentStep;

        if (step == null)
        {
            typeText.text = "";
            sectionText.text = "";
            titleText.text = gameObject.scene.name;
            summaryText.text = "This scene is not in the GameFlow asset.";

            return;
        }

        typeText.text = TypeLabel(step.type);
        sectionText.text = SceneFlow.CurrentSectionName;
        titleText.text = step.sceneName.Replace('_', ' ').ToUpper();
        summaryText.text = step.summary;
    }

    void Update()
    {
        if (SceneFlow.IsTransitioning)
            return;

        if (MenuInput.Confirm())
        {
            Sfx.Play("menu_select");
            SceneFlow.Next();
        }
    }

    string TypeLabel(GameFlow.StepType type)
    {
        switch (type)
        {
            case GameFlow.StepType.Video: return "VIDEO CUTSCENE";
            case GameFlow.StepType.Dialogue: return "DIALOGUE";
            case GameFlow.StepType.Boss: return "BOSS FIGHT";
            case GameFlow.StepType.Chase: return "HELICOPTER CHASE";
            case GameFlow.StepType.Credits: return "CREDITS";
            default: return "STAGE";
        }
    }
}
