using UnityEngine;

// One dialogue scene's script: where it happens, its lines in order,
// and (optionally) which scene comes after it.
[CreateAssetMenu(fileName = "Dialogue", menuName = "Contra/Dialogue/Sequence")]
public class DialogueSequence : ScriptableObject
{
    // Stamped at the top of the screen when the scene starts,
    // e.g. "FOREST EDGE - DUSK"
    public string location;

    // Full-screen picture behind the dialogue. Leave it empty to
    // see the scene's own backdrop instead.
    public Sprite background;

    public DialogueLine[] lines;

    // Scene to load when the dialogue ends. Leave it empty for the
    // next scene in the game flow (the GameFlow asset).
    public string nextSceneName;
}
