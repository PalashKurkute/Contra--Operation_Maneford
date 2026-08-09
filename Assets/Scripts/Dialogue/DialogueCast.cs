using UnityEngine;

// Everyone who can speak in dialogue: their name, colour, voice and
// portraits. Lines refer to speakers by id (e.g. "PIP").
[CreateAssetMenu(fileName = "Cast", menuName = "Contra/Dialogue/Cast")]
public class DialogueCast : ScriptableObject
{
    [System.Serializable]
    public class Portrait
    {
        public DialogueEmotion emotion;
        public Sprite sprite;
    }

    [System.Serializable]
    public class Speaker
    {
        public string id;

        // Shown on the name plate
        public string displayName;
        public Color nameColor = Color.white;

        // Their usual side of the screen
        public DialogueSide side = DialogueSide.Right;

        // Their lines come over the radio unless a line says otherwise
        public bool radio;

        // Pitch of their typing blips (1 = normal, higher = squeakier)
        public float voicePitch = 1f;

        // How fast their text types out (1 = normal)
        public float talkSpeed = 1f;

        // One per emotion; a missing emotion falls back to Neutral
        public Portrait[] portraits;

        public Sprite GetPortrait(DialogueEmotion emotion)
        {
            if (portraits == null || portraits.Length == 0)
                return null;

            foreach (Portrait portrait in portraits)
            {
                if (portrait.emotion == emotion && portrait.sprite != null)
                    return portrait.sprite;
            }

            foreach (Portrait portrait in portraits)
            {
                if (portrait.emotion == DialogueEmotion.Neutral && portrait.sprite != null)
                    return portrait.sprite;
            }

            return portraits[0].sprite;
        }
    }

    public Speaker[] speakers;

    public Speaker Find(string id)
    {
        foreach (Speaker speaker in speakers)
        {
            if (speaker.id == id)
                return speaker;
        }

        Debug.LogWarning("⚠️ No speaker '" + id + "' in the dialogue cast");

        return null;
    }
}
