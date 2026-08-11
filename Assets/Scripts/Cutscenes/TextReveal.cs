using UnityEngine;
using TMPro;

// Types a text out letter by letter as "progress" goes from 0
// to 1. Animate progress from a Timeline; it also previews
// in the Editor while scrubbing.
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TextReveal : MonoBehaviour
{
    [Range(0f, 1f)]
    public float progress = 1f;

    private TMP_Text text;

    void LateUpdate()
    {
        Apply();
    }

    // Called after an Animation track writes "progress"
    void OnDidApplyAnimationProperties()
    {
        Apply();
    }

    void Apply()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();

        int letters = text.GetParsedText().Length;

        text.maxVisibleCharacters = Mathf.RoundToInt(progress * letters);
    }
}
