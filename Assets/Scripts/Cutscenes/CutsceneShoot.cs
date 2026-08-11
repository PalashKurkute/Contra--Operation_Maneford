using UnityEngine;

// Goes in a cutscene's shoot scene (the scene the video is
// recorded from, under Assets/Cutscenes). It is not part of the
// game's flow, so this keeps the game's own systems out of the shot.
public class CutsceneShoot : MonoBehaviour
{
    void Awake()
    {
        // Keep rendering while the Editor is in the background,
        // or the Recorder stalls when another window has focus
        Application.runInBackground = true;

        // SceneFlow creates itself in every scene; its fade and
        // F9 skip don't belong in the recording
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.gameObject.SetActive(false);
    }
}
