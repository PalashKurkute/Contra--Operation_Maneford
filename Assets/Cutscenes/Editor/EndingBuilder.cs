using System.Collections.Generic;
using System.Linq;
using TMPro;
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

// Builds the shoot scene for Cutscene_Ending, after the helicopter
// chase (STORY_AND_GAME_STRUCTURE.md, 7): at dawn the General's
// helicopter sputters and crashes in a field. Rook and Tala walk up;
// he crawls out and kneels. "Go on, then. The King... is ready." / "Nobody's
// finishing anything." / "They were never your army. They were just
// lions." / "They were the only subjects... who ever bowed." Tala lifts
// the lion helmet off his head as the first light reaches them: "It's
// over, Leopold." Then sunrise on the ridge: Rook sits among the freed
// lions, one lies down beside him and he rests his hand on its mane;
// far off, a great lion stands on a rock (Sultan, for the epilogue).
//
// Contra > Cutscenes > Ending > Build Shoot Scene rebuilds
// Ending/Ending_Shoot.unity and its Timeline from the art and sound
// made by Tools/cutscene_ending_*. Then press Play in that scene to
// record the master, and run
//     python Tools/cutscene_encode.py Cutscene_Ending 54
public static class EndingBuilder
{
    const string Folder = "Assets/Cutscenes/Ending/";
    const string ScenePath = Folder + "Ending_Shoot.unity";
    const string TimelinePath = Folder + "Ending.playable";
    const string StillsFolder = "Temp/Captures/Ending";
    const string Twilight = "Assets/Cutscenes/AfterStage1/Art/";
    const string Dusk = "Assets/Cutscenes/Intro/Art/";
    const string Escape = "Assets/Cutscenes/HeliEscape/Art/";

    // Shot timing in seconds; Tools/cutscene_ending_audio.py writes
    // the score and effects to the same times
    const float Dive = 1.9f;       // the engine dies; it spins down
    const float Crash = 3.0f;      // cut to the field
    const float Impact = 3.7f;     // it hits
    const float Rest = 4.6f;       // still, nose in the dirt
    const float Approach = 5.8f;   // Rook and Tala walk up, guns raised
    const float Crawl = 10.0f;     // the General crawls out
    const float Kneel = 12.2f;     // and kneels
    const float Two = 13.0f;       // cut to the two of them
    const float Gen01 = 13.4f;     // "Go on, then. The King... is ready."
    const float Rook01 = 18.4f;    // "Nobody's finishing anything."
    const float Lower = 19.6f;     // Rook lowers his gun
    const float Close = 20.8f;     // cut close on the General
    const float Rook02 = 21.2f;    // "They were never your army. They were just lions."
    const float Gen02 = 26.4f;     // "They were the only subjects... who ever bowed."
    const float TalaIn = 30.8f;    // cut: Tala steps in
    const float Reach = 32.0f;     // reaches for the helmet
    const float Lift = 32.4f;      // lifts it off
    const float Tala01 = 33.6f;    // "It's over, Leopold."
    const float Light = 34.0f;     // the first light of the sun reaches them
    const float White = 36.6f;     // fade to white
    const float Ridge = 37.5f;     // sunrise on the ridge
    const float Caption = 38.5f;   // the place caption
    const float LionIn = 40.0f;    // a lion walks up
    const float LionDown = 42.4f;  // and lies down beside him
    const float Touch = 44.0f;     // he rests his hand on its mane
    const float Wide = 46.0f;      // pull back: the sun, the far rock
    const float Black = 52.5f;     // fade to black
    const float End = 54.0f;

    const float Ground = -2.5f;
    static readonly Vector2 AnchorA = new Vector2(-0.5f, -0.4f);
    static readonly Vector2 AnchorC = new Vector2(199.5f, -0.4f);

    static readonly Vector2 WreckAt = new Vector2(5.2f, -1.55f);
    const float HeliScale = 1.3f;
    static readonly Vector2 KneelAt = new Vector2(2.5f, Ground + 39f / 32f);
    static readonly Vector2 HelmetDome = new Vector2(0.06f, 0.66f);   // from the kneel sprite's centre
    const float RookAt = -0.6f, TalaWait = -1.8f, TalaAt = 1.75f;
    static readonly float RookY = Ground + 19f / 16f;
    static readonly float TalaY = Ground + 17f / 16f;

    const float RidgeY = -2.55f;
    static readonly Vector2 SitAt = new Vector2(198.6f, RidgeY + 1.45f);
    static readonly float LionLieY = RidgeY + 14f / 16f;

    static readonly Color Predawn = new Color(0.72f, 0.74f, 0.96f);
    static readonly Color Shade = new Color(0.13f, 0.08f, 0.1f);
    static readonly Color Charred = new Color(0.22f, 0.17f, 0.19f);
    static readonly Color Sunlight = new Color(1f, 0.7f, 0.45f);

    static Sprite ArtSprite(string name) => LoadSprite(Folder + "Art/" + name + ".png");

    static Sprite[] Frames(string sheet, string prefix, int count)
    {
        return Enumerable.Range(0, count).Select(i => LoadSprite(sheet, prefix + i)).ToArray();
    }

    const string Hero = "Assets/Sprites/Hero/Hero.png";
    static Sprite[] RookIdle => Frames(Hero, "Hero_idle_", 5);
    static Sprite[] RookRun => Frames(Hero, "Hero_run_", 6);
    static Sprite RookLowered => LoadSprite(Hero, "Hero_run_down_3");
    // Lion_0..3 are the gallop, Lion_4 lies down, Lion_5 stands tall
    static Sprite LionFrame(int i) => LoadSprite("Assets/Sprites/Lion/Lion.png", "Lion_" + i);

    static Sprite[] Heli => new[] { LoadSprite("Assets/HelicopterStage/Art/GeneralHeli_0.png"),
                                    LoadSprite("Assets/HelicopterStage/Art/GeneralHeli_1.png") };

    static AudioClip Sound(string name) => LoadAudio(Folder + "Audio/" + name + ".wav");

    // =========================
    // BUILD
    // =========================

    [MenuItem("Contra/Cutscenes/Ending/Build Shoot Scene")]
    public static void Build()
    {
        if (!SafeToBuild(ScenePath))
            return;

        StartFromIntro(ScenePath);

        Transform sets = Root("Sets");
        Transform cameras = Root("Cameras");

        Transform sparks = Child(sets, "B_Gate/Sparks");
        Transform smoke = Child(sets, "B_Gate/Smoke");
        Transform fireflies = Child(sets, "B_Gate/Fireflies");
        Transform birds = Child(sets, "A_Aerial/Birds");
        foreach (Transform fx in new[] { sparks, smoke, fireflies, birds })
            fx.SetParent(null, true);

        ClearChildren(sets);

        Transform template = Child(cameras, "CM_Aerial");
        template.SetParent(null, true);
        ClearChildren(cameras);

        Transform a = BuildField(sets, sparks, smoke);
        Transform c = BuildRidge(sets, fireflies, birds);
        Object.DestroyImmediate(sparks.gameObject);

        string[] names = { "CM_Sky", "CM_Crash", "CM_Approach", "CM_Crawl", "CM_Two", "CM_Close",
                           "CM_Tala", "CM_Ridge", "CM_Wide" };
        var cams = new Dictionary<string, CinemachineCamera>();
        foreach (string name in names)
        {
            GameObject cam = Object.Instantiate(template.gameObject, cameras);
            cam.name = name;
            cams[name] = cam.GetComponent<CinemachineCamera>();
        }
        Object.DestroyImmediate(template.gameObject);

        Transform overlay = Root("Overlay");
        Child(overlay, "Caption/Place").GetComponent<TMP_Text>().text = "MANEFORD WILDLIFE RESERVE";
        Child(overlay, "Caption/Time").GetComponent<TMP_Text>().text = "DAY 10  -  06:12";
        Child(overlay, "Caption").GetComponent<CanvasGroup>().alpha = 0f;
        Child(overlay, "Title").GetComponent<CanvasGroup>().alpha = 0f;

        TimelineAsset timeline = NewTimeline(TimelinePath, End);
        PlayableDirector director = ResetDirector(timeline);

        AddShots(timeline, director, Root("Main Camera").GetComponent<CinemachineBrain>(),
            ("Sky", 0, Crash, cams["CM_Sky"]),
            ("Crash", Crash, Approach, cams["CM_Crash"]),
            ("Approach", Approach, Crawl, cams["CM_Approach"]),
            ("Crawl", Crawl, Two, cams["CM_Crawl"]),
            ("Two", Two, Close, cams["CM_Two"]),
            ("Close", Close, TalaIn, cams["CM_Close"]),
            ("Tala", TalaIn, Ridge, cams["CM_Tala"]),
            ("Ridge", Ridge, Wide, cams["CM_Ridge"]),
            ("Wide", Wide, End, cams["CM_Wide"]));

        AddAnimationTrack(timeline, director, "Camera Moves",
            cameras.GetComponent<Animator>(), CameraClip().Save(Folder + "Animation/Cameras.anim"));
        AddAnimationTrack(timeline, director, "Props",
            sets.GetComponent<Animator>(), PropsClip().Save(Folder + "Animation/Props.anim"));
        AddAnimationTrack(timeline, director, "Overlay",
            overlay.GetComponent<Animator>(), OverlayClip().Save(Folder + "Animation/Overlay.anim"));
        AddAnimationTrack(timeline, director, "Lighting",
            Root("Lighting").GetComponent<Animator>(), LightingClip().Save(Folder + "Animation/Lighting.anim"));

        AddControl(timeline, director, "FX Trail", Child(a, "Heli/Trail").gameObject, 0, Impact, 3);
        AddControl(timeline, director, "FX Impact", Child(a, "Impact").gameObject, Impact, Approach, 5);
        AddControl(timeline, director, "FX Dust", Child(a, "Dust").gameObject, Impact, Crawl, 7);
        AddControl(timeline, director, "FX Wreck Smoke", Child(a, "Wreck Smoke").gameObject, Rest, Ridge, 9);
        AddControl(timeline, director, "FX Flames", Child(a, "Flames").gameObject, Impact, Ridge, 15);
        AddControl(timeline, director, "FX Embers", Child(a, "Embers").gameObject, Rest, Ridge, 17);
        AddControl(timeline, director, "FX Motes", Child(c, "Motes").gameObject, Ridge, End, 11);
        AddControl(timeline, director, "FX Birds", Child(c, "Birds").gameObject, Ridge + 1.5f, End, 13);

        AddSound(timeline);
        AddRecorder(timeline, "Cutscene_Ending");

        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("🎬 Built " + ScenePath);
    }

    [MenuItem("Contra/Cutscenes/Ending/Render Stills")]
    public static void RenderKeyStills()
    {
        RenderAt(1.0f, 2.6f, 3.9f, 5.0f, 8.0f, 11.0f, 12.6f, 15.0f, 20.0f, 23.0f, 28.0f,
                 32.2f, 33.4f, 35.5f, 39.5f, 43.0f, 44.8f, 47.5f, 51.5f);
    }

    public static void RenderAt(params float[] times)
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);

        RenderStills(Root("Shoot").GetComponent<PlayableDirector>(), times, StillsFolder);
        Debug.Log("🖼️ Stills in " + StillsFolder);
    }

    // =========================
    // SET A: THE FIELD BEFORE DAWN
    // =========================

    static Transform BuildField(Transform sets, Transform sparks, Transform smoke)
    {
        Transform a = AddGroup("A_Field", sets);

        AddParallax("Sky", a, ArtSprite("sky_predawn"), new Vector2(1f, -3.7f), 1f, AnchorA, -100);
        AddParallax("Mist", a, LoadSprite(Twilight + "tw_mist.png"), new Vector2(0f, -5.1f), 0.85f, AnchorA, -90);
        AddParallax("Hills", a, LoadSprite(Twilight + "tw_hills.png"), new Vector2(0f, -2.2f), 0.7f, AnchorA, -80);
        AddParallax("Pines Far", a, LoadSprite(Twilight + "tw_pines_far.png"), new Vector2(0f, -1.45f), 0.5f, AnchorA, -70);
        AddParallax("Pines Near", a, LoadSprite(Twilight + "tw_pines_near.png"), new Vector2(0f, -1.4f), 0.3f, AnchorA, -60, "Default");
        AddSprite("Tree Left", a, LoadSprite(Twilight + "tw_tree_1.png"), new Vector2(-10.2f, Ground + 3.3f), -10);
        AddSprite("Tree Right", a, LoadSprite(Twilight + "tw_tree_2.png"), new Vector2(9.6f, Ground + 3.3f), -10);
        AddSprite("Ground", a, LoadSprite(Twilight + "clearing_ground.png"), new Vector2(0f, Ground - 1.5f), 0);
        AddParallax("Foreground Grass", a, LoadSprite(Twilight + "fg_grass.png"), new Vector2(0f, Ground - 1.2f),
            -0.25f, AnchorA, 20, "Foreground");

        // The General's helicopter, trailing smoke
        SpriteRenderer heli = AddSprite("Heli", a, Heli[0], new Vector2(-10f, 4.6f), 6, scale: HeliScale, flipX: true);
        GameObject trail = Object.Instantiate(smoke.gameObject, heli.transform);
        trail.name = "Trail";
        trail.transform.localPosition = new Vector2(1.2f, 0.1f);
        trail.transform.localScale = Vector3.one / HeliScale;
        ParticleSystem.MainModule trailMain = trail.GetComponent<ParticleSystem>().main;
        trailMain.simulationSpace = ParticleSystemSimulationSpace.World;
        trail.SetActive(false);

        AddCharacter("Rook", a, RookRun[0], new Vector2(-8f, RookY), 15, Predawn);
        AddSprite("Tala", a, LoadSprite(Escape + "tala_run_0.png"), new Vector2(-9.2f, TalaY), 13, tint: Predawn);

        SpriteRenderer general = AddSprite("General", a, ArtSprite("general_crawl"),
            new Vector2(3.9f, Ground + 12f / 16f), 9, tint: Predawn, flipX: true);
        AddSprite("Helmet", general.transform, ArtSprite("helmet"), Vector2.zero, 10, tint: Predawn, flipX: true)
            .gameObject.SetActive(false);
        general.gameObject.SetActive(false);

        AddLight("Fire", a, WreckAt + new Vector2(-0.8f, -0.2f), new Color(1f, 0.55f, 0.2f), 0f, 6f, falloff: 0.4f);
        AddLight("Dawn", a, new Vector2(6f, 0.5f), Sunlight, 0f, 12f, falloff: 0.25f);

        // The crash: a burst of sparks, a cloud of dust, then smoke
        GameObject impact = Object.Instantiate(sparks.gameObject, a);
        impact.name = "Impact";
        impact.transform.localPosition = new Vector2(4.6f, -1.8f);
        ParticleSystem ips = impact.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ips.main;
        main.loop = false;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.gravityModifier = 1f;
        ParticleSystem.EmissionModule emission = ips.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
        impact.SetActive(false);

        foreach ((string name, Vector2 at) in new[] { ("Dust", new Vector2(4.6f, Ground + 0.3f)),
                                                     ("Wreck Smoke", WreckAt + new Vector2(-0.4f, 0.4f)) })
        {
            GameObject fx = Object.Instantiate(smoke.gameObject, a);
            fx.name = name;
            fx.transform.localPosition = at;
            fx.SetActive(false);
        }
        Object.DestroyImmediate(smoke.gameObject);

        Stream(sparks, a, "Flames", WreckAt + new Vector2(-0.3f, -0.3f), 30, 0.5f, -0.5f,
            new Vector2(0.6f, 1.4f), new Vector2(0.08f, 0.18f), new Color(1f, 0.85f, 0.4f), new Color(1f, 0.35f, 0.08f));
        Stream(sparks, a, "Embers", WreckAt + new Vector2(-1f, -0.5f), 10, 2.2f, -0.12f,
            new Vector2(0.3f, 0.9f), new Vector2(0.04f, 0.08f), new Color(1f, 0.7f, 0.3f), new Color(1f, 0.4f, 0.1f));

        // Over Rook's shoulder for the standoff: him, big and dark, in
        // the foreground, the rifle on the General
        AddCharacter("Rook OTS", a, RookIdle[0], new Vector2(-0.1f, -1.75f), 30, new Color(0.3f, 0.27f, 0.36f))
            .transform.localScale = Vector3.one * CharacterScale * 1.6f;

        return a;
    }

    // A steady stream of the intro's sparks, rising (negative gravity)
    static void Stream(Transform sparks, Transform set, string name, Vector2 position, float rate, float radius,
        float gravity, Vector2 speed, Vector2 size, Color from, Color to)
    {
        GameObject go = Object.Instantiate(sparks.gameObject, set);
        go.name = name;
        go.transform.localPosition = position;
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.gravityModifier = gravity;
        main.maxParticles = 128;
        main.startColor = new ParticleSystem.MinMaxGradient(from, to);
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = rate;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new ParticleSystem.Burst[0]);
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.rotation = Vector3.zero;
        go.SetActive(false);
    }

    // =========================
    // SET C: SUNRISE ON THE RIDGE
    // =========================

    static Transform BuildRidge(Transform sets, Transform fireflies, Transform birds)
    {
        Transform c = AddGroup("C_Ridge", sets);

        AddParallax("Sky", c, ArtSprite("sky_sunrise"), new Vector2(201f, -3.7f), 1f, AnchorC, -100);
        AddParallax("Sun", c, ArtSprite("sun"), new Vector2(203f, -2.6f), 0.95f, AnchorC, -95);
        AddParallax("Cloud 1", c, LoadSprite(Dusk + "cloud_1.png"), new Vector2(196f, 1.9f), 0.9f, AnchorC, -97,
            tint: new Color(1f, 0.82f, 0.78f));
        AddParallax("Cloud 2", c, LoadSprite(Dusk + "cloud_2.png"), new Vector2(204f, 2.6f), 0.92f, AnchorC, -97,
            tint: new Color(1f, 0.86f, 0.8f));
        AddParallax("Mist", c, LoadSprite(Dusk + "layer_mist.png"), new Vector2(200f, -5.1f), 0.85f, AnchorC, -90);
        AddParallax("Hills", c, LoadSprite(Dusk + "layer_hills.png"), new Vector2(200f, -2.2f), 0.7f, AnchorC, -80);

        // Far off: a rock, and on it a great lion
        Transform rock = AddParallax("Far Rock", c, ArtSprite("far_rock"), new Vector2(205.6f, -1.2f), 0.55f, AnchorC, -76);
        SpriteRenderer sultan = AddCharacter("Sultan", rock.GetChild(0), LionFrame(5), new Vector2(-0.15f, 1.35f),
            -75, new Color(0.23f, 0.13f, 0.18f), layer: "Background");
        sultan.transform.localScale = Vector3.one * CharacterScale * 0.42f;

        AddParallax("Pines Far", c, LoadSprite(Dusk + "layer_pines_far.png"), new Vector2(200f, -1.45f), 0.5f, AnchorC, -70);

        AddLight("Sun Glow", c, new Vector2(203f, -2.2f), Sunlight, 1.2f, 14f, 360f, 0f, 0.25f, 0.3f, "Background");

        AddSprite("Ridge", c, ArtSprite("ridge"), new Vector2(200f, RidgeY - 0.85f), 5);
        AddSprite("Rook", c, ArtSprite("rook_sit_0"), SitAt, 7);

        AddCharacter("Lion A", c, LionFrame(4), new Vector2(195.4f, LionLieY), 6, Shade, flipX: true);
        AddCharacter("Lion B", c, LionFrame(4), new Vector2(203.6f, LionLieY + 0.1f), 6, Shade);
        AddCharacter("Lion C", c, LionFrame(1), new Vector2(206.5f, LionLieY + 0.15f), 8, Shade);

        fireflies.SetParent(c, false);
        fireflies.name = "Motes";
        fireflies.localPosition = new Vector2(199.5f, -1f);
        ParticleSystem.MainModule motes = fireflies.GetComponent<ParticleSystem>().main;
        motes.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.55f, 0.8f), new Color(1f, 0.7f, 0.4f, 0.5f));
        fireflies.gameObject.SetActive(false);

        birds.SetParent(c, false);
        birds.localPosition = new Vector2(203.5f, 1.8f);
        birds.gameObject.SetActive(false);

        return c;
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

        Shot(clip, "CM_Sky",
            new Curve(0f, -3.5f, Ease.Linear).To(Crash, -0.8f),
            new Curve(0f, 3.4f, Ease.Linear).To(Crash, 2.6f),
            Curve.Constant(3.4f),
            new Curve(0f, 0.2f).To(Dive, 0.3f).To(Crash, 0.8f));

        Shot(clip, "CM_Crash",
            new Curve(Crash, 3f, Ease.Hold),
            new Curve(Crash, -0.2f, Ease.Linear).To(Approach, -0.4f),
            new Curve(Crash, 3.3f, Ease.Linear).To(Approach, 3.1f),
            new Curve(Crash, 0.15f, Ease.Hold).To(Impact, 0.15f, Ease.Smooth).To(Impact + 0.05f, 4.5f)
                .To(Rest, 0.8f).To(Approach, 0.1f));

        Shot(clip, "CM_Approach",
            new Curve(Approach, -2.4f, Ease.Linear).To(Crawl, 0.2f),
            Curve.Constant(-0.85f),
            new Curve(Approach, 2.7f, Ease.Linear).To(Crawl, 2.55f),
            Curve.Constant(0.12f));

        Shot(clip, "CM_Crawl",
            new Curve(Crawl, 3.3f, Ease.Linear).To(Two, 2.9f),
            Curve.Constant(-1.2f),
            new Curve(Crawl, 1.7f, Ease.Linear).To(Two, 1.55f),
            Curve.Constant(0.15f));

        Shot(clip, "CM_Two",
            new Curve(Two, 1.5f, Ease.Linear).To(Close, 1.7f),
            new Curve(Two, -0.95f, Ease.Linear).To(Close, -0.85f),
            new Curve(Two, 2.1f, Ease.Hold).To(Two + 0.02f, 2.1f).To(Two + 0.18f, 1.8f).To(Lower, 1.68f).To(Close, 1.6f),
            new Curve(Two, 1.4f).To(Two + 0.5f, 0.18f).To(Close, 0.15f));

        Shot(clip, "CM_Close",
            new Curve(Close, 2.35f, Ease.Linear).To(TalaIn, 2.45f),
            new Curve(Close, -0.95f, Ease.Linear).To(TalaIn, -0.85f),
            new Curve(Close, 1.35f, Ease.Linear).To(TalaIn, 1.15f),
            Curve.Constant(0.12f));

        Shot(clip, "CM_Tala",
            new Curve(TalaIn, 1.6f, Ease.Linear).To(Ridge, 2.1f),
            new Curve(TalaIn, -0.95f, Ease.Linear).To(Ridge, -0.75f),
            new Curve(TalaIn, 1.9f, Ease.Linear).To(Ridge, 1.65f),
            Curve.Constant(0.12f));

        Shot(clip, "CM_Ridge",
            new Curve(Ridge, 199.6f, Ease.Linear).To(Wide, 199.8f),
            new Curve(Ridge, -1.1f, Ease.Linear).To(Wide, -1.2f),
            new Curve(Ridge, 2.75f, Ease.Linear).To(Wide, 2.45f),
            Curve.Constant(0.08f));

        Shot(clip, "CM_Wide",
            new Curve(Wide, 199.9f).To(Black, 201.6f).To(End, 201.7f),
            new Curve(Wide, -1.15f).To(Black, -0.1f).To(End, 0f),
            new Curve(Wide, 2.6f).To(Black, 3.6f).To(End, 3.7f),
            Curve.Constant(0.04f));

        return clip;
    }

    // =========================
    // PROPS
    // =========================

    static Clip PropsClip()
    {
        Clip clip = new Clip("Props");

        clip.Active("A_Field", (0f, true), (Ridge, false));
        clip.Active("C_Ridge", (0f, false), (Ridge, true));

        AnimateCrash(clip);
        AnimateTalk(clip);
        AnimateRidge(clip);

        return clip;
    }

    static void AnimateCrash(Clip clip)
    {
        const string a = "A_Field/";

        // Sputtering across the sky, then spinning down into the field
        clip.Move(a + "Heli",
            new Curve(0f, -10f, Ease.Linear).To(Dive, -2.5f).To(Crash - 0.01f, 2.5f, Ease.Hold)
                .To(Crash, 1.4f, Ease.Linear).To(Impact, 4.6f, Ease.Linear).To(Impact + 0.25f, 4.9f).To(Rest, WreckAt.x),
            new Curve(0f, 4.6f).To(0.8f, 4.3f).To(1.4f, 4.6f).To(Dive, 4.2f, Ease.Linear).To(Crash - 0.01f, 0.8f, Ease.Hold)
                .To(Crash, 6.5f, Ease.Linear).To(Impact, -1.2f, Ease.Linear).To(Impact + 0.25f, -0.9f).To(Rest, WreckAt.y));
        clip.Rotate(a + "Heli",
            new Curve(0f, 0f).To(0.6f, 8f).To(1.1f, -6f).To(1.6f, 9f).To(Dive, 0f, Ease.Linear)
                .To(Crash, -560f, Ease.Linear).To(Impact, -760f, Ease.Linear).To(Impact + 0.25f, -745f).To(Rest, -738f));
        clip.Sprites(a + "Heli", new SpriteKeys().Cycle(0f, Dive, 0.06f, Heli).Cycle(Dive, Rest, 0.14f, Heli).Set(Rest, Heli[0]));
        foreach ((string ch, float v) in new[] { ("r", Charred.r), ("g", Charred.g), ("b", Charred.b) })
            clip.Float(a + "Heli", typeof(SpriteRenderer), "m_Color." + ch,
                new Curve(0f, Predawn[ch == "r" ? 0 : ch == "g" ? 1 : 2], Ease.Hold).To(Impact, v, Ease.Hold));

        Curve fire = new Curve(0f, 0f, Ease.Hold).To(Impact, 2.2f);
        for (float t = Impact + 0.5f; t < Ridge; t += 0.13f)
            fire.To(t, 1.0f + 0.3f * Mathf.Sin(t * 11f) + 0.2f * Mathf.Sin(t * 27f));
        clip.Float(a + "Fire", typeof(Light2D), "m_Intensity", fire);
    }

    static void AnimateTalk(Clip clip)
    {
        const string a = "A_Field/";

        // Rook walks up, aims, and lowers his gun
        clip.Sprites(a + "Rook", new SpriteKeys()
            .Cycle(0f, Approach + 3.8f, 0.13f, RookRun)
            .Cycle(Approach + 3.8f, Lower, 0.16f, RookIdle)
            .Set(Lower, RookLowered));
        clip.Move(a + "Rook", new Curve(0f, -8f, Ease.Hold).To(Approach, -8f, Ease.Linear).To(Approach + 3.8f, RookAt, Ease.Hold),
            Bob(new Curve(0f, RookY, Ease.Hold).To(Approach + 3.8f, RookY), RookY, Approach + 3.8f, Ridge, 1f));

        // Tala behind him; later she walks past to the General
        Sprite[] talaWalk = { LoadSprite(Escape + "tala_run_0.png"), LoadSprite(Escape + "tala_run_1.png") };
        clip.Sprites(a + "Tala", new SpriteKeys()
            .Cycle(0f, Approach + 4f, 0.17f, talaWalk)
            .Set(Approach + 4f, LoadSprite(Escape + "tala_stand.png"))
            .Cycle(TalaIn, Reach, 0.2f, talaWalk)
            .Set(Reach, ArtSprite("tala_reach"))
            .Set(Lift + 0.8f, ArtSprite("tala_hold"))
            .Cycle(Tala01, Tala01 + 2.1f, 0.12f, ArtSprite("tala_hold_talk"), ArtSprite("tala_hold"))
            .Set(Tala01 + 2.1f, ArtSprite("tala_hold")));
        clip.Move(a + "Tala",
            new Curve(0f, -9.2f, Ease.Hold).To(Approach, -9.2f, Ease.Linear).To(Approach + 4f, TalaWait, Ease.Hold)
                .To(TalaIn, TalaWait, Ease.Linear).To(Reach, TalaAt, Ease.Hold),
            Bob(new Curve(0f, TalaY, Ease.Hold).To(Approach + 4f, TalaY), TalaY, Approach + 4f, TalaIn, 1.25f)
                .To(TalaIn, TalaY, Ease.Hold));

        // The General: crawls out from behind the wreck, kneels; his
        // face falls; Tala lifts the helmet off
        string gen = a + "General";
        clip.Active(gen, (0f, false), (Crawl, true));

        clip.Active(a + "Rook", (0f, true), (Two, false), (Close, true));
        clip.Active(a + "Tala", (0f, true), (Two, false), (Close, true));
        clip.Active(a + "Rook OTS", (0f, false), (Two, true), (Close, false));
        clip.Sprites(a + "Rook OTS", new SpriteKeys().Cycle(Two, Lower, 0.16f, RookIdle).Set(Lower, RookLowered));
        clip.Move(a + "Rook OTS", Curve.Constant(-0.1f), Bob(new Curve(0f, -1.75f, Ease.Hold), -1.75f, Two, Close, 1.1f, 0.1f));
        clip.Sprites(gen, new SpriteKeys()
            .Set(0f, ArtSprite("general_crawl"))
            .Set(Kneel, ArtSprite("general_kneel"))
            .Cycle(Gen01, Gen01 + 4.6f, 0.13f, ArtSprite("general_kneel_talk"), ArtSprite("general_kneel"))
            .Set(Gen01 + 4.6f, ArtSprite("general_kneel"))
            .Set(Gen02 - 0.4f, ArtSprite("general_kneel_sad"))
            .Cycle(Gen02, Gen02 + 3.3f, 0.15f, ArtSprite("general_kneel_sad_talk"), ArtSprite("general_kneel_sad"))
            .Set(Gen02 + 3.3f, ArtSprite("general_kneel_sad")));
        float crawlY = Ground + 12f / 16f;
        Curve genY = new Curve(0f, crawlY, Ease.Hold).To(Kneel, KneelAt.y);
        Bob(genY, KneelAt.y, Kneel + 0.4f, Gen02, 1.1f);
        Bob(genY, KneelAt.y, Gen02, Lift, 0.42f);          // his shoulders shake
        clip.Move(gen,
            new Curve(0f, 3.9f, Ease.Hold).To(Crawl, 3.9f).To(Kneel - 0.2f, KneelAt.x + 0.1f, Ease.Hold).To(Kneel, KneelAt.x, Ease.Hold),
            genY.To(Lift + 0.1f, KneelAt.y, Ease.Hold));

        // The helmet rides on his head (as his child) until Tala lifts it
        Vector2 lifted = new Vector2(-0.15f, 0.45f);
        Vector2 held = new Vector2(TalaAt, TalaY) + new Vector2(0.28f, 0.09f) - HelmetDome - KneelAt;
        string helmet = gen + "/Helmet";
        clip.Active(helmet, (0f, false), (Kneel, true));
        clip.Move(helmet,
            new Curve(0f, 0f, Ease.Hold).To(Lift, 0f).To(Lift + 0.6f, lifted.x).To(Lift + 0.8f, lifted.x).To(Lift + 1.1f, held.x),
            new Curve(0f, 0f, Ease.Hold).To(Gen02 - 0.4f, -1f / 16f, Ease.Hold)
                .To(Lift, -1f / 16f).To(Lift + 0.6f, lifted.y).To(Lift + 0.8f, lifted.y).To(Lift + 1.1f, held.y));

        // The first light of the sun, from the right
        clip.Float(a + "Dawn", typeof(Light2D), "m_Intensity",
            new Curve(0f, 0f, Ease.Hold).To(Light, 0f).To(White, 3.2f));
    }

    static void AnimateRidge(Clip clip)
    {
        const string c = "C_Ridge/";

        // The sun clears the hills
        clip.Move(c + "Sun", Curve.Constant(203f), new Curve(Ridge, -2.9f, Ease.Linear).To(End, 0.4f));
        clip.Move(c + "Sun Glow", Curve.Constant(203f), new Curve(Ridge, -2.5f, Ease.Linear).To(End, 0.8f));
        clip.Float(c + "Sun Glow", typeof(Light2D), "m_Intensity",
            new Curve(Ridge, 0.9f, Ease.Linear).To(Wide, 1.3f).To(End, 1.6f));

        // A lion walks up and lies down beside him; he reaches out
        clip.Sprites(c + "Lion C", new SpriteKeys()
            .Set(0f, LionFrame(1))
            .Cycle(LionIn, LionDown - 0.6f, 0.25f, LionFrame(1), LionFrame(2))
            .Set(LionDown - 0.6f, LionFrame(1))
            .Set(LionDown, LionFrame(4)));
        clip.Move(c + "Lion C",
            new Curve(0f, 206.5f, Ease.Hold).To(LionIn, 206.5f, Ease.Linear).To(LionDown - 0.6f, 201.2f, Ease.Hold),
            Bob(new Curve(0f, LionLieY + 0.15f, Ease.Hold).To(LionDown, LionLieY + 0.15f).To(LionDown + 0.3f, LionLieY),
                LionLieY, LionDown + 0.3f, End, 1.3f));

        clip.Sprites(c + "Rook", new SpriteKeys()
            .Cycle(Ridge, Touch, 0.5f, ArtSprite("rook_sit_0"), ArtSprite("rook_sit_0w"))
            .Cycle(Touch, End, 0.5f, ArtSprite("rook_sit_1"), ArtSprite("rook_sit_1w")));
        clip.Move(c + "Rook", Curve.Constant(SitAt.x), Bob(new Curve(0f, SitAt.y, Ease.Hold), SitAt.y, Ridge, End, 1.6f));

        clip.Move(c + "Lion A", Curve.Constant(195.4f), Bob(new Curve(0f, LionLieY, Ease.Hold), LionLieY, Ridge, End, 1.5f));
        clip.Move(c + "Lion B", Curve.Constant(203.6f),
            Bob(new Curve(0f, LionLieY + 0.1f, Ease.Hold), LionLieY + 0.1f, Ridge + 0.4f, End, 1.8f));

        clip.Move(c + "Cloud 1", new Curve(Ridge, 196f, Ease.Linear).To(End, 198.6f), Curve.Constant(1.9f));
        clip.Move(c + "Cloud 2", new Curve(Ridge, 204f, Ease.Linear).To(End, 206.2f), Curve.Constant(2.6f));
    }

    // A breath (or, with a short period, a sob): y rises a pixel and
    // settles, from 'from' until 'to'. The curve must be at y by 'from'.
    static Curve Bob(Curve curve, float y, float from, float to, float period, float amp = 1f / 16f)
    {
        bool up = true;
        for (float t = from + period / 2f; t < to - 0.01f; t += period / 2f, up = !up)
            curve.To(t, up ? y + amp : y);
        return curve;
    }

    // =========================
    // OVERLAY AND LIGHTING
    // =========================

    static Clip OverlayClip()
    {
        Clip clip = new Clip("Overlay");

        clip.Float("Fade", typeof(Image), "m_Color.a",
            new Curve(0f, 1f).To(0.8f, 0f, Ease.Hold).To(Black, 0f).To(End - 0.2f, 1f, Ease.Hold));

        // Into white as the sun reaches them, and out of it on the ridge
        clip.Float("Flash", typeof(Image), "m_Color.a",
            new Curve(0f, 0f, Ease.Hold).To(Impact, 0.5f, Ease.Linear).To(Impact + 0.25f, 0f, Ease.Hold)
                .To(White, 0f).To(Ridge - 0.1f, 1f, Ease.Hold).To(Ridge, 1f).To(Ridge + 1.4f, 0f, Ease.Hold));

        clip.Float("Caption", typeof(CanvasGroup), "m_Alpha",
            new Curve(0f, 0f, Ease.Hold).To(Caption - 0.05f, 1f, Ease.Hold).To(43.2f, 1f).To(44f, 0f, Ease.Hold));
        clip.Float("Caption/Place", typeof(TextReveal), "progress",
            new Curve(0f, 0f, Ease.Hold).To(Caption, 0f, Ease.Linear).To(Caption + 1.1f, 1f, Ease.Hold));
        clip.Float("Caption/Time", typeof(TextReveal), "progress",
            new Curve(0f, 0f, Ease.Hold).To(Caption + 1.2f, 0f, Ease.Linear).To(Caption + 2f, 1f, Ease.Hold));
        clip.Float("Title", typeof(CanvasGroup), "m_Alpha", Curve.Constant(0f));

        return clip;
    }

    static Clip LightingClip()
    {
        Clip clip = new Clip("Lighting");

        // Grey before dawn, warming as the light arrives
        clip.Float("Global Light 2D", typeof(Light2D), "m_Intensity",
            new Curve(0f, 0.8f, Ease.Hold).To(Light, 0.8f).To(White, 1f, Ease.Hold));
        clip.Float("SlamFX", typeof(Volume), "weight",
            new Curve(0f, 0f, Ease.Hold).To(Impact, 0.9f).To(Impact + 0.8f, 0f, Ease.Hold));

        return clip;
    }

    // =========================
    // SOUND
    // =========================

    static void AddSound(TimelineAsset timeline)
    {
        AddAudio(timeline, "Music", (0, Sound("mus_ending"), 0.85f));
        AddAudio(timeline, "Voice",
            (Gen01, Sound("vo_gen_01"), 0.95f),
            (Rook01, Sound("vo_rook_01"), 0.95f),
            (Rook02, Sound("vo_rook_02"), 0.95f),
            (Gen02, Sound("vo_gen_02"), 0.95f),
            (Tala01, Sound("vo_tala_01"), 0.95f));
        AddAudio(timeline, "Ambience",
            (0, Sound("amb_field"), 0.4f),
            (Ridge, Sound("amb_dawn"), 0.5f));
        AddAudio(timeline, "Fire",
            (Impact, LoadAudio("Assets/Cutscenes/HeliEscape/Audio/amb_fire.wav"), 0.25f));
        AddAudio(timeline, "SFX 1",
            (0, Sound("sfx_sputter"), 0.75f),
            (Impact, Sound("sfx_crash"), 0.95f),
            (Impact + 0.1f, Sound("sfx_ring"), 0.4f));
        AddAudio(timeline, "SFX 2",
            (Approach, Sound("sfx_steps"), 0.5f),
            (Crawl, Sound("sfx_crawl"), 0.6f),
            (Lower, Sound("sfx_gun_lower"), 0.5f),
            (Lift, Sound("sfx_helmet"), 0.5f));
        AddAudio(timeline, "SFX 3",
            (Two, Sound("sfx_aim"), 0.7f),
            (Caption, Sound("sfx_type_1"), 0.45f),
            (Caption + 1.2f, Sound("sfx_type_2"), 0.45f),
            (LionIn, LoadAudio("Assets/Cutscenes/AfterStage1/Audio/sfx_lion_walk.wav"), 0.4f),
            (LionDown + 0.3f, LoadAudio("Assets/Cutscenes/AfterStage1/Audio/sfx_chuff.wav"), 0.5f),
            (Touch + 0.3f, Sound("sfx_purr"), 0.5f));
    }
}
