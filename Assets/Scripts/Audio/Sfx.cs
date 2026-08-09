using UnityEngine;
using System.Collections.Generic;

// Plays sound effects by name from anywhere: Sfx.Play("shoot").
// The sounds are listed in Resources/SoundLibrary. Created
// automatically before the first scene loads and kept for the
// whole game, so sounds don't cut off when the scene changes.
public class Sfx : MonoBehaviour
{
    // How many sounds can play at the same time
    const int Voices = 16;

    private static Sfx instance;

    private SoundLibrary library;
    private AudioSource[] voices;
    private int nextVoice;

    private Dictionary<string, SoundLibrary.Sound> sounds =
        new Dictionary<string, SoundLibrary.Sound>();

    private Dictionary<string, float> lastPlayed =
        new Dictionary<string, float>();

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    static void CreateInstance()
    {
        instance = null;

        SoundLibrary library =
            Resources.Load<SoundLibrary>("SoundLibrary");

        if (library == null)
        {
            Debug.LogWarning(
                "⚠️ Resources/SoundLibrary is missing, so there are no sound effects."
            );

            return;
        }

        GameObject host = new GameObject("Sfx");
        DontDestroyOnLoad(host);

        instance = host.AddComponent<Sfx>();
        instance.Setup(library);
    }

    void Setup(SoundLibrary soundLibrary)
    {
        library = soundLibrary;

        foreach (SoundLibrary.Sound sound in library.sounds)
        {
            if (sound.clip != null && !string.IsNullOrEmpty(sound.name))
            {
                sounds[sound.name] = sound;
            }
        }

        voices = new AudioSource[Voices];

        for (int i = 0; i < Voices; i++)
        {
            AudioSource voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;

            voices[i] = voice;
        }
    }

    // Play a sound. pitch and volume multiply the library's settings.
    public static void Play(string name, float pitch = 1f, float volume = 1f)
    {
        if (instance != null)
        {
            instance.PlaySound(name, pitch, volume);
        }
    }

    void PlaySound(string name, float pitch, float volume)
    {
        SoundLibrary.Sound sound;

        if (!sounds.TryGetValue(name, out sound))
        {
            Debug.LogWarning("⚠️ Sfx: no sound called '" + name + "'");
            return;
        }

        // Skip repeats that come too close together
        float now = Time.unscaledTime;
        float last;

        if (lastPlayed.TryGetValue(name, out last) &&
            now - last < sound.minInterval)
            return;

        lastPlayed[name] = now;

        AudioSource voice = FreeVoice();

        voice.clip = sound.clip;
        voice.volume = sound.volume * volume * library.masterVolume;
        voice.pitch = pitch * (1f + Random.Range(
            -sound.pitchVariation,
            sound.pitchVariation
        ));

        voice.Play();
    }

    // A voice that isn't busy; if they all are, the oldest one
    AudioSource FreeVoice()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            int index = (nextVoice + i) % voices.Length;

            if (!voices[index].isPlaying)
            {
                nextVoice = (index + 1) % voices.Length;
                return voices[index];
            }
        }

        AudioSource oldest = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;

        return oldest;
    }
}
