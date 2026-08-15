using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;

// In-level dialogue that never stops the game: a small box at the
// bottom of the screen with a little portrait. Lines queue up, type
// out, stay for a moment, then fade away.
//
// Call DialogueOverlay.Play(sequence) from anywhere. The overlay is
// created from Resources/DialogueOverlay the first time it's needed
// and belongs to the current scene. It runs on game time, so it waits
// while the game is paused (game over, level card).
public class DialogueOverlay : MonoBehaviour
{
    public DialogueCast cast;

    [Header("Box")]
    public CanvasGroup box;
    public UIShake shaker;
    public Image portrait;
    public GameObject portraitRadio;
    public RadioStatic boxStatic;
    public TMP_Text nameText;
    public TMP_Text bodyText;

    [Header("Timing")]
    public float charactersPerSecond = 45f;

    // How long a finished line stays before the next one
    public float holdTime = 2f;
    public float fadeTime = 0.25f;

    [Header("Voice")]
    public int blipEvery = 3;
    public float blipVolume = 0.6f;

    private static DialogueOverlay instance;

    private Queue<DialogueLine> queue = new Queue<DialogueLine>();
    private List<DialogueText.Command> commands = new List<DialogueText.Command>();
    private bool playing;
    private int blips;

    // =========================
    // PUBLIC API
    // =========================

    public static void Play(DialogueSequence sequence)
    {
        if (sequence == null || sequence.lines == null)
            return;

        DialogueOverlay overlay = Get();

        if (overlay == null)
            return;

        foreach (DialogueLine line in sequence.lines)
        {
            overlay.queue.Enqueue(line);
        }

        if (!overlay.playing)
        {
            overlay.StartCoroutine(overlay.PlayQueue());
        }
    }

    // True while lines are showing or waiting
    public static bool IsBusy
    {
        get { return instance != null && instance.playing; }
    }

    static DialogueOverlay Get()
    {
        if (instance != null)
            return instance;

        DialogueOverlay prefab =
            Resources.Load<DialogueOverlay>("DialogueOverlay");

        if (prefab == null)
        {
            Debug.LogError("❌ Resources/DialogueOverlay prefab is missing!");
            return null;
        }

        instance = Instantiate(prefab);
        instance.name = "DialogueOverlay";

        return instance;
    }

    void Awake()
    {
        instance = this;
        box.alpha = 0f;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // =========================
    // PLAYBACK
    // =========================

    IEnumerator PlayQueue()
    {
        playing = true;

        while (queue.Count > 0)
        {
            DialogueLine line = queue.Dequeue();

            ShowSpeaker(line);

            if (box.alpha < 1f)
            {
                yield return Fade(1f);
            }

            yield return TypeLine(line);

            yield return new WaitForSeconds(holdTime);

            if (queue.Count == 0)
            {
                yield return Fade(0f);
            }
        }

        playing = false;
    }

    void ShowSpeaker(DialogueLine line)
    {
        DialogueCast.Speaker speaker = cast.Find(line.speakerId);

        bool intercom = line.radio == DialogueRadio.Intercom;

        bool radio = intercom || line.radio == DialogueRadio.On ||
            (line.radio == DialogueRadio.Auto && speaker != null && speaker.radio);

        nameText.text = (speaker != null ? speaker.displayName : line.speakerId) +
            (intercom ? DialoguePlayer.IntercomTag : radio ? DialoguePlayer.RadioTag : "");
        nameText.color = speaker != null ? speaker.nameColor : Color.white;

        Sprite face = speaker != null ? speaker.GetPortrait(line.emotion) : null;
        portrait.sprite = face;
        portrait.enabled = face != null;

        portraitRadio.SetActive(radio && face != null);
        boxStatic.gameObject.SetActive(radio);

        if (radio)
        {
            boxStatic.Burst(0.3f);
            Sfx.Play("radio_static", 1f, 0.6f);
        }
    }

    IEnumerator TypeLine(DialogueLine line)
    {
        DialogueCast.Speaker speaker = cast.Find(line.speakerId);

        float speed = charactersPerSecond * (speaker != null ? speaker.talkSpeed : 1f);
        float pitch = speaker != null ? speaker.voicePitch : 1f;

        bodyText.text = DialogueText.Parse(line.text, commands);
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        int total = bodyText.textInfo.characterCount;
        int nextCommand = 0;
        float debt = 0f;

        for (int shown = 0; shown <= total; shown++)
        {
            while (nextCommand < commands.Count &&
                commands[nextCommand].index <= shown)
            {
                yield return RunCommand(commands[nextCommand]);
                nextCommand++;
            }

            if (shown == total)
                break;

            bodyText.maxVisibleCharacters = shown + 1;

            char letter = bodyText.textInfo.characterInfo[shown].character;

            if (char.IsLetterOrDigit(letter) && blips++ % blipEvery == 0)
            {
                Sfx.Play("blip", pitch * Random.Range(0.97f, 1.03f), blipVolume);
            }

            // Frame-rate independent typing
            debt += 1f / speed;

            while (debt > 0f)
            {
                yield return null;
                debt -= Time.deltaTime;
            }
        }

        bodyText.maxVisibleCharacters = total;
    }

    IEnumerator RunCommand(DialogueText.Command command)
    {
        switch (command.name)
        {
            case "pause":
                float seconds;

                if (!float.TryParse(
                    command.value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out seconds))
                {
                    seconds = 0.5f;
                }

                yield return new WaitForSeconds(seconds);
                break;

            case "shake":
                shaker.Shake(12f, 0.35f);

                CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();

                if (cameraFollow != null)
                {
                    cameraFollow.Shake(0.3f, 0.4f);
                }

                Sfx.Play("thud");
                break;

            case "sfx":
                Sfx.Play(command.value);
                break;

            default:
                Debug.LogWarning(
                    "⚠️ Unknown dialogue command {" + command.name + "}"
                );
                break;
        }
    }

    IEnumerator Fade(float target)
    {
        float start = box.alpha;

        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            box.alpha = Mathf.Lerp(start, target, t / fadeTime);
            yield return null;
        }

        box.alpha = target;
    }
}
