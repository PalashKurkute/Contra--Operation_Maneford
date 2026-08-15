using UnityEngine;

public enum DialogueEmotion
{
    Neutral,
    Angry,
    Smug,
    Surprised,
    Panic,
    Soft
}

public enum DialogueSide
{
    // Where the speaker already is, else their usual side
    Auto,
    Left,
    Right
}

public enum DialogueRadio
{
    // The speaker's usual setting (Pip is always on the radio)
    Auto,
    On,
    Off,

    // Over the base loudspeakers: static and a [PA] tag
    Intercom
}

// One line of dialogue.
//
// The text can use TextMeshPro rich text (<b>, <i>, <color=#FFD700>)
// and these commands:
//   {pause=0.5}   wait half a second (just {pause} waits 0.5 s)
//   {shake}       shake the screen
//   {sfx=roar}    play a sound from the SoundLibrary
[System.Serializable]
public class DialogueLine
{
    public string speakerId = "ROOK";
    public DialogueEmotion emotion;

    [TextArea(2, 4)]
    public string text;

    public DialogueSide side;
    public DialogueRadio radio;
}
