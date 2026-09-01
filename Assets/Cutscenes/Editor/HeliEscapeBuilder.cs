using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;
using UnityEngine.UI;
using static CutsceneBuildKit;

// Builds the shoot scene for Cutscene_HelicopterEscape, between the
// Mecha-Lion fight and the helicopter chase (STORY_AND_GAME_STRUCTURE.md,
// 7): the Mecha-Lion fails and explodes; the General climbs out of
// the wreck, singed, helmet askew ("This is not over! The King will
// return!"), scurries to his helicopter and lifts off ("After a short
// break!"); Rook and Tala run for the other one, unlocked and empty,
// and go after him.
//
// Contra > Cutscenes > HeliEscape > Build Shoot Scene rebuilds
// HeliEscape/HeliEscape_Shoot.unity and its Timeline from the art and
// sound made by Tools/cutscene_heliescape_*. Then press Play in that
// scene to record the master, and run
//     python Tools/cutscene_encode.py Cutscene_HelicopterEscape 18.4
public static class HeliEscapeBuilder
{
    const string Folder = "Assets/Cutscenes/HeliEscape/";
    const string ScenePath = Folder + "HeliEscape_Shoot.unity";
    const string TimelinePath = Folder + "HeliEscape.playable";
    const string StillsFolder = "Temp/Captures/HeliEscape";
    const string Night = "Assets/Cutscenes/AfterStage1/Art/";

    // Shot timing in seconds; Tools/cutscene_heliescape_audio.py
    // writes the score and effects to the same times
    const float Spark = 0.4f;      // the Mecha-Lion sparks
    const float Stagger = 0.9f;    // it staggers, powering down
    const float Fall = 1.6f;       // it keels over
    const float Thud = 2.1f;       // hits the ground
    const float Explode = 2.4f;    // and explodes
    const float Wreck = 3.2f;      // cut to the burning wreck
    const float Hatch = 3.6f;      // a plate pops off
    const float Climb = 3.9f;      // the General climbs out
    const float Gen01 = 4.2f;      // "This is not over! The King will return!"
    const float Run = 8.3f;        // cut wide: he scurries to his helicopter
    const float Board = 9.7f;      // in; the rotor spins up
    const float Lift = 10.3f;      // cut: lift-off
    const float Gen02 = 10.9f;     // "After a short break!"
    const float Away = 12.6f;      // off he flies
    const float Team = 13.2f;      // cut: Rook and Tala run for the other one
    const float Reach = 14.6f;     // in they get
    const float Spin = 14.9f;      // its rotor spins up
    const float Rise = 15.9f;      // lift-off, after him
    const float Cut = 17.6f;       // hard cut to black
    const float End = 18.4f;

    const float Ground = -2.5f;
    static readonly Vector2 Anchor = new Vector2(3f, 2.5f);

    static readonly Vector2 MechaAt = new Vector2(1f, Ground);
    const float MechaScale = 0.85f;
    static readonly Vector2 HeadOffset = new Vector2(-3.1f, 4.75f);
    static readonly Vector2 WreckAt = new Vector2(0.8f, -1.2f);

    const float RookX = -6f, TalaX = -7.1f;
    static readonly float RookY = Ground + 19f / 16f;
    static readonly float TalaY = Ground + 17f / 16f;
    static readonly Vector2 GeneralTop = new Vector2(1.4f, 1.3f);
    static readonly float GeneralY = Ground + 19f / 16f;

    // Helicopters (16 PPU art, shown bigger) sit on their pads
    const float HeliScale = 1.75f;
    const float GeneralPadX = 8f, TeamPadX = 16f;
    static readonly float HeliY = Ground + 10f / 16f + 1.75f;
    static readonly Vector2 WindowOffset = new Vector2(1.4f, 1f);

    static readonly Color NightTint = new Color(0.8f, 0.82f, 0.95f);
    static readonly Color Charred = new Color(0.28f, 0.22f, 0.22f);
    static readonly Color Fire = new Color(1f, 0.55f, 0.2f);
    static readonly Color Sodium = new Color(1f, 0.78f, 0.5f);
    static readonly Color EyeRed = new Color(1f, 0.12f, 0.08f);

    static Sprite ArtSprite(string name)
    {
        return LoadSprite(Folder + "Art/" + name + ".png");
    }

    static Sprite NightSprite(string name)
    {
        return LoadSprite(Night + name + ".png");
    }

    static Sprite[] Frames(string sheet, string prefix, int count)
    {
        return Enumerable.Range(0, count).Select(i => LoadSprite(sheet, prefix + i)).ToArray();
    }

    static Sprite[] RookIdle => Frames("Assets/Sprites/Hero/Hero.png", "Hero_idle_", 5);
    static Sprite[] RookRun => Frames("Assets/Sprites/Hero/Hero.png", "Hero_run_", 6);

    static Sprite[] Heli(string name)
    {
        return new[] { LoadSprite("Assets/HelicopterStage/Art/" + name + "_0.png"),
                       LoadSprite("Assets/HelicopterStage/Art/" + name + "_1.png") };
    }

    static AudioClip Sound(string name)
    {
        return LoadAudio(Folder + "Audio/" + name + ".wav");
    }

    // =========================
    // BUILD
    // =========================

    [MenuItem("Contra/Cutscenes/HeliEscape/Build Shoot Scene")]
    public static void Build()
    {
        if (!SafeToBuild(ScenePath))
            return;

        StartFromIntro(ScenePath);

        Transform sets = Root("Sets");
        Transform cameras = Root("Cameras");

        Transform sparks = Child(sets, "B_Gate/Sparks");
        Transform smoke = Child(sets, "B_Gate/Smoke");
        foreach (Transform fx in new[] { sparks, smoke })
            fx.SetParent(null, true);

        ClearChildren(sets);

        Transform template = Child(cameras, "CM_Aerial");
        template.SetParent(null, true);
        ClearChildren(cameras);

        Transform a = BuildPad(sets, sparks, smoke);

        var cams = new Dictionary<string, CinemachineCamera>();
        foreach (string name in new[] { "CM_Collapse", "CM_Wreck", "CM_Run", "CM_Lift", "CM_Team" })
        {
            GameObject cam = Object.Instantiate(template.gameObject, cameras);
            cam.name = name;
            cams[name] = cam.GetComponent<CinemachineCamera>();
        }
        Object.DestroyImmediate(template.gameObject);

        Transform overlay = Root("Overlay");
        Child(overlay, "Caption").GetComponent<CanvasGroup>().alpha = 0f;
        Child(overlay, "Title").GetComponent<CanvasGroup>().alpha = 0f;

        TimelineAsset timeline = NewTimeline(TimelinePath, End);
        PlayableDirector director = ResetDirector(timeline);

        AddShots(timeline, director, Root("Main Camera").GetComponent<CinemachineBrain>(),
            ("Collapse", 0, Wreck, cams["CM_Collapse"]),
            ("Wreck", Wreck, Run, cams["CM_Wreck"]),
            ("Run", Run, Lift, cams["CM_Run"]),
            ("Lift", Lift, Team, cams["CM_Lift"]),
            ("Team", Team, End, cams["CM_Team"]));

        AddAnimationTrack(timeline, director, "Camera Moves",
            cameras.GetComponent<Animator>(), CameraClip().Save(Folder + "Animation/Cameras.anim"));
        AddAnimationTrack(timeline, director, "Props",
            sets.GetComponent<Animator>(), PropsClip().Save(Folder + "Animation/Props.anim"));
        AddAnimationTrack(timeline, director, "Overlay",
            overlay.GetComponent<Animator>(), OverlayClip().Save(Folder + "Animation/Overlay.anim"));
        AddAnimationTrack(timeline, director, "Lighting",
            Root("Lighting").GetComponent<Animator>(), LightingClip().Save(Folder + "Animation/Lighting.anim"));

        AddControl(timeline, director, "FX Sparks", Child(a, "Sparks").gameObject, Spark, Explode, 3);
        AddControl(timeline, director, "FX Blast", Child(a, "Blast").gameObject, Explode, Wreck + 1f, 5);
        AddControl(timeline, director, "FX Smoke", Child(a, "Smoke").gameObject, Explode, Team, 7);
        AddControl(timeline, director, "FX Embers", Child(a, "Embers").gameObject, Explode, Team, 9);
        AddControl(timeline, director, "FX Dust A", Child(a, "Dust A").gameObject, Board + 0.3f, Team, 11);
        AddControl(timeline, director, "FX Dust B", Child(a, "Dust B").gameObject, Spin + 0.3f, End, 13);

        AddSound(timeline);
        AddRecorder(timeline, "Cutscene_HelicopterEscape");

        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("🎬 Built " + ScenePath);
    }

    [MenuItem("Contra/Cutscenes/HeliEscape/Render Stills")]
    public static void RenderKeyStills()
    {
        RenderAt(0.6f, 1.9f, 2.5f, 3.0f, 3.7f, 5.0f, 7.5f, 8.8f, 9.6f, 11.2f, 12.9f, 13.8f, 15.2f, 16.4f, 17.4f);
    }

    public static void RenderAt(params float[] times)
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);

        RenderStills(Root("Shoot").GetComponent<PlayableDirector>(), times, StillsFolder);
        Debug.Log("🖼️ Stills in " + StillsFolder);
    }

    // =========================
    // THE SET: THE COMPOUND'S HELIPADS, NIGHT
    // =========================

    static Transform BuildPad(Transform sets, Transform sparks, Transform smoke)
    {
        Transform a = AddGroup("A_Pad", sets);
        float g = Ground;

        AddParallax("Sky", a, NightSprite("sky_night"), new Vector2(2f, -0.5f), 1f, Anchor, -100);
        AddParallax("Mountains", a, NightSprite("night_far"), new Vector2(3f, -2.3f), 0.85f, Anchor, -90);
        AddParallax("Hills", a, NightSprite("night_hills"), new Vector2(3f, -0.7f), 0.7f, Anchor, -80);
        AddParallax("Keep", a, NightSprite("keep"), new Vector2(-2f, g + 6.2f), 0.45f, Anchor, -70,
            tint: new Color(0.55f, 0.55f, 0.65f));
        AddParallax("Tower", a, NightSprite("tower"), new Vector2(13f, g + 5.2f), 0.3f, Anchor, -65,
            tint: new Color(0.6f, 0.6f, 0.7f));

        AddSprite("Fence L", a, NightSprite("fence"), new Vector2(-4f, g + 1.875f), -15);
        AddSprite("Fence R", a, NightSprite("fence"), new Vector2(12f, g + 1.875f), -15);
        AddSprite("Flood L", a, NightSprite("flood_pole"), new Vector2(-3.5f, g + 2.25f), -12);
        AddSprite("Flood R", a, NightSprite("flood_pole"), new Vector2(12f, g + 2.25f), -12);
        AddSprite("Crates", a, NightSprite("crates"), new Vector2(4.4f, g + 15f / 16f), -5);
        AddSprite("Sandbags", a, NightSprite("sandbags"), new Vector2(-9f, g + 7f / 16f), -5);
        AddSprite("Ground", a, NightSprite("base_ground"), new Vector2(5f, g - 1.5f), 0);
        AddSprite("Pad A", a, ArtSprite("helipad"), new Vector2(GeneralPadX, g + 5f / 16f), 3);
        AddSprite("Pad B", a, ArtSprite("helipad"), new Vector2(TeamPadX, g + 5f / 16f), 3);

        AddLight("Flood Lamp L", a, new Vector2(-3.5f, g + 4.2f), Sodium, 0.9f, 7f, 70f, 180f, 0.15f);
        AddLight("Flood Lamp R", a, new Vector2(12f, g + 4.2f), Sodium, 0.9f, 7f, 70f, 180f, 0.15f);

        // The Mecha-Lion, beaten, as in the boss fight
        Transform mecha = AddGroup("Mecha", a, MechaAt);
        mecha.localScale = new Vector3(MechaScale, MechaScale, 1f);
        AddSprite("Body", mecha, LoadSprite("Assets/BossFight/Art/MechaBody.png"), new Vector2(0f, 1.75f), 10);
        SpriteRenderer head = AddSprite("Head", mecha, LoadSprite("Assets/BossFight/Art/MechaHead.png"),
            HeadOffset, 11);
        AddSprite("Eyes", head.transform, LoadSprite("Assets/Cutscenes/AfterStage2/Art/mecha_eyes.png"),
            Vector2.zero, 12, unlit: true);

        for (int i = 1; i <= 4; i++)
            AddSprite("Plate " + i, a, LoadSprite("Assets/BossFight/Art/ArmorPlate.png"), WreckAt, 13)
                .gameObject.SetActive(false);

        // The General climbs out from behind the wreck
        AddSprite("General", a, ArtSprite("general_shout"), new Vector2(GeneralTop.x, -1f), 9, flipX: true)
            .gameObject.SetActive(false);

        AddCharacter("Rook", a, RookIdle[0], new Vector2(RookX, RookY), 15, NightTint);
        AddSprite("Tala", a, ArtSprite("tala_stand"), new Vector2(TalaX, TalaY), 14, tint: NightTint);

        SpriteRenderer heli = AddSprite("General Heli", a, Heli("GeneralHeli")[0],
            new Vector2(GeneralPadX, HeliY), 6, scale: HeliScale, flipX: true);
        AddSprite("General Window", a, ArtSprite("general_window"),
            new Vector2(GeneralPadX, HeliY) + WindowOffset, 7, flipX: false).gameObject.SetActive(false);
        AddSprite("Team Heli", a, Heli("PlayerHeli")[0], new Vector2(TeamPadX, HeliY), 6, scale: HeliScale);

        AddLight("Fire", a, WreckAt, Fire, 0f, 7f, falloff: 0.4f);
        AddLight("Fireball", a, WreckAt + new Vector2(0f, 1f), new Color(1f, 0.8f, 0.5f), 0f, 12f, falloff: 0.3f);
        AddLight("Eye Glow", a, MechaAt + (HeadOffset + new Vector2(-0.31f, 0.19f)) * MechaScale, EyeRed,
            1.2f, 2f);

        // Effects, all from the intro's sparks and smoke
        Vector2 neck = MechaAt + new Vector2(-2f, 3.5f) * MechaScale;
        PlaceFX(sparks, a, "Sparks", neck);
        Burst(sparks, a, "Blast", WreckAt + new Vector2(0f, 0.6f), 90, new Vector2(3f, 9f), new Vector2(0.15f, 0.4f),
            new Color(1f, 0.95f, 0.6f), new Color(1f, 0.4f, 0.1f), 0.3f, false);
        Burst(sparks, a, "Embers", WreckAt, 14, new Vector2(0.6f, 1.6f), new Vector2(0.06f, 0.12f),
            new Color(1f, 0.7f, 0.2f), new Color(1f, 0.3f, 0.05f), -0.15f, true);
        PlaceFX(smoke, a, "Smoke", WreckAt + new Vector2(0f, -0.4f));
        foreach ((string name, float x) in new[] { ("Dust A", GeneralPadX), ("Dust B", TeamPadX) })
        {
            GameObject dust = Object.Instantiate(smoke.gameObject, a);
            dust.name = name;
            dust.transform.localPosition = new Vector2(x, g + 0.4f);
            dust.SetActive(false);
        }

        return a;
    }

    static void PlaceFX(Transform fx, Transform set, string name, Vector2 position)
    {
        fx.SetParent(set, false);
        fx.name = name;
        fx.localPosition = position;
        fx.gameObject.SetActive(false);
    }

    // A copy of the intro's sparks: a one-off burst, or a steady stream
    static void Burst(Transform sparks, Transform set, string name, Vector2 position, int count,
        Vector2 speed, Vector2 size, Color from, Color to, float gravity, bool stream)
    {
        GameObject go = Object.Instantiate(sparks.gameObject, set);
        go.name = name;
        go.transform.localPosition = position;

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = stream;
        main.duration = 1f;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(stream ? 1f : 0.3f, stream ? 2f : 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.gravityModifier = gravity;
        main.maxParticles = 128;
        main.startColor = new ParticleSystem.MinMaxGradient(from, to);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverDistance = 0f;
        emission.rateOverTime = stream ? count : 0f;
        emission.SetBursts(stream ? new ParticleSystem.Burst[0] : new[] { new ParticleSystem.Burst(0f, count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = stream ? 1.2f : 0.4f;
        shape.rotation = Vector3.zero;

        go.SetActive(false);
    }

    // =========================
    // CAMERAS
    // =========================

    static void Shot(Clip clip, string cam, Curve x, Curve y, Curve size, Curve shake)
    {
        clip.Move(cam, x, y, -10f);
        clip.Float(cam, typeof(CinemachineCamera), "Lens.OrthographicSize", size);
        clip.Float(cam, typeof(CinemachineBasicMultiChannelPerlin), "AmplitudeGain", shake);
    }

    static Clip CameraClip()
    {
        Clip clip = new Clip("Cameras");

        Shot(clip, "CM_Collapse",
            new Curve(0f, -1.6f, Ease.Linear).To(Wreck, -1.2f),
            new Curve(0f, 0.7f, Ease.Linear).To(Wreck, 0.6f),
            new Curve(0f, 3.6f, Ease.Linear).To(Explode, 3.45f, Ease.Hold).To(Explode + 0.05f, 3.3f).To(Wreck, 3.4f),
            new Curve(0f, 0.15f, Ease.Hold).To(Stagger, 0.15f).To(Fall, 0.5f).To(Thud, 0.5f, Ease.Hold)
                .To(Thud + 0.05f, 1.5f).To(Explode - 0.01f, 0.4f, Ease.Hold).To(Explode + 0.05f, 4.5f).To(Wreck, 1f));

        Shot(clip, "CM_Wreck",
            new Curve(Wreck, 0.7f, Ease.Linear).To(Gen01, 1.2f).To(Run, 1.35f),
            new Curve(Wreck, -0.2f).To(Gen01, 0.95f).To(Run, 1.05f),
            new Curve(Wreck, 2.4f).To(Gen01, 1.95f).To(Run, 1.75f),
            new Curve(Wreck, 0.5f).To(Climb, 0.2f));

        Shot(clip, "CM_Run",
            new Curve(Run, 2.5f, Ease.Linear).To(Lift, 5.8f),
            new Curve(Run, 0.4f, Ease.Linear).To(Lift, 0.5f),
            Curve.Constant(3.4f),
            Curve.Constant(0.2f));

        Shot(clip, "CM_Lift",
            new Curve(Lift, GeneralPadX + 0.4f, Ease.Linear).To(Away, GeneralPadX + 1f).To(Team, GeneralPadX + 2.5f),
            new Curve(Lift, 0.6f).To(Away, 2.8f).To(Team, 3.6f),
            Curve.Constant(3f),
            new Curve(Lift, 0.6f).To(Away, 0.3f));

        Shot(clip, "CM_Team",
            new Curve(Team, 10.5f, Ease.Linear).To(Rise, 14f).To(Cut, 15.5f),
            new Curve(Team, 0.4f, Ease.Linear).To(Rise, 0.8f).To(Cut, 2.2f),
            new Curve(Team, 3.4f, Ease.Linear).To(Cut, 3.2f),
            new Curve(Team, 0.15f, Ease.Hold).To(Spin, 0.15f).To(Rise, 0.6f).To(Cut, 0.5f));

        return clip;
    }

    // =========================
    // PROPS
    // =========================

    static Clip PropsClip()
    {
        Clip clip = new Clip("Props");
        const string a = "A_Pad/";

        AnimateMecha(clip, a);
        AnimateGeneral(clip, a);
        AnimateTeam(clip, a);

        // The fire and the blast
        Curve fire = new Curve(0f, 0f, Ease.Hold).To(Explode, 3f);
        for (float t = Explode + 0.6f; t < End; t += 0.17f)
            fire.To(t, 1.3f + 0.5f * Mathf.Sin(t * 13f) + 0.3f * Mathf.Sin(t * 29f));
        clip.Float(a + "Fire", typeof(Light2D), "m_Intensity", fire);
        clip.Float(a + "Fireball", typeof(Light2D), "m_Intensity",
            new Curve(0f, 0f, Ease.Hold).To(Explode, 6f, Ease.Smooth).To(Explode + 0.7f, 0f, Ease.Hold));

        return clip;
    }

    static void AnimateMecha(Clip clip, string a)
    {
        string m = a + "Mecha/";
        float hy = HeadOffset.y;

        // Staggering: a twitch, the head drooping; then over it goes
        Curve bodyX = new Curve(0f, 0f, Ease.Hold);
        foreach (float t in new[] { 0.5f, 0.75f, 1.0f, 1.2f, 1.4f })
            bodyX.To(t, 0.08f, Ease.Hold).To(t + 0.05f, 0f, Ease.Hold);
        clip.Move(m + "Body", bodyX, new Curve(0f, 1.75f, Ease.Hold).To(Fall, 1.75f).To(Thud, 1.35f, Ease.Hold));
        clip.Rotate(m + "Body", new Curve(0f, 0f, Ease.Hold).To(Fall, 0f).To(Thud, -7f, Ease.Hold));

        clip.Move(m + "Head",
            new Curve(0f, HeadOffset.x, Ease.Hold).To(Fall, HeadOffset.x).To(Thud, HeadOffset.x - 0.6f, Ease.Hold),
            new Curve(0f, hy, Ease.Hold).To(Stagger, hy).To(Fall, hy - 0.4f).To(Thud, 2.1f, Ease.Hold));
        clip.Rotate(m + "Head",
            new Curve(0f, 0f, Ease.Hold).To(Stagger, 0f).To(Fall, 12f).To(Thud, 38f, Ease.Hold));

        // Eyes flicker out; the blast leaves it charred
        var eyes = new List<(float, bool)> { (0f, true) };
        foreach (float t in new[] { 1.0f, 1.3f, 1.55f, 1.8f })
        {
            eyes.Add((t, false));
            eyes.Add((t + 0.08f, true));
        }
        eyes.Add((Thud - 0.1f, false));
        clip.Active(m + "Head/Eyes", eyes.ToArray());
        clip.Float(a + "Eye Glow", typeof(Light2D), "m_Intensity",
            new Curve(0f, 1.2f, Ease.Hold).To(Stagger, 1.2f).To(Thud - 0.1f, 0.3f, Ease.Hold).To(Thud, 0f, Ease.Hold));

        foreach (string part in new[] { "Body", "Head" })
            foreach ((string ch, float v) in new[] { ("r", Charred.r), ("g", Charred.g), ("b", Charred.b) })
                clip.Float(m + part, typeof(SpriteRenderer), "m_Color." + ch,
                    new Curve(0f, 1f, Ease.Hold).To(Explode, v, Ease.Hold));

        // Armour flies off in the blast; one more pops at the hatch
        FlyApart(clip, a + "Plate 1", WreckAt, new Vector2(-4f, 7f), 600f, Explode);
        FlyApart(clip, a + "Plate 2", WreckAt, new Vector2(3.5f, 8f), -540f, Explode);
        FlyApart(clip, a + "Plate 3", WreckAt + new Vector2(0.5f, 0f), new Vector2(6f, 5f), 720f, Explode);
        FlyApart(clip, a + "Plate 4", new Vector2(GeneralTop.x, 0.1f), new Vector2(-2.5f, 6.5f), -400f, Hatch);
    }

    static void FlyApart(Clip clip, string path, Vector2 from, Vector2 velocity, float spin, float start)
    {
        const float gravity = 14f;
        float floor = Ground + 0.15f;

        Curve x = new Curve(0f, from.x, Ease.Hold).To(start, from.x, Ease.Linear);
        Curve y = new Curve(0f, from.y, Ease.Hold).To(start, from.y, Ease.Linear);
        Curve angle = new Curve(0f, 0f, Ease.Hold).To(start, 0f, Ease.Linear);

        for (float t = 1f / 30f; t < 3f; t += 1f / 30f)
        {
            float height = from.y + velocity.y * t - 0.5f * gravity * t * t;
            bool landed = height <= floor && velocity.y - gravity * t < 0f;

            x.To(start + t, from.x + velocity.x * t, landed ? Ease.Hold : Ease.Linear);
            y.To(start + t, landed ? floor : height, landed ? Ease.Hold : Ease.Linear);
            angle.To(start + t, spin * t, landed ? Ease.Hold : Ease.Linear);

            if (landed)
                break;
        }

        clip.Move(path, x, y);
        clip.Rotate(path, angle);
        clip.Active(path, (0f, false), (start, true));
    }

    static void AnimateGeneral(Clip clip, string a)
    {
        string gen = a + "General";
        Sprite[] run = { ArtSprite("general_run_0"), ArtSprite("general_run_1") };

        // Up out of the wreck, the rant (with a hop on each threat),
        // down off it and away to his helicopter
        clip.Active(gen, (0f, false), (Climb, true), (Board, false));
        clip.Sprites(gen, new SpriteKeys()
            .Set(0f, ArtSprite("general_shout"))
            .Cycle(Run + 0.2f, Board, 0.1f, run));
        clip.Flags(gen, typeof(SpriteRenderer), "m_FlipX", (0f, true), (Run + 0.2f, false));

        float top = GeneralTop.y;
        clip.Move(gen,
            new Curve(0f, GeneralTop.x, Ease.Hold).To(Run, GeneralTop.x, Ease.Linear).To(Run + 0.35f, 2.6f, Ease.Linear)
                .To(Board - 0.1f, GeneralPadX - 1.4f, Ease.Hold),
            new Curve(0f, -1f, Ease.Hold).To(Climb, -0.6f).To(Climb + 0.25f, top)
                .To(Gen01 + 0.6f, top).To(Gen01 + 0.7f, top + 0.15f).To(Gen01 + 0.85f, top)
                .To(Gen01 + 2.6f, top).To(Gen01 + 2.7f, top + 0.15f).To(Gen01 + 2.85f, top, Ease.Hold)
                .To(Run, top).To(Run + 0.2f, top + 0.3f).To(Run + 0.4f, GeneralY, Ease.Hold));

        // His helicopter: rotor spins up, it lifts, he leans out, it goes
        Sprite[] heli = Heli("GeneralHeli");
        clip.Sprites(a + "General Heli", new SpriteKeys()
            .Set(0f, heli[0]).Cycle(Board, Lift, 0.09f, heli).Cycle(Lift, End, 0.05f, heli));

        Curve hx = new Curve(0f, GeneralPadX, Ease.Hold).To(Away, GeneralPadX + 0.6f, Ease.Linear).To(Team, GeneralPadX + 9f).To(End, GeneralPadX + 40f);
        Curve hy = new Curve(0f, HeliY, Ease.Hold).To(Lift, HeliY).To(Away, HeliY + 3.4f, Ease.Linear).To(Team, HeliY + 6f).To(End, HeliY + 12f);
        clip.Move(a + "General Heli", hx, hy);
        clip.Rotate(a + "General Heli",
            new Curve(0f, 0f, Ease.Hold).To(Lift, 0f).To(Lift + 0.6f, 4f).To(Away, 0f).To(Away + 0.4f, -14f));

        Curve wx = new Curve(0f, GeneralPadX + WindowOffset.x, Ease.Hold).To(Away, GeneralPadX + 0.6f + WindowOffset.x, Ease.Linear);
        Curve wy = new Curve(0f, HeliY + WindowOffset.y, Ease.Hold).To(Lift, HeliY + WindowOffset.y)
            .To(Away, HeliY + 3.4f + WindowOffset.y, Ease.Linear);
        clip.Move(a + "General Window", wx, wy);
        clip.Active(a + "General Window", (0f, false), (Gen02 - 0.3f, true), (Away, false));
    }

    static void AnimateTeam(Clip clip, string a)
    {
        Sprite[] talaRun = { ArtSprite("tala_run_0"), ArtSprite("tala_run_1") };

        // Watching the fight's end; then off-screen until they run in
        clip.Sprites(a + "Rook", new SpriteKeys().Cycle(0f, Team, 0.15f, RookIdle).Cycle(Team, Reach, 0.08f, RookRun));
        clip.Sprites(a + "Tala", new SpriteKeys().Set(0f, ArtSprite("tala_stand")).Cycle(Team, Reach, 0.12f, talaRun));

        clip.Move(a + "Rook", new Curve(0f, RookX, Ease.Hold).To(Team, 5.8f, Ease.Linear).To(Reach, TeamPadX - 1.6f, Ease.Hold),
            Curve.Constant(RookY));
        clip.Move(a + "Tala", new Curve(0f, TalaX, Ease.Hold).To(Team, 4.8f, Ease.Linear).To(Reach + 0.2f, TeamPadX - 2.2f, Ease.Hold),
            Curve.Constant(TalaY));
        clip.Active(a + "Rook", (0f, true), (Reach + 0.1f, false));
        clip.Active(a + "Tala", (0f, true), (Reach + 0.3f, false));

        Sprite[] heli = Heli("PlayerHeli");
        clip.Sprites(a + "Team Heli", new SpriteKeys()
            .Set(0f, heli[0]).Cycle(Spin, Rise, 0.09f, heli).Cycle(Rise, End, 0.05f, heli));
        clip.Move(a + "Team Heli",
            new Curve(0f, TeamPadX, Ease.Hold).To(Rise, TeamPadX).To(Cut, TeamPadX + 2.5f),
            new Curve(0f, HeliY, Ease.Hold).To(Rise, HeliY).To(Cut, HeliY + 3.2f));
        clip.Rotate(a + "Team Heli", new Curve(0f, 0f, Ease.Hold).To(Rise, 0f).To(Rise + 0.8f, -10f));
    }

    // =========================
    // OVERLAY AND LIGHTING
    // =========================

    static Clip OverlayClip()
    {
        Clip clip = new Clip("Overlay");

        clip.Float("Fade", typeof(Image), "m_Color.a",
            new Curve(0f, 1f).To(0.4f, 0f, Ease.Hold).To(Cut, 1f, Ease.Hold));
        clip.Float("Flash", typeof(Image), "m_Color.a",
            new Curve(0f, 0f, Ease.Hold).To(Explode, 0.9f, Ease.Linear).To(Explode + 0.35f, 0f, Ease.Hold));
        clip.Float("Caption", typeof(CanvasGroup), "m_Alpha", Curve.Constant(0f));
        clip.Float("Title", typeof(CanvasGroup), "m_Alpha", Curve.Constant(0f));

        return clip;
    }

    static Clip LightingClip()
    {
        Clip clip = new Clip("Lighting");

        clip.Float("Global Light 2D", typeof(Light2D), "m_Intensity", Curve.Constant(0.85f));
        clip.Float("SlamFX", typeof(Volume), "weight",
            new Curve(0f, 0f, Ease.Hold).To(Thud, 0.3f).To(Thud + 0.25f, 0f, Ease.Hold)
                .To(Explode, 1f).To(Explode + 0.8f, 0f, Ease.Hold));

        return clip;
    }

    // =========================
    // SOUND
    // =========================

    static void AddSound(TimelineAsset timeline)
    {
        AddAudio(timeline, "Music", (0, Sound("mus_heliescape"), 0.8f));
        AddAudio(timeline, "Voice",
            (Gen01, Sound("vo_gen_01"), 0.9f),
            (Gen02, Sound("vo_gen_02"), 0.9f));
        AddAudio(timeline, "Ambience",
            (0, LoadAudio("Assets/Cutscenes/AfterStage1/Audio/amb_base.wav"), 0.4f),
            (Explode, Sound("amb_fire"), 0.5f));
        AddAudio(timeline, "SFX 1",
            (Spark, Sound("sfx_sparks"), 0.6f),
            (Stagger, Sound("sfx_power_down"), 0.7f),
            (Thud, Sound("sfx_thud"), 0.8f));
        AddAudio(timeline, "SFX 2",
            (Explode, Sound("sfx_explosion"), 0.95f),
            (Hatch, Sound("sfx_hatch"), 0.7f));
        AddAudio(timeline, "SFX 3",
            (Run, Sound("sfx_scurry"), 0.6f),
            (Board, Sound("sfx_door_slam"), 0.7f),
            (Board, Sound("sfx_rotor_general"), 0.7f));
        AddAudio(timeline, "SFX 4",
            (Spin, Sound("sfx_rotor_team"), 0.7f));
    }
}
