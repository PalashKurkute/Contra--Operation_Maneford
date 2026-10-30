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

// Builds the shoot scene for Cutscene_AfterStage1, the cutscene
// between Stage 1 and Stage 2 (STORY_AND_GAME_STRUCTURE.md, 7.1):
// the last collar in the pack shatters, the freed lion looks back
// at Rook and runs into the forest; far off, searchlights sweep the
// General's compound, his voice comes over the loudspeakers
// ("He freed my lions? Fine. Release the soldiers."), the alarm
// goes off and the soldiers march out.
//
// Contra > Cutscenes > AfterStage1 > Build Shoot Scene rebuilds
// AfterStage1/AfterStage1_Shoot.unity and its Timeline from the art
// and sound made by Tools/cutscene_afterstage1_*. Then press Play in
// that scene to record the master, and run
//     python Tools/cutscene_encode.py Cutscene_AfterStage1 24.4
public static class AfterStage1Builder
{
    const string Folder = "Assets/Cutscenes/AfterStage1/";
    const string ScenePath = Folder + "AfterStage1_Shoot.unity";
    const string TimelinePath = Folder + "AfterStage1.playable";
    const string StillsFolder = "Temp/Captures/AfterStage1";

    // Shot timing in seconds; Tools/cutscene_afterstage1_audio.py
    // writes the score and effects to the same times
    const float Shatter = 2.6f;    // the last collar breaks
    const float Shake = 3.1f;      // the lion shakes its head
    const float TwoShot = 3.8f;    // cut to Rook and the lion
    const float Rise = 4.2f;       // it gets up
    const float Walk = 4.7f;       // turns away, a few steps
    const float Stop = 5.6f;
    const float Look = 5.85f;      // looks back at Rook
    const float Run = 7.15f;       // runs into the forest
    const float Roar = 8.7f;       // a roar far off; searchlights over the hills
    const float Base = 9.8f;       // cut to the compound at night
    const float Caption = 10.3f;   // the place caption types on
    const float PaOn = 11.85f;     // the loudspeakers click on
    const float Gen01 = 12.35f;    // "He freed my lions?"
    const float Banner = 14.4f;    // cut to the banner
    const float Gen02 = 15.1f;     // "Fine."
    const float Gen03 = 16.4f;     // "Release the soldiers."
    const float Alarm = 18.5f;     // alarm beacons and klaxon
    const float Gate = 19.3f;      // cut to the gate
    const float GateOpen = 19.5f;  // the gate rolls open
    const float March = 20.1f;     // the soldiers march out
    const float Cut = 23.6f;       // hard cut to black on the last hit
    const float End = 24.4f;

    // Ground lines and parallax anchors (the camera position at
    // which each set's layers sit where they're placed)
    const float GroundA = -2f;
    const float GroundC = -2.5f;
    static readonly Vector2 AnchorA = new Vector2(-0.5f, -0.4f);
    static readonly Vector2 AnchorB = new Vector2(100f, 0.8f);
    static readonly Vector2 AnchorC = new Vector2(200f, 2.5f);

    // Where the lion lies, and its collar on the neck
    static readonly Vector2 LionAt = new Vector2(1f, GroundA + 14f / 16f);
    static readonly Vector2 NeckOffset = new Vector2(3.5f / 16f, 1f / 16f);

    static readonly Color Twilight = new Color(0.72f, 0.74f, 0.96f);
    static readonly Color Silhouette = new Color(0.1f, 0.1f, 0.16f);
    static readonly Color SoldierTint = new Color(0.3f, 0.3f, 0.34f);
    static readonly Color Searchlight = new Color(1f, 0.96f, 0.85f);
    static readonly Color Sodium = new Color(1f, 0.78f, 0.5f);
    static readonly Color AlarmRed = new Color(1f, 0.12f, 0.08f);

    static Sprite ArtSprite(string name)
    {
        return LoadSprite(Folder + "Art/" + name + ".png");
    }

    // Lion_0..3 are the gallop, Lion_4 lies down, Lion_5 stands tall
    static Sprite LionFrame(int i)
    {
        return LoadSprite("Assets/Sprites/Lion/Lion.png", "Lion_" + i);
    }

    static Sprite[] Frames(string sheet, string prefix, int count)
    {
        return Enumerable.Range(0, count).Select(i => LoadSprite(sheet, prefix + i)).ToArray();
    }

    static Sprite[] RookIdle => Frames("Assets/Sprites/Hero/Hero.png", "Hero_idle_", 5);
    static Sprite[] SoldierRun => Frames("Assets/Sprites/EnemySoldier/EnemySoldier.png", "Soldier_run_", 6);

    static AudioClip Sound(string name)
    {
        return LoadAudio(Folder + "Audio/" + name + ".wav");
    }

    // =========================
    // BUILD
    // =========================

    [MenuItem("Contra/Cutscenes/AfterStage1/Build Shoot Scene")]
    public static void Build()
    {
        if (!SafeToBuild(ScenePath))
            return;

        StartFromIntro(ScenePath);

        Transform sets = Root("Sets");
        Transform cameras = Root("Cameras");

        // Keep the intro's particle systems (tuned already) and one
        // of its cameras as a template, then clear the intro out
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

        Transform a = BuildClearing(sets, sparks, smoke, fireflies, birds);
        BuildFar(sets);
        BuildCompound(sets);

        Dictionary<string, CinemachineCamera> cams = BuildCameras(cameras, template);
        Object.DestroyImmediate(template.gameObject);

        SetUpOverlay();

        TimelineAsset timeline = NewTimeline(TimelinePath, End);
        PlayableDirector director = ResetDirector(timeline);

        AddShots(timeline, director, Root("Main Camera").GetComponent<CinemachineBrain>(),
            ("Collar", 0, TwoShot, cams["CM_Collar"]),
            ("Look Back", TwoShot, Base, cams["CM_TwoShot"]),
            ("Compound", Base, Banner, cams["CM_Far"]),
            ("Banner", Banner, Gate, cams["CM_Banner"]),
            ("Gate", Gate, End, cams["CM_Gate"]));

        AddAnimationTrack(timeline, director, "Camera Moves",
            cameras.GetComponent<Animator>(), CameraClip().Save(Folder + "Animation/Cameras.anim"));
        AddAnimationTrack(timeline, director, "Props",
            sets.GetComponent<Animator>(), PropsClip().Save(Folder + "Animation/Props.anim"));
        AddAnimationTrack(timeline, director, "Overlay",
            Root("Overlay").GetComponent<Animator>(), OverlayClip().Save(Folder + "Animation/Overlay.anim"));
        AddAnimationTrack(timeline, director, "Lighting",
            Root("Lighting").GetComponent<Animator>(), LightingClip().Save(Folder + "Animation/Lighting.anim"));

        AddControl(timeline, director, "FX Sparks", sparks.gameObject, 0.4f, Shatter + 0.02f, 3);
        AddControl(timeline, director, "FX Shards", Child(a, "Shards").gameObject, Shatter, TwoShot, 5);
        AddControl(timeline, director, "FX Smoke", smoke.gameObject, Shatter + 0.05f, Base, 7);
        AddControl(timeline, director, "FX Birds", birds.gameObject, Shatter + 0.06f, 6.6f, 11);
        AddControl(timeline, director, "FX Fireflies", fireflies.gameObject, TwoShot, Base, 13);

        AddSound(timeline);
        AddRecorder(timeline, "Cutscene_AfterStage1");

        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("🎬 Built " + ScenePath);
    }

    [MenuItem("Contra/Cutscenes/AfterStage1/Render Stills")]
    public static void RenderKeyStills()
    {
        RenderAt(0.7f, 2.3f, 2.66f, 3.3f, 4.0f, 5.2f, 6.3f, 7.5f, 9.4f,
                 10.6f, 12.8f, 14.6f, 15.3f, 17.0f, 18.9f, 19.6f, 21.0f, 23.2f);
    }

    public static void RenderAt(params float[] times)
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);

        RenderStills(Root("Shoot").GetComponent<PlayableDirector>(), times, StillsFolder);
        Debug.Log("🖼️ Stills in " + StillsFolder);
    }

    // =========================
    // SET A: THE CLEARING, BLUE HOUR
    // =========================

    static Transform BuildClearing(Transform sets, Transform sparks, Transform smoke,
        Transform fireflies, Transform birds)
    {
        Transform a = AddGroup("A_Clearing", sets);

        // The sky rides with the camera; placed so the moon is top left
        AddParallax("Sky", a, ArtSprite("sky_twilight"), new Vector2(1f, -3.7f), 1f, AnchorA, -100);
        AddParallax("Mist", a, ArtSprite("tw_mist"), new Vector2(0f, -5.1f), 0.85f, AnchorA, -90);
        AddParallax("Hills", a, ArtSprite("tw_hills"), new Vector2(0f, -2.2f), 0.7f, AnchorA, -80);
        AddParallax("Pines Far", a, ArtSprite("tw_pines_far"), new Vector2(0f, -1.45f), 0.5f, AnchorA, -70);
        AddParallax("Pines Near", a, ArtSprite("tw_pines_near"), new Vector2(0f, -1.4f), 0.3f, AnchorA, -60, "Default");

        // Far off over the hills: the compound's searchlights. They
        // light only the background, so their beams sit behind the
        // near trees.
        AddLight("Far Beam 1", a, new Vector2(4.6f, -1f), Searchlight, 0f, 18f, 10f, 30f, 0.6f, 0.5f, "Background");
        AddLight("Far Beam 2", a, new Vector2(6.9f, -1.2f), Searchlight, 0f, 18f, 10f, -20f, 0.6f, 0.5f, "Background");

        // What happened here: a clawed ranger tent, a cage bent open
        AddSprite("Tent", a, ArtSprite("tent_torn"), new Vector2(-7.1f, GroundA + 15f / 16f - 0.06f), -20);
        AddSprite("Cage", a, ArtSprite("cage_broken"), new Vector2(5.8f, GroundA + 17f / 16f - 0.06f), -20);

        AddSprite("Tree Left", a, ArtSprite("tw_tree_1"), new Vector2(-9.6f, GroundA + 3.3f), -10);
        // In front of the lion, which runs off behind them
        AddSprite("Tree Right", a, ArtSprite("tw_tree_2"), new Vector2(8.4f, GroundA + 3.3f), 15);
        AddSprite("Tree Far Right", a, ArtSprite("tw_tree_1"), new Vector2(10.8f, GroundA + 3.2f), 16,
               flipX: true);

        AddSprite("Ground", a, ArtSprite("clearing_ground"), new Vector2(0f, GroundA - 1.5f), 0);
        AddParallax("Foreground Grass", a, ArtSprite("fg_grass"), new Vector2(0f, GroundA - 1.2f),
                 -0.25f, AnchorA, 10, "Foreground");

        // Rook, and the last lion of the pack, down, its collar sparking
        AddCharacter("Rook", a, RookIdle[0], new Vector2(-4.2f, GroundA + 19f / 16f), 10, Twilight);

        SpriteRenderer lion = AddCharacter("Lion", a, LionFrame(4), LionAt, 10, Twilight);

        // The collar rides on the lion (undoing its 6.25x scale) so
        // it twitches with it
        SpriteRenderer collar = AddSprite("Collar", lion.transform, ArtSprite("collar"),
            NeckOffset / CharacterScale, 12, scale: 1f / CharacterScale);
        AddSprite("LED", collar.transform, ArtSprite("collar_led"), new Vector2(-0.5f / 16f, 0f), 13,
               unlit: true);
        AddLight("Glow", collar.transform, Vector2.zero, AlarmRed, 0.4f, 1.6f);

        Vector2 neck = LionAt + NeckOffset;
        AddLight("Break Flash", a, neck, new Color(0.75f, 0.9f, 1f), 0f, 5f, falloff: 0.7f);
        AddSprite("Half A", a, ArtSprite("collar_half_a"), neck, 14, tint: Twilight).gameObject.SetActive(false);
        AddSprite("Half B", a, ArtSprite("collar_half_b"), neck, 14, tint: Twilight).gameObject.SetActive(false);

        PlaceFX(sparks, a, neck);
        PlaceFX(smoke, a, neck + new Vector2(0f, -0.3f));
        PlaceFX(fireflies, a, new Vector2(-2f, -1f));
        PlaceFX(birds, a, new Vector2(8.6f, 2.2f));

        BuildShards(sparks, a, neck);

        return a;
    }

    static void PlaceFX(Transform fx, Transform set, Vector2 position)
    {
        fx.SetParent(set, false);
        fx.localPosition = position;
        fx.gameObject.SetActive(false);
    }

    // The collar's shards: the intro's sparks, as one burst that falls
    static void BuildShards(Transform sparks, Transform set, Vector2 position)
    {
        GameObject shards = Object.Instantiate(sparks.gameObject, set);
        shards.name = "Shards";
        shards.transform.localPosition = position;

        ParticleSystem ps = shards.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.duration = 1f;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.0625f, 0.125f);
        main.gravityModifier = 1.2f;
        main.maxParticles = 64;
        main.startColor = new ParticleSystem.MinMaxGradient(
            Color.white, new Color(0.5f, 0.95f, 1f));

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.rotation = Vector3.zero;

        shards.SetActive(false);
    }

    // =========================
    // SET B: THE COMPOUND FROM THE FOREST EDGE
    // =========================

    static void BuildFar(Transform sets)
    {
        Transform b = AddGroup("B_Far", sets);

        AddParallax("Sky", b, ArtSprite("sky_night"), new Vector2(98.25f, -1.55f), 1f, AnchorB, -100);
        AddParallax("Mountains", b, ArtSprite("night_far"), new Vector2(100f, -6.2f), 0.85f, AnchorB, -90);

        // The compound far off, with its searchlights on the towers
        Transform compound = AddParallax("Compound", b, ArtSprite("compound_far"),
            new Vector2(100f, -1.4f + 84f / 32f), 0.55f, AnchorB, -70);
        Transform layer = compound.GetChild(0);
        AddLight("Beam L", layer, new Vector2(-5.875f, 0.94f), Searchlight, 1f, 22f, 12f, 30f, 0.8f, 0.5f,
              "Background");
        AddLight("Beam R", layer, new Vector2(5.875f, 0.94f), Searchlight, 1f, 22f, 12f, -25f, 0.8f, 0.5f,
              "Background");

        AddParallax("Field", b, ArtSprite("field_far"), new Vector2(100f, -2.65f), 0.3f, AnchorB, -50, "Default");

        // The forest edge where Rook stands watching
        AddParallax("Tree L1", b, ArtSprite("night_tree_1"), new Vector2(92.2f, 0.4f), -0.3f, AnchorB, 0, "Foreground");
        AddParallax("Tree L2", b, ArtSprite("night_tree_2"), new Vector2(93.6f, 0f), -0.3f, AnchorB, 1, "Foreground");
        AddParallax("Tree R", b, ArtSprite("night_tree_1"), new Vector2(108f, 0.3f), -0.3f, AnchorB, 0, "Foreground",
                 flipX: true);
        AddParallax("Brush", b, ArtSprite("fg_brush"), new Vector2(100f, -2.9f), -0.3f, AnchorB, 5, "Foreground");

        Transform rook = AddParallax("Rook", b, null, new Vector2(97.6f, -2f + 19f / 16f), -0.3f, AnchorB, 4,
                                  "Foreground");
        AddCharacter("Rook", rook.GetChild(0), RookIdle[0], Vector2.zero, 4, Silhouette, layer: "Foreground");
    }

    // =========================
    // SET C: THE COMPOUND, CLOSE
    // =========================

    // Searchlight lamps on the towers' roofs
    static readonly Vector2 LampL = new Vector2(189.34f, 6.97f);
    static readonly Vector2 LampR = new Vector2(210.66f, 6.97f);

    static void BuildCompound(Transform sets)
    {
        Transform c = AddGroup("C_Compound", sets);
        float g = GroundC;

        AddParallax("Sky", c, ArtSprite("sky_night"), new Vector2(199f, -0.5f), 1f, AnchorC, -100);
        AddParallax("Mountains", c, ArtSprite("night_far"), new Vector2(200f, -2.3f), 0.85f, AnchorC, -90);
        AddParallax("Hills", c, ArtSprite("night_hills"), new Vector2(200f, -0.7f), 0.7f, AnchorC, -80);

        AddSprite("Wing L", c, ArtSprite("wing"), new Vector2(192.5f, g + 2.25f), -30);
        AddSprite("Wing R", c, ArtSprite("wing"), new Vector2(207.5f, g + 2.25f), -30, flipX: true);
        AddSprite("Keep", c, ArtSprite("keep"), new Vector2(200f, g + 5.5f), -25);
        AddSprite("Banner", c, LoadSprite("Assets/Dialogue/Props/Banner.png"), new Vector2(200f, 2.5f), -24,
               tint: new Color(0.55f, 0.5f, 0.55f));
        AddSprite("PA", c, ArtSprite("pa_horns"), new Vector2(200f, 6.6f), -23);
        AddSprite("PA L", c, ArtSprite("pa_horns"), new Vector2(195.2f, 8.2f), -23);
        AddSprite("PA R", c, ArtSprite("pa_horns"), new Vector2(204.8f, 8.2f), -23, flipX: true);

        AddSprite("Tower L", c, ArtSprite("tower"), new Vector2(189f, g + 5f), -20);
        AddSprite("Tower R", c, ArtSprite("tower"), new Vector2(211f, g + 5f), -20, flipX: true);
        AddSprite("Crates", c, ArtSprite("crates"), new Vector2(205.8f, g + 15f / 16f), -15);

        AddSprite("Gate Glow", c, ArtSprite("gate_glow"), new Vector2(200f, g + 1.75f), 17, unlit: true);
        AddSprite("Gate L", c, ArtSprite("gate_panel"), new Vector2(198.75f, g + 1.75f), 18);
        AddSprite("Gate R", c, ArtSprite("gate_panel"), new Vector2(201.25f, g + 1.75f), 18, flipX: true);
        AddSprite("Fence L", c, ArtSprite("fence"), new Vector2(189.5f, g + 1.875f), 20);
        AddSprite("Fence R", c, ArtSprite("fence"), new Vector2(210.5f, g + 1.875f), 20);
        AddSprite("Sign", c, ArtSprite("gate_sign"), new Vector2(194.6f, g + 2.3f), 21);
        AddSprite("Pillar L", c, ArtSprite("gate_pillar"), new Vector2(197.125f, g + 2.375f), 24);
        AddSprite("Pillar R", c, ArtSprite("gate_pillar"), new Vector2(202.875f, g + 2.375f), 24);
        AddSprite("Beacon L", c, ArtSprite("beacon_off"), new Vector2(197.125f, g + 4.91f), 25, unlit: true);
        AddSprite("Beacon R", c, ArtSprite("beacon_off"), new Vector2(202.875f, g + 4.91f), 25, unlit: true);
        AddSprite("Flood L", c, ArtSprite("flood_pole"), new Vector2(191.6f, g + 2.25f), 22);
        AddSprite("Flood R", c, ArtSprite("flood_pole"), new Vector2(207f, g + 2.25f), 22);
        AddSprite("Sandbags", c, ArtSprite("sandbags"), new Vector2(193.4f, g + 7f / 16f), 26);
        AddSprite("Ground", c, ArtSprite("base_ground"), new Vector2(200f, g - 1.5f), 0);

        string[] lit = { "Background", "Default" };

        AddLight("Searchlight L", c, LampL, Searchlight, 0.9f, 20f, 9f, -70f, 0.5f, 0.5f, lit);
        AddLight("Searchlight R", c, LampR, Searchlight, 0.9f, 20f, 9f, 60f, 0.5f, 0.5f, lit);

        AddLight("Uplight L", c, new Vector2(198.2f, -1f), Sodium, 0.9f, 9f, 28f, -10f, 0.12f, 0.5f, lit);
        AddLight("Uplight R", c, new Vector2(201.8f, -1f), Sodium, 0.9f, 9f, 28f, 10f, 0.12f, 0.5f, lit);
        AddLight("Flood Lamp L", c, new Vector2(191.6f, g + 4.2f), Sodium, 1.2f, 5.5f, 60f, 180f, 0.2f, 0.5f, lit);
        AddLight("Flood Lamp R", c, new Vector2(207f, g + 4.2f), Sodium, 1.2f, 5.5f, 60f, 180f, 0.2f, 0.5f, lit);

        AddLight("Alarm L", c, new Vector2(197.125f, g + 4.95f), AlarmRed, 0f, 3.2f, volume: 0.25f);
        AddLight("Alarm R", c, new Vector2(202.875f, g + 4.95f), AlarmRed, 0f, 3.2f, volume: 0.25f);
        AddLight("Alarm Wash", c, new Vector2(200f, 3f), AlarmRed, 0f, 8f, falloff: 0.3f);

        // The soldiers, waiting behind the gate
        for (int i = 0; i < SoldierCount; i++)
        {
            SpriteRenderer soldier = AddCharacter("Soldier " + (i + 1), c, SoldierRun[i % 6],
                new Vector2(SoldierStartX, g + 19f / 16f), 30, SoldierTint, flipX: true);
            soldier.gameObject.SetActive(false);
        }
    }

    const int SoldierCount = 6;
    const float SoldierStartX = 200.5f;
    const float SoldierSpeed = 2.4f;
    const float SoldierGap = 0.45f;

    // =========================
    // CAMERAS
    // =========================

    static Dictionary<string, CinemachineCamera> BuildCameras(Transform cameras, Transform template)
    {
        var cams = new Dictionary<string, CinemachineCamera>();

        foreach (string name in new[] { "CM_Collar", "CM_TwoShot", "CM_Far", "CM_Banner", "CM_Gate" })
        {
            GameObject cam = Object.Instantiate(template.gameObject, cameras);
            cam.name = name;
            cams[name] = cam.GetComponent<CinemachineCamera>();
        }

        return cams;
    }

    static Clip CameraClip()
    {
        Clip clip = new Clip("Cameras");
        string perlin = "AmplitudeGain";
        string ortho = "Lens.OrthographicSize";

        // A1: close on the collar, a slow push, a punch on the break
        clip.Move("CM_Collar",
            new Curve(0f, 1.15f).To(Shatter, 1.25f, Ease.Hold),
            new Curve(0f, -1.05f, Ease.Hold), -10f);
        clip.Float("CM_Collar", typeof(CinemachineCamera), ortho,
            new Curve(0f, 2.35f).To(Shatter - 0.01f, 2.12f, Ease.Linear)
                .To(Shatter + 0.05f, 2f).To(Shatter + 0.8f, 2.22f).To(TwoShot, 2.24f));
        clip.Float("CM_Collar", typeof(CinemachineBasicMultiChannelPerlin), perlin,
            new Curve(0f, 0.25f).To(Shatter - 0.01f, 0.4f, Ease.Linear)
                .To(Shatter + 0.02f, 4f, Ease.Smooth).To(Shatter + 0.5f, 0.3f));

        // A2: Rook and the lion; drifts right, tilts up to the sky
        clip.Move("CM_TwoShot",
            new Curve(TwoShot, -2f, Ease.Linear).To(8f, -0.9f).To(Base, 0.6f),
            new Curve(TwoShot, -0.3f, Ease.Hold).To(8f, -0.3f).To(Base, 0.5f), -10f);
        clip.Float("CM_TwoShot", typeof(CinemachineCamera), ortho,
            new Curve(TwoShot, 3.5f, Ease.Linear).To(8f, 3.6f).To(Base, 3.95f));
        clip.Float("CM_TwoShot", typeof(CinemachineBasicMultiChannelPerlin), perlin,
            Curve.Constant(0.2f));

        // B1: the compound from the forest edge, a slow push
        clip.Move("CM_Far",
            new Curve(Base, 99.5f, Ease.Linear).To(Banner, 100.4f),
            new Curve(Base, 0.8f, Ease.Linear).To(Banner, 0.95f), -10f);
        clip.Float("CM_Far", typeof(CinemachineCamera), ortho,
            new Curve(Base, 4.4f).To(Banner, 4.05f));
        clip.Float("CM_Far", typeof(CinemachineBasicMultiChannelPerlin), perlin,
            Curve.Constant(0.12f));

        // B2: the banner, pushing in on his face; the alarm rattles it
        clip.Move("CM_Banner",
            new Curve(Banner, 200f, Ease.Hold),
            new Curve(Banner, 4f).To(Alarm, 3.2f).To(Gate, 3.15f), -10f);
        clip.Float("CM_Banner", typeof(CinemachineCamera), ortho,
            new Curve(Banner, 3.6f).To(Alarm, 2.35f).To(Gate, 2.3f));
        clip.Float("CM_Banner", typeof(CinemachineBasicMultiChannelPerlin), perlin,
            new Curve(Banner, 0.1f, Ease.Hold).To(Alarm, 0.1f, Ease.Smooth)
                .To(Alarm + 0.2f, 0.7f).To(Gate, 0.5f));

        // B3: the gate; drifts left with the soldiers
        clip.Move("CM_Gate",
            new Curve(Gate, 198f, Ease.Linear).To(Cut, 196.8f),
            new Curve(Gate, 0.45f, Ease.Linear).To(Cut, 0.35f), -10f);
        clip.Float("CM_Gate", typeof(CinemachineCamera), ortho,
            new Curve(Gate, 3.4f, Ease.Linear).To(Cut, 3.55f));
        clip.Float("CM_Gate", typeof(CinemachineBasicMultiChannelPerlin), perlin,
            Curve.Constant(0.35f));

        return clip;
    }

    // =========================
    // PROPS
    // =========================

    static Clip PropsClip()
    {
        Clip clip = new Clip("Props");

        clip.Active("A_Clearing", (0f, true), (Base, false));
        clip.Active("B_Far", (0f, false), (Base, true), (Banner, false));
        clip.Active("C_Compound", (0f, false), (Banner, true));

        AnimateClearing(clip);
        AnimateFar(clip);
        AnimateCompound(clip);

        return clip;
    }

    static void AnimateClearing(Clip clip)
    {
        const string lion = "A_Clearing/Lion";
        float x = LionAt.x;
        float px = 1f / 16f;

        // The collar beeps faster and faster before it breaks
        float[] beeps = { 0.5f, 1.15f, 1.65f, 2f, 2.22f, 2.37f, 2.47f, 2.54f };

        // Down and twitching; the break; the head shake; up, away,
        // a look back at Rook; then gone into the trees
        clip.Sprites(lion, new SpriteKeys()
            .Set(0f, LionFrame(4))
            .Set(Rise, LionFrame(1))
            .Cycle(Walk, Stop, 0.22f, LionFrame(1), LionFrame(2))
            .Set(Stop, LionFrame(1))
            .Cycle(Run, 8.6f, 0.1f, LionFrame(1), LionFrame(2)));

        clip.Flags(lion, typeof(SpriteRenderer), "m_FlipX",
            (0f, false), (Shake, true), (Shake + 0.12f, false), (Shake + 0.24f, true),
            (Shake + 0.36f, false), (Walk, true), (Look, false), (Run, true));

        Curve lionX = new Curve(0f, x, Ease.Hold);
        foreach (float t in new[] { 0.9f, 1.5f, 2.05f, 2.3f, 2.42f, 2.5f })
            lionX.To(t, x + (t > 1.4f && t < 1.6f ? -px : px), Ease.Hold).To(t + 0.06f, x, Ease.Hold);
        lionX.To(Shatter, x + px, Ease.Hold).To(Shatter + 0.12f, x, Ease.Hold)
            .To(Walk, x, Ease.Linear).To(Stop, x + 1.6f, Ease.Hold)
            .To(Run, x + 1.6f, Ease.Smooth).To(7.6f, x + 3.4f, Ease.Linear).To(8.6f, x + 13f, Ease.Hold);
        clip.Move(lion, lionX, Curve.Constant(LionAt.y));
        clip.Active(lion, (0f, true), (8.6f, false));

        clip.Active(lion + "/Collar", (0f, true), (Shatter, false));

        var led = new List<(float, bool)> { (0f, false) };
        Curve glow = new Curve(0f, 0.3f, Ease.Hold);
        foreach (float t in beeps)
        {
            led.Add((t, true));
            led.Add((t + 0.06f, false));
            glow.To(t, 2.2f, Ease.Hold).To(t + 0.06f, 0.3f + t * 0.25f, Ease.Hold);
        }
        clip.Active(lion + "/Collar/LED", led.ToArray());
        clip.Float(lion + "/Collar/Glow", typeof(Light2D), "m_Intensity", glow);

        clip.Float("A_Clearing/Break Flash", typeof(Light2D), "m_Intensity",
            new Curve(0f, 0f, Ease.Hold).To(Shatter, 5f, Ease.Smooth).To(Shatter + 0.45f, 0f, Ease.Hold));

        Vector2 neck = LionAt + NeckOffset;
        FlyApart(clip, "A_Clearing/Half A", neck, new Vector2(-2.2f, 4.8f), 720f);
        FlyApart(clip, "A_Clearing/Half B", neck, new Vector2(2.8f, 5.4f), -680f);

        clip.Sprites("A_Clearing/Rook", new SpriteKeys().Cycle(0f, Base, 0.15f, RookIdle));

        // Far off: the compound's searchlights come on over the hills
        foreach ((string beam, float from, float to) in new[] {
            ("A_Clearing/Far Beam 1", 30f, 6f), ("A_Clearing/Far Beam 2", -20f, 4f) })
        {
            clip.Float(beam, typeof(Light2D), "m_Intensity",
                new Curve(0f, 0f, Ease.Hold).To(Roar - 0.3f, 0f).To(Roar + 0.9f, 0.7f));
            clip.Float(beam, typeof(Light2D), "m_LightVolumeIntensity",
                new Curve(0f, 0f, Ease.Hold).To(Roar - 0.3f, 0f).To(Roar + 0.9f, 0.6f));
            clip.Rotate(beam, new Curve(Roar - 0.3f, from).To(Base, to));
        }
    }

    // A piece of the collar flung off, tumbling until it lands
    static void FlyApart(Clip clip, string path, Vector2 from, Vector2 velocity, float spin)
    {
        const float gravity = 14f;
        float floor = GroundA + 0.12f;

        Curve x = new Curve(0f, from.x, Ease.Hold).To(Shatter, from.x, Ease.Linear);
        Curve y = new Curve(0f, from.y, Ease.Hold).To(Shatter, from.y, Ease.Linear);
        Curve angle = new Curve(0f, 0f, Ease.Hold).To(Shatter, 0f, Ease.Linear);

        for (float t = 1f / 30f; ; t += 1f / 30f)
        {
            float height = from.y + velocity.y * t - 0.5f * gravity * t * t;
            bool landed = height <= floor && velocity.y - gravity * t < 0f;

            x.To(Shatter + t, from.x + velocity.x * t, landed ? Ease.Hold : Ease.Linear);
            y.To(Shatter + t, landed ? floor : height, landed ? Ease.Hold : Ease.Linear);
            angle.To(Shatter + t, spin * t, landed ? Ease.Hold : Ease.Linear);

            if (landed)
                break;
        }

        clip.Move(path, x, y);
        clip.Rotate(path, angle);
        clip.Active(path, (0f, false), (Shatter, true));
    }

    static void AnimateFar(Clip clip)
    {
        const string beams = "B_Far/Compound/Layer/";

        clip.Rotate(beams + "Beam L", new Curve(Base, 30f).To(11.8f, -12f).To(Banner, 22f));
        clip.Rotate(beams + "Beam R", new Curve(Base, -25f).To(12.4f, 16f).To(Banner, -10f));

        clip.Sprites("B_Far/Rook/Layer/Rook", new SpriteKeys().Cycle(Base, Banner, 0.15f, RookIdle));
    }

    static void AnimateCompound(Clip clip)
    {
        const string c = "C_Compound/";

        // Left searchlight sweeps across the banner as he speaks, then
        // both swing round to the forest when the soldiers go out
        clip.Rotate(c + "Searchlight L",
            new Curve(Banner, -70f).To(15.8f, -118f).To(17.6f, -82f).To(Gate, -105f)
                .To(21f, 118f).To(Cut, 104f));
        clip.Rotate(c + "Searchlight R",
            new Curve(Banner, 60f).To(16.5f, 110f).To(Gate, 70f).To(21.5f, 82f).To(Cut, 96f));

        // Alarm: beacons flash, red light pulses over everything
        SpriteKeys beacon = new SpriteKeys().Set(0f, ArtSprite("beacon_off"))
            .Cycle(Alarm, End, 0.3f, ArtSprite("beacon"), ArtSprite("beacon_off"));
        clip.Sprites(c + "Beacon L", beacon);
        clip.Sprites(c + "Beacon R", beacon);

        Curve pulse = new Curve(0f, 0f, Ease.Hold);
        Curve wash = new Curve(0f, 0f, Ease.Hold);
        for (float t = Alarm; t < End; t += 0.6f)
        {
            pulse.To(t, 2.4f).To(t + 0.3f, 0.2f);
            wash.To(t, 1.5f).To(t + 0.3f, 0.25f);
        }
        clip.Float(c + "Alarm L", typeof(Light2D), "m_Intensity", pulse);
        clip.Float(c + "Alarm R", typeof(Light2D), "m_Intensity", pulse);
        clip.Float(c + "Alarm Wash", typeof(Light2D), "m_Intensity", wash);

        // The gate rolls open behind the fence
        float y = GroundC + 1.75f;
        clip.Move(c + "Gate L", new Curve(0f, 198.75f, Ease.Hold).To(GateOpen, 198.75f).To(GateOpen + 1.2f, 196.4f),
                  Curve.Constant(y));
        clip.Move(c + "Gate R", new Curve(0f, 201.25f, Ease.Hold).To(GateOpen, 201.25f).To(GateOpen + 1.2f, 203.6f),
                  Curve.Constant(y));

        // ...and the soldiers march out of the light, towards the forest
        Sprite[] run = SoldierRun;
        for (int i = 0; i < SoldierCount; i++)
        {
            string path = c + "Soldier " + (i + 1);
            float enter = March + SoldierGap * i;
            float far = SoldierStartX - SoldierSpeed * (End - enter);

            clip.Active(path, (0f, false), (enter, true));
            clip.Move(path, new Curve(enter, SoldierStartX, Ease.Linear).To(End, far),
                      Curve.Constant(GroundC + 19f / 16f));
            clip.Float(path, typeof(SpriteRenderer), "m_Color.a",
                new Curve(enter, 0f, Ease.Linear).To(enter + 0.3f, 1f));
            clip.Sprites(path, new SpriteKeys().Cycle(enter, End, 0.12f,
                run.Skip(i % 6).Concat(run.Take(i % 6)).ToArray()));
        }
    }

    // =========================
    // OVERLAY AND LIGHTING
    // =========================

    static void SetUpOverlay()
    {
        Transform overlay = Root("Overlay");

        Child(overlay, "Caption/Place").GetComponent<TMP_Text>().text = "ROYAL MANEFORD COMPOUND";
        Child(overlay, "Caption/Time").GetComponent<TMP_Text>().text = "DAY 9  -  21:40";
        Child(overlay, "Caption").GetComponent<CanvasGroup>().alpha = 0f;
        Child(overlay, "Title").GetComponent<CanvasGroup>().alpha = 0f;
    }

    static Clip OverlayClip()
    {
        Clip clip = new Clip("Overlay");

        // In from black; a hard cut back to black on the last hit
        clip.Float("Fade", typeof(Image), "m_Color.a",
            new Curve(0f, 1f).To(0.6f, 0f, Ease.Hold).To(Cut, 1f, Ease.Hold));

        clip.Float("Flash", typeof(Image), "m_Color.a",
            new Curve(0f, 0f, Ease.Hold).To(Shatter, 0.7f, Ease.Linear).To(Shatter + 0.3f, 0f, Ease.Hold));

        clip.Float("Caption", typeof(CanvasGroup), "m_Alpha",
            new Curve(0f, 0f, Ease.Hold).To(Caption - 0.05f, 1f, Ease.Hold).To(13.7f, 1f).To(14.1f, 0f, Ease.Hold));
        clip.Float("Caption/Place", typeof(TextReveal), "progress",
            new Curve(0f, 0f, Ease.Hold).To(Caption, 0f, Ease.Linear).To(Caption + 1f, 1f, Ease.Hold));
        clip.Float("Caption/Time", typeof(TextReveal), "progress",
            new Curve(0f, 0f, Ease.Hold).To(Caption + 1.1f, 0f, Ease.Linear).To(Caption + 1.9f, 1f, Ease.Hold));

        clip.Float("Title", typeof(CanvasGroup), "m_Alpha", Curve.Constant(0f));

        return clip;
    }

    static Clip LightingClip()
    {
        Clip clip = new Clip("Lighting");

        clip.Float("Global Light 2D", typeof(Light2D), "m_Intensity", Curve.Constant(1f));

        // The impact look (bloom, chromatic aberration) on the break,
        // and a smaller jolt when the alarm goes
        clip.Float("SlamFX", typeof(Volume), "weight",
            new Curve(0f, 0f, Ease.Hold).To(Shatter, 1f).To(Shatter + 0.6f, 0f, Ease.Hold)
                .To(Alarm, 0.35f).To(Alarm + 0.5f, 0f, Ease.Hold));

        return clip;
    }

    // =========================
    // SOUND
    // =========================

    static void AddSound(TimelineAsset timeline)
    {
        AddAudio(timeline, "Music", (0, Sound("mus_afterstage1"), 0.8f));
        AddAudio(timeline, "Voice",
            (Gen01, Sound("vo_gen_01"), 0.85f),
            (Gen02, Sound("vo_gen_02"), 0.85f),
            (Gen03, Sound("vo_gen_03"), 0.85f));
        AddAudio(timeline, "Ambience",
            (0, Sound("amb_twilight"), 0.55f),
            (Base, Sound("amb_base"), 0.6f));
        AddAudio(timeline, "SFX 1",
            (0, Sound("sfx_collar_buzz"), 0.8f),
            (Shatter, Sound("sfx_shatter"), 0.85f),
            (Shake, Sound("sfx_shake"), 0.7f),
            (Rise, Sound("sfx_rise"), 0.6f));
        AddAudio(timeline, "SFX 2",
            (Shatter + 0.06f, Sound("sfx_birds"), 0.5f),
            (Walk, Sound("sfx_lion_walk"), 0.6f),
            (Look + 0.1f, Sound("sfx_chuff"), 0.8f),
            (Run, Sound("sfx_lion_run"), 0.8f),
            (Roar, Sound("sfx_roar_echo"), 0.6f));
        AddAudio(timeline, "SFX 3",
            (Base + 0.1f, Sound("sfx_searchlight"), 0.4f),
            (Caption, Sound("sfx_type_1"), 0.6f),
            (Caption + 1.1f, Sound("sfx_type_2"), 0.6f),
            (PaOn, Sound("sfx_pa_on"), 0.8f));
        AddAudio(timeline, "SFX 4",
            (Alarm, Sound("sfx_alarm"), 0.7f),
            (GateOpen, Sound("sfx_gate"), 0.8f),
            (March, Sound("sfx_march"), 0.8f));
    }
}
