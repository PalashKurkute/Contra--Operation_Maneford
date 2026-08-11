using UnityEngine;
using UnityEngine.Video;

// One video cutscene: the clip, its subtitles, and a stand-in
// (a still image with text) for while the real clip doesn't
// exist yet. Played by VideoCutscene.
[CreateAssetMenu(fileName = "Cutscene", menuName = "Contra/Cutscene")]
public class CutsceneData : ScriptableObject
{
    [System.Serializable]
    public class Subtitle
    {
        // Seconds from the start of the cutscene
        public float start;
        public float end;

        // Shown before the line in the speaker's color, e.g. "PIP"
        public string speaker;
        public Color speakerColor = Color.white;

        [TextArea(1, 3)]
        public string text;
    }

    // Leave empty to show the placeholder instead
    public VideoClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    public Subtitle[] subtitles;

    [Header("Placeholder (used while there is no clip)")]
    public Sprite placeholderImage;

    // What happens in the cutscene, shown on the placeholder
    [TextArea(2, 6)]
    public string placeholderText;

    // Seconds the placeholder stays up before moving on
    public float placeholderLength = 8f;
}
