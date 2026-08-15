using UnityEngine;
using System.Collections;

// Plays overlay dialogue (DialogueOverlay) when a story event happens,
// e.g. "kills_1" or "goal". Use the event name "start" to play it when
// the scene starts. See StoryEvents for the names the game raises.
public class OverlayTrigger : MonoBehaviour
{
    public DialogueSequence lines;

    // "start", or a StoryEvents name
    public string eventName = "start";

    // Seconds of game time to wait first
    public float delay = 0f;

    // Only ever play once
    public bool once = true;

    private bool fired;

    void OnEnable()
    {
        StoryEvents.Raised += OnStoryEvent;
    }

    void OnDisable()
    {
        StoryEvents.Raised -= OnStoryEvent;
    }

    void Start()
    {
        if (eventName == "start")
        {
            Fire();
        }
    }

    void OnStoryEvent(string raised)
    {
        if (raised == eventName)
        {
            Fire();
        }
    }

    void Fire()
    {
        if (once && fired)
            return;

        fired = true;

        StartCoroutine(PlayAfterDelay());
    }

    IEnumerator PlayAfterDelay()
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        DialogueOverlay.Play(lines);
    }
}
