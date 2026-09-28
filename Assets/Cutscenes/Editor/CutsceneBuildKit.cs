using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Timeline;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

// Building blocks for making a cutscene's shoot scene from code
// (see AfterStage1Builder). A builder starts from a copy of the
// intro's shoot scene, which already has the camera, post FX,
// letterbox, caption, flash, fade and Recorder set up, clears out
// the intro's sets and shots, and adds its own with a new Timeline.
// Rerunning a builder rebuilds the scene, its clips and its Timeline.
public static class CutsceneBuildKit
{
    public const string IntroScene = "Assets/Cutscenes/Intro/Intro_Shoot.unity";
    public const string IntroTimeline = "Assets/Cutscenes/Intro/Intro.playable";

    // Character sheets are imported at 100 pixels per unit and the
    // sets at 16, so this brings a character to the sets' pixel size
    public const float CharacterScale = 100f / 16f;

    const string Materials = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/";

    public static Material LitMaterial =>
        AssetDatabase.LoadAssetAtPath<Material>(Materials + "Sprite-Lit-Default.mat");

    // For things that glow on their own (lamps, LEDs)
    public static Material UnlitMaterial =>
        AssetDatabase.LoadAssetAtPath<Material>(Materials + "Sprite-Unlit-Default.mat");

    // =========================
    // SCENE
    // =========================

    // Fresh copy of the intro's shoot scene at scenePath, opened
    public static Scene StartFromIntro(string scenePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath));

        // Overwrite in place so the scene keeps its GUID between builds
        File.Copy(IntroScene, scenePath, true);
        AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);

        return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    }

    // Refuses to build over unsaved work in whatever scene is open
    public static bool SafeToBuild(string scenePath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play mode before building a cutscene.");
            return false;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);

            if (open.isDirty && open.path != scenePath)
            {
                Debug.LogWarning("Save or discard the changes to " + open.path +
                                 " before building a cutscene.");
                return false;
            }
        }

        return true;
    }

    public static Transform Root(string name)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == name)
                return go.transform;
        }

        throw new Exception("The shoot scene has no root object '" + name + "'");
    }

    public static Transform Child(Transform parent, string path)
    {
        Transform found = parent.Find(path);

        if (found == null)
            throw new Exception(parent.name + " has no child '" + path + "'");

        return found;
    }

    // Destroys every child except the ones listed
    public static void ClearChildren(Transform parent, params Transform[] keep)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (!keep.Contains(child))
                Object.DestroyImmediate(child.gameObject);
        }
    }

    // =========================
    // OBJECTS
    // =========================

    public static Transform AddGroup(string name, Transform parent, Vector2 position = default)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        return go.transform;
    }

    public static SpriteRenderer AddSprite(string name, Transform parent, Sprite sprite,
        Vector2 position, int order, string layer = "Default", Color? tint = null,
        float scale = 1f, bool flipX = false, bool unlit = false)
    {
        Transform t = AddGroup(name, parent, position);
        t.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = unlit ? UnlitMaterial : LitMaterial;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint ?? Color.white;
        sr.flipX = flipX;
        return sr;
    }

    // A character from a 100 PPU sheet, scaled to the sets
    public static SpriteRenderer AddCharacter(string name, Transform parent, Sprite sprite,
        Vector2 position, int order, Color tint, bool flipX = false, string layer = "Default")
    {
        return AddSprite(name, parent, sprite, position, order, layer, tint,
                      CharacterScale, flipX);
    }

    // A layer that drifts with the camera (CutsceneParallax), like
    // the intro's: the root is placed, its child "Layer" slides.
    // Returns the root; its sprite is root.GetChild(0).
    public static Transform AddParallax(string name, Transform parent, Sprite sprite,
        Vector2 position, float parallax, Vector2 anchor, int order,
        string layer = "Background", bool flipX = false, Color? tint = null)
    {
        Transform root = AddGroup(name, parent, position);

        CutsceneParallax p = root.gameObject.AddComponent<CutsceneParallax>();
        p.parallax = parallax;
        p.anchor = anchor;

        if (sprite != null)
        {
            p.layer = AddSprite("Layer", root, sprite, Vector2.zero, order, layer, tint,
                             1f, flipX).transform;
        }
        else
        {
            p.layer = AddGroup("Layer", root);
        }

        return root;
    }

    // A point light, or a spot when angle < 360 (it points along the
    // object's up axis: rotation 0 is straight up, 90 is left).
    // volume > 0 makes the cone itself visible, like a beam in haze.
    // A light's cone is drawn over the sorting layers it lights, so
    // light only the layers that should sit behind the beam.
    public static Light2D AddLight(string name, Transform parent, Vector2 position,
        Color color, float intensity, float radius, float angle = 360f,
        float rotation = 0f, float volume = 0f, float falloff = 0.5f,
        params string[] layers)
    {
        Transform t = AddGroup(name, parent, position);
        t.localEulerAngles = new Vector3(0f, 0f, rotation);

        Light2D light = t.gameObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightInnerRadius = 0f;
        light.pointLightOuterRadius = radius;
        light.pointLightOuterAngle = angle;
        light.pointLightInnerAngle = angle < 360f ? angle * 0.35f : 360f;
        light.falloffIntensity = falloff;
        light.volumeIntensity = volume;
        light.volumetricEnabled = volume > 0f;
        light.shadowsEnabled = false;

        string[] targets = layers.Length > 0 ?
            layers : SortingLayer.layers.Select(l => l.name).ToArray();
        light.targetSortingLayers = targets.Select(SortingLayer.NameToID).ToArray();

        return light;
    }

    // =========================
    // ASSETS
    // =========================

    public static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
            throw new Exception("No sprite at " + path);

        return sprite;
    }

    // One sprite from a sliced sheet, by name
    public static Sprite LoadSprite(string sheet, string name)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(sheet)
            .OfType<Sprite>().FirstOrDefault(s => s.name == name);

        if (sprite == null)
            throw new Exception(sheet + " has no sprite '" + name + "'");

        return sprite;
    }

    public static AudioClip LoadAudio(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        if (clip == null)
            throw new Exception("No audio clip at " + path);

        return clip;
    }

    // Saves a new asset, replacing whatever was at the path
    public static T SaveNew<T>(T asset, string path) where T : Object
    {
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    // Creates an asset folder (and its parents) if it isn't there
    public static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    // =========================
    // ANIMATION
    // =========================

    // How a curve moves from one key to the next
    public enum Ease
    {
        Smooth,   // eases through the key (auto tangents)
        InOut,    // starts and stops gently (flat tangents)
        Linear,   // constant speed
        Hold      // jumps at the next key
    }

    // A float curve, built key by key. A key's ease is how the curve
    // leaves it, towards the next key.
    public class Curve
    {
        readonly List<(float t, float v, Ease ease)> keys = new List<(float, float, Ease)>();

        public Curve(float t, float v, Ease ease = Ease.InOut)
        {
            To(t, v, ease);
        }

        public Curve To(float t, float v, Ease ease = Ease.InOut)
        {
            keys.Add((t, v, ease));
            return this;
        }

        public static Curve Constant(float v)
        {
            return new Curve(0f, v, Ease.Hold);
        }

        public AnimationCurve Build()
        {
            AnimationCurve curve = new AnimationCurve(
                keys.Select(k => new Keyframe(k.t, k.v, 0f, 0f)).ToArray()
            );

            for (int i = 0; i < keys.Count; i++)
            {
                AnimationUtility.SetKeyRightTangentMode(curve, i, Mode(keys[i].ease));

                Ease arriving = i > 0 ? keys[i - 1].ease : keys[i].ease;
                AnimationUtility.SetKeyLeftTangentMode(curve, i, Mode(arriving));
            }

            return curve;
        }

        static AnimationUtility.TangentMode Mode(Ease ease)
        {
            switch (ease)
            {
                case Ease.Linear: return AnimationUtility.TangentMode.Linear;
                case Ease.Hold: return AnimationUtility.TangentMode.Constant;
                case Ease.InOut: return AnimationUtility.TangentMode.Free;
                default: return AnimationUtility.TangentMode.ClampedAuto;
            }
        }
    }

    // Sprite swaps over time, e.g. a walk cycle
    public class SpriteKeys
    {
        readonly SortedDictionary<float, Sprite> keys = new SortedDictionary<float, Sprite>();

        public SpriteKeys Set(float t, Sprite sprite)
        {
            keys[(float)Math.Round(t, 4)] = sprite;
            return this;
        }

        // Loops through frames from start until end
        public SpriteKeys Cycle(float start, float end, float frameTime, params Sprite[] frames)
        {
            int i = 0;

            for (float t = start; t < end - 0.0001f; t += frameTime)
                Set(t, frames[i++ % frames.Length]);

            return this;
        }

        public ObjectReferenceKeyframe[] Build()
        {
            return keys.Select(k => new ObjectReferenceKeyframe { time = k.Key, value = k.Value })
                       .ToArray();
        }
    }

    // All the animated properties of one Timeline animation track,
    // with paths relative to the track's bound Animator
    public class Clip
    {
        public readonly AnimationClip clip;

        public Clip(string name)
        {
            clip = new AnimationClip { name = name, frameRate = 60f };
        }

        public Clip Float(string path, Type type, string property, Curve curve)
        {
            AnimationUtility.SetEditorCurve(
                clip, EditorCurveBinding.FloatCurve(path, type, property), curve.Build()
            );
            return this;
        }

        // Position in x and y; z is held so it isn't zeroed
        public Clip Move(string path, Curve x, Curve y, float z = 0f)
        {
            Float(path, typeof(Transform), "m_LocalPosition.x", x);
            Float(path, typeof(Transform), "m_LocalPosition.y", y);
            return Float(path, typeof(Transform), "m_LocalPosition.z", Curve.Constant(z));
        }

        public Clip Rotate(string path, Curve degrees)
        {
            Float(path, typeof(Transform), "localEulerAnglesRaw.x", Curve.Constant(0f));
            Float(path, typeof(Transform), "localEulerAnglesRaw.y", Curve.Constant(0f));
            return Float(path, typeof(Transform), "localEulerAnglesRaw.z", degrees);
        }

        // On/off switches (GameObject active, flipX...) at the given times
        public Clip Flags(string path, Type type, string property, params (float t, bool on)[] keys)
        {
            Curve curve = new Curve(keys[0].t, keys[0].on ? 1f : 0f, Ease.Hold);

            foreach ((float t, bool on) in keys.Skip(1))
                curve.To(t, on ? 1f : 0f, Ease.Hold);

            return Float(path, type, property, curve);
        }

        public Clip Active(string path, params (float t, bool on)[] keys)
        {
            return Flags(path, typeof(GameObject), "m_IsActive", keys);
        }

        public Clip Sprites(string path, SpriteKeys keys)
        {
            AnimationUtility.SetObjectReferenceCurve(
                clip,
                EditorCurveBinding.PPtrCurve(path, typeof(SpriteRenderer), "m_Sprite"),
                keys.Build()
            );
            return this;
        }

        public AnimationClip Save(string path)
        {
            return SaveNew(clip, path);
        }
    }

    // =========================
    // TIMELINE
    // =========================

    public static TimelineAsset NewTimeline(string path, double duration)
    {
        TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.editorSettings.frameRate = 60;
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = duration;
        return SaveNew(timeline, path);
    }

    // Points the shoot scene's director at the new Timeline,
    // forgetting the intro's bindings
    public static PlayableDirector ResetDirector(TimelineAsset timeline)
    {
        PlayableDirector director = Root("Shoot").GetComponent<PlayableDirector>();

        SerializedObject so = new SerializedObject(director);
        so.FindProperty("m_SceneBindings").ClearArray();
        so.FindProperty("m_ExposedReferences.m_References").ClearArray();
        so.ApplyModifiedPropertiesWithoutUndo();

        director.playableAsset = timeline;
        return director;
    }

    static PropertyName NewReference()
    {
        return new PropertyName(GUID.Generate().ToString());
    }

    public static void AddAnimationTrack(TimelineAsset timeline, PlayableDirector director,
        string name, Animator target, AnimationClip clip)
    {
        UnityEngine.Timeline.AnimationTrack track = timeline.CreateTrack<UnityEngine.Timeline.AnimationTrack>(null, name);
        TimelineClip c = track.CreateClip(clip);
        c.start = 0;
        c.duration = timeline.fixedDuration;
        c.displayName = clip.name;

        director.SetGenericBinding(track, target);
    }

    public static void AddShots(TimelineAsset timeline, PlayableDirector director,
        CinemachineBrain brain, params (string name, double start, double end, CinemachineCamera camera)[] shots)
    {
        CinemachineTrack track = timeline.CreateTrack<CinemachineTrack>(null, "Shots");

        foreach (var shot in shots)
        {
            TimelineClip c = track.CreateClip<CinemachineShot>();
            c.displayName = shot.name;
            c.start = shot.start;
            c.duration = shot.end - shot.start;

            CinemachineShot asset = (CinemachineShot)c.asset;
            asset.VirtualCamera.exposedName = NewReference();
            director.SetReferenceValue(asset.VirtualCamera.exposedName, shot.camera);
        }

        director.SetGenericBinding(track, brain);
    }

    // Turns a particle system (or any object) on for a stretch of
    // time, simulating its particles so scrubbing shows them too
    public static void AddControl(TimelineAsset timeline, PlayableDirector director,
        string name, GameObject target, double start, double end, uint seed = 1)
    {
        ControlTrack track = timeline.CreateTrack<ControlTrack>(null, name);
        TimelineClip c = track.CreateClip<ControlPlayableAsset>();
        c.displayName = target.name;
        c.start = start;
        c.duration = end - start;

        ControlPlayableAsset asset = (ControlPlayableAsset)c.asset;
        asset.sourceGameObject.exposedName = NewReference();
        director.SetReferenceValue(asset.sourceGameObject.exposedName, target);
        asset.updateParticle = true;
        asset.particleRandomSeed = seed;
        asset.active = true;
        asset.postPlayback = ActivationControlPlayable.PostPlaybackState.Revert;
        asset.updateDirector = false;
        asset.updateITimeControl = false;
        asset.searchHierarchy = false;
    }

    // Clips that overlap on one track crossfade, which fades in the
    // later clip's start (a voice line's first words), so a clip that
    // starts before the previous one's tail ends goes on its own track
    public static void AddAudio(TimelineAsset timeline, string name,
        params (double start, AudioClip clip, float volume)[] clips)
    {
        var lanes = new List<(AudioTrack track, double end)>();

        foreach (var entry in clips.OrderBy(e => e.start))
        {
            int lane = lanes.FindIndex(l => l.end <= entry.start);

            if (lane < 0)
            {
                string trackName = lanes.Count == 0 ? name : name + " (" + (lanes.Count + 1) + ")";
                lanes.Add((timeline.CreateTrack<AudioTrack>(null, trackName), 0));
                lane = lanes.Count - 1;
            }

            AudioTrack track = lanes[lane].track;
            lanes[lane] = (track, entry.start + entry.clip.length);

            TimelineClip c = track.CreateClip(entry.clip);
            c.start = entry.start;
            c.displayName = entry.clip.name;

            SerializedObject so = new SerializedObject(c.asset);
            so.FindProperty("m_ClipProperties.volume").floatValue = entry.volume;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // Records the Game view to Recordings/<output>_master.mov when
    // the shoot scene plays, with the intro's master settings
    // (1920x1080 ProRes 422 HQ with sound)
    public static void AddRecorder(TimelineAsset timeline, string output)
    {
        TimelineAsset intro = AssetDatabase.LoadAssetAtPath<TimelineAsset>(IntroTimeline);

        RecorderSettings template = intro.GetOutputTracks()
            .OfType<RecorderTrack>()
            .SelectMany(t => t.GetClips())
            .Select(c => ((RecorderClip)c.asset).settings)
            .First(s => s != null);

        RecorderSettings settings = Object.Instantiate(template);
        settings.name = output + " master";

        // The copy comes out at 30 fps uncapped, so set the intro's rate
        settings.FrameRatePlayback = template.FrameRatePlayback;
        settings.FrameRate = template.FrameRate;
        settings.CapFrameRate = template.CapFrameRate;

        SerializedObject so = new SerializedObject(settings);
        so.FindProperty("fileNameGenerator.m_FileName").stringValue = output + "_master";
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.AddObjectToAsset(settings, timeline);

        RecorderTrack track = timeline.CreateTrack<RecorderTrack>(null, "Recorder");
        TimelineClip c = track.CreateClip<RecorderClip>();
        c.displayName = "Master (ProRes 422 HQ)";
        c.start = 0;
        c.duration = timeline.fixedDuration;
        ((RecorderClip)c.asset).settings = settings;
    }

    // =========================
    // PREVIEWS
    // =========================

    // Renders the shoot camera at the given times to PNGs, without
    // Play mode. The overlay UI (letterbox, captions, fades) is not
    // in these.
    public static void RenderStills(PlayableDirector director, IEnumerable<float> times,
        string folder, int width = 960, int height = 540)
    {
        Directory.CreateDirectory(folder);

        Camera camera = Root("Main Camera").GetComponent<Camera>();
        CinemachineBrain brain = camera.GetComponent<CinemachineBrain>();

        RenderTexture target = new RenderTexture(
            width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB
        );
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);

        director.RebuildGraph();

        // 2D lights build their shape and culling bounds in LateUpdate,
        // which an Editor in the background may not have run yet
        System.Reflection.MethodInfo updateLight = typeof(Light2D).GetMethod(
            "LateUpdate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        foreach (float t in times)
        {
            director.time = t;
            director.Evaluate();
            brain.ManualUpdate();

            foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                updateLight.Invoke(light, null);

            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            File.WriteAllBytes(
                Path.Combine(folder, "t_" + t.ToString("00.00") + ".png"),
                image.EncodeToPNG()
            );
        }

        camera.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(image);
    }
}
