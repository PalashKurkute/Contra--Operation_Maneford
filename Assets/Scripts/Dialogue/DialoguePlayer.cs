using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;

// Plays a DialogueSequence on the DialogueScene prefab: types each line
// out with the speaker's voice blips, animates the portraits, runs the
// inline commands ({pause=0.5}, {shake}, {sfx=roar}) and moves on to
// the next scene at the end.
//
// Enter, Space or J: show the whole line, then go to the next one.
// Hold Enter: skip the whole scene.
public class DialoguePlayer : MonoBehaviour
{
    public DialogueSequence sequence;
    public DialogueCast cast;

    [Header("Screen")]
    public Image background;
    public UIShake shaker;
    public TMP_Text locationText;
    public DialoguePortrait leftPortrait;
    public DialoguePortrait rightPortrait;

    [Header("Text Box")]
    public CanvasGroup box;
    public RectTransform namePlate;
    public TMP_Text nameText;
    public RadioStatic boxStatic;
    public TMP_Text bodyText;
    public GameObject continueIcon;

    [Header("Skip")]
    public CanvasGroup skipHint;
    public Image skipBar;

    // Seconds Enter must be held to skip the scene
    public float holdToSkip = 1f;

    [Header("Typing")]
    public float charactersPerSecond = 40f;

    // One voice blip every this many letters
    public int blipEvery = 2;

    // Extra wait after . ! ? and after , ; :
    public float sentencePause = 0.18f;
    public float commaPause = 0.07f;

    [Header("Shake")]
    public float shakeStrength = 18f;
    public float shakeTime = 0.4f;

    public const string RadioTag =
        " <size=70%><color=#22D3EE>[RADIO]</color></size>";

    public const string IntercomTag =
        " <size=70%><color=#B8BCC4>[PA]</color></size>";

    private List<DialogueText.Command> commands =
        new List<DialogueText.Command>();

    private bool pressed;      // advance was pressed this frame
    private bool rushing;      // show the rest of this line at once
    private bool skipping;     // skip the whole scene
    private bool finished;

    // Typing time still owed; carries over between letters so the
    // speed doesn't depend on the frame rate
    private float pauseDebt;

    private float skipHeld;
    private bool skipArmed;
    private int blips;
    private float namePlateInset;

    // =========================
    // FLOW
    // =========================

    IEnumerator Start()
    {
        box.alpha = 0f;
        continueIcon.SetActive(false);
        boxStatic.gameObject.SetActive(false);
        locationText.text = "";
        skipBar.fillAmount = 0f;
        namePlateInset = namePlate.anchoredPosition.x;

        if (sequence == null || cast == null)
        {
            Debug.LogError(
                "❌ DialoguePlayer needs a Sequence and a Cast!"
            );

            yield break;
        }

        background.sprite = sequence.background;
        background.enabled = sequence.background != null;

        // Wait for the fade-in (and any level card) to finish
        while (SceneFlow.IsTransitioning)
            yield return null;

        if (!string.IsNullOrEmpty(sequence.location))
        {
            StartCoroutine(ShowLocation(sequence.location));
        }

        yield return Wait(0.5f);
        yield return Fade(box, 1f, 0.2f);

        foreach (DialogueLine line in sequence.lines)
        {
            if (skipping)
                break;

            yield return PlayLine(line);
        }

        yield return End();
    }

    void Update()
    {
        if (finished)
            return;

        // Hold Enter to skip the whole scene. The hold only counts from
        // a fresh press after the scene has appeared, so holding Enter
        // through a skipped cutscene doesn't skip this dialogue too.
        bool holding = MenuInput.SkipHeld();

        if (!holding && !SceneFlow.IsTransitioning)
        {
            skipArmed = true;
        }

        if (holding && skipArmed)
        {
            skipHeld += Time.unscaledDeltaTime;
        }
        else
        {
            skipHeld = Mathf.Max(0f, skipHeld - Time.unscaledDeltaTime * 3f);
        }

        skipBar.fillAmount = Mathf.Clamp01(skipHeld / holdToSkip);

        skipHint.alpha = Mathf.MoveTowards(
            skipHint.alpha,
            skipHeld > 0.15f ? 1f : 0.5f,
            Time.unscaledDeltaTime * 4f
        );

        if (!skipping && skipHeld >= holdToSkip)
        {
            skipping = true;

            Sfx.Play("menu_select");
        }

        if (!SceneFlow.IsTransitioning && MenuInput.Confirm())
        {
            pressed = true;
        }
    }

    IEnumerator End()
    {
        finished = true;

        continueIcon.SetActive(false);

        if (!skipping)
        {
            yield return Fade(box, 0f, 0.2f);
        }

        while (SceneFlow.IsTransitioning)
            yield return null;

        if (!string.IsNullOrEmpty(sequence.nextSceneName))
        {
            SceneFlow.LoadScene(sequence.nextSceneName);
        }
        else
        {
            SceneFlow.Next();
        }
    }

    // =========================
    // ONE LINE
    // =========================

    IEnumerator PlayLine(DialogueLine line)
    {
        DialogueCast.Speaker speaker = cast.Find(line.speakerId);
        string id = speaker != null ? speaker.id : line.speakerId;

        bool intercom = line.radio == DialogueRadio.Intercom;

        bool radio = intercom || line.radio == DialogueRadio.On ||
            (line.radio == DialogueRadio.Auto && speaker != null && speaker.radio);

        // Who talks on which side
        DialogueSide side = ChooseSide(line, id, speaker);

        DialoguePortrait talker =
            side == DialogueSide.Left ? leftPortrait : rightPortrait;

        DialoguePortrait listener =
            talker == leftPortrait ? rightPortrait : leftPortrait;

        // They switched sides
        if (listener.SpeakerId == id)
        {
            listener.Hide();
        }

        talker.Show(
            id,
            speaker != null ? speaker.GetPortrait(line.emotion) : null,
            radio
        );

        talker.SetLit(true);
        listener.SetLit(false);

        // Name plate on the talker's side
        nameText.text = (speaker != null ? speaker.displayName : id) +
            (intercom ? IntercomTag : radio ? RadioTag : "");
        nameText.color = speaker != null ? speaker.nameColor : Color.white;

        PlaceNamePlate(side == DialogueSide.Right);

        // Radio calls crackle in
        boxStatic.gameObject.SetActive(radio);

        if (radio)
        {
            boxStatic.Burst(0.35f);
            Sfx.Play("radio_static");
        }

        // Type it out
        bodyText.text = DialogueText.Parse(line.text, commands);
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        int total = bodyText.textInfo.characterCount;

        float speed = charactersPerSecond *
            (speaker != null ? speaker.talkSpeed : 1f);

        float pitch = speaker != null ? speaker.voicePitch : 1f;

        int nextCommand = 0;

        rushing = false;
        pressed = false;
        pauseDebt = 0f;

        talker.SetTalking(true);

        for (int shown = 0; shown <= total && !skipping; shown++)
        {
            // Commands that come before this letter
            while (nextCommand < commands.Count &&
                commands[nextCommand].index <= shown)
            {
                yield return RunCommand(commands[nextCommand]);
                nextCommand++;
            }

            if (shown == total)
                break;

            bodyText.maxVisibleCharacters = shown + 1;

            if (rushing)
                continue;

            char letter = bodyText.textInfo.characterInfo[shown].character;

            if (char.IsLetterOrDigit(letter) && blips++ % blipEvery == 0)
            {
                Sfx.Play("blip", pitch * Random.Range(0.97f, 1.03f));
            }

            yield return Pause(1f / speed + PauseAfter(shown, total));
        }

        bodyText.maxVisibleCharacters = total;
        talker.SetTalking(false);

        if (skipping)
            yield break;

        // Wait for the player
        continueIcon.SetActive(true);
        pressed = false;

        while (!pressed && !skipping)
            yield return null;

        pressed = false;
        continueIcon.SetActive(false);

        Sfx.Play("advance");
    }

    DialogueSide ChooseSide(
        DialogueLine line,
        string id,
        DialogueCast.Speaker speaker
    )
    {
        if (line.side != DialogueSide.Auto)
            return line.side;

        // Stay where they already are
        if (leftPortrait.SpeakerId == id)
            return DialogueSide.Left;

        if (rightPortrait.SpeakerId == id)
            return DialogueSide.Right;

        if (speaker != null && speaker.side == DialogueSide.Left)
            return DialogueSide.Left;

        return DialogueSide.Right;
    }

    void PlaceNamePlate(bool right)
    {
        float x = right ? 1f : 0f;

        namePlate.anchorMin = new Vector2(x, 1f);
        namePlate.anchorMax = new Vector2(x, 1f);
        namePlate.pivot = new Vector2(x, namePlate.pivot.y);

        namePlate.anchoredPosition = new Vector2(
            right ? -namePlateInset : namePlateInset,
            namePlate.anchoredPosition.y
        );
    }

    // A short stop after punctuation makes the text feel spoken
    float PauseAfter(int index, int total)
    {
        TMP_CharacterInfo[] letters = bodyText.textInfo.characterInfo;
        char letter = letters[index].character;

        bool endOfWord = index + 1 >= total ||
            char.IsWhiteSpace(letters[index + 1].character);

        if (letter == '.' || letter == '!' || letter == '?')
            return endOfWord ? sentencePause : commaPause;

        if ((letter == ',' || letter == ';' || letter == ':') && endOfWord)
            return commaPause;

        return 0f;
    }

    // =========================
    // COMMANDS
    // =========================

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

                if (!rushing)
                {
                    yield return Pause(seconds);
                }

                break;

            case "shake":
                Shake();
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

    void Shake()
    {
        shaker.Shake(shakeStrength, shakeTime);

        // Shake the world behind the dialogue too, if there is one
        CameraFollow cameraFollow = FindAnyObjectByType<CameraFollow>();

        if (cameraFollow != null)
        {
            cameraFollow.Shake(0.25f, shakeTime);
        }

        Sfx.Play("thud");
    }

    // =========================
    // HELPERS
    // =========================

    // Typing wait; pressing advance shows the rest of the line instead
    IEnumerator Pause(float seconds)
    {
        pauseDebt += seconds;

        while (pauseDebt > 0f && !skipping)
        {
            if (pressed)
            {
                pressed = false;
                rushing = true;
                pauseDebt = 0f;

                yield break;
            }

            yield return null;

            // Typing stops while the game is paused
            if (!PauseMenu.IsPaused)
                pauseDebt -= Time.unscaledDeltaTime;
        }
    }

    IEnumerator Wait(float seconds)
    {
        float time = 0f;

        while (time < seconds && !skipping)
        {
            yield return null;

            if (!PauseMenu.IsPaused)
                time += Time.unscaledDeltaTime;
        }
    }

    IEnumerator Fade(CanvasGroup group, float target, float seconds)
    {
        float start = group.alpha;
        float time = 0f;

        while (time < seconds)
        {
            time += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, time / seconds);

            yield return null;
        }

        group.alpha = target;
    }

    // Types the location stamp in, holds it, then fades it out
    IEnumerator ShowLocation(string location)
    {
        locationText.text = location;
        locationText.alpha = 1f;
        locationText.maxVisibleCharacters = 0;

        for (int i = 1; i <= location.Length; i++)
        {
            locationText.maxVisibleCharacters = i;

            if (i % 2 == 0 && !char.IsWhiteSpace(location[i - 1]))
            {
                Sfx.Play("blip", 2f, 0.4f);
            }

            yield return new WaitForSecondsRealtime(0.04f);
        }

        yield return new WaitForSecondsRealtime(3f);

        for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
        {
            locationText.alpha = 1f - t / 0.6f;
            yield return null;
        }

        locationText.alpha = 0f;
    }
}
