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

// Builds the shoot scene for Cutscene_AfterStage2, the cutscene
// between Stage 2 and Stage 3 (STORY_AND_GAME_STRUCTURE.md, 7):
// the General takes tea in his control room while the monitors show
// Rook downing his twelfth soldier. He spits his tea, orders "Wake
// the Mecha-Lion.", slams the big red button (DO NOT PRESS (ROYAL)),
// waves off Dobbs and the fuel gauge, and in the hangar the
// Mecha-Lion's eyes light up.
//
// Contra > Cutscenes > AfterStage2 > Build Shoot Scene rebuilds
// AfterStage2/AfterStage2_Shoot.unity and its Timeline from the art
// and sound made by Tools/cutscene_afterstage2_*. Then press Play in
// that scene to record the master, and run
//     python Tools/cutscene_encode.py Cutscene_AfterStage2 24.9
public static class AfterStage2Builder
{
    const string Folder = "Assets/Cutscenes/AfterStage2/";
    const string ScenePath = Folder + "AfterStage2_Shoot.unity";
    const string TimelinePath = Folder + "AfterStage2.playable";
    const string StillsFolder = "Temp/Captures/AfterStage2";

    // Shot timing in seconds; Tools/cutscene_afterstage2_audio.py
    // writes the score and effects to the same times
    const float Caption = 0.9f;     // the place caption types on
    const float Sip = 1.5f;         // the cup goes up
    const float Monitor = 5.0f;     // cut to the big screen
    const float Fire = 5.4f;        // on screen: Rook fires
    const float Hit = 5.65f;        //   the twelfth soldier is hit
    const float Down = 6.0f;        //   and goes down
    const float UnitsDown = 6.4f;   // ALL UNITS DOWN
    const float Tea = 7.0f;         // cut to the General, mid-sip
    const float Spit = 7.5f;        // the spit-take
    const float Order = 9.0f;       // cut: he points
    const float Gen01 = 9.4f;       // "Wake the Mecha-Lion."
    const float Button = 11.4f;     // cut to the button
    const float Cover = 11.7f;      // the glass flips up
    const float Slam = 12.5f;       // his fist comes down
    const float Alarm = 13.3f;      // cut to the room, red light
    const float Dobbs01 = 13.8f;    // "Sir, about the fuel gauge..."
    const float NotNow = 15.9f;     // cut to the General
    const float Gen02 = 16.0f;      // "Not now, Dobbs!"
    const float Hangar = 18.0f;     // cut to the hangar at night
    const float Door = 18.4f;       // the door rolls up
    const float Eyes = 21.3f;       // two red eyes in the dark
    const float Glow = 22.0f;       // the head lights up
    const float Roar = 22.6f;       // the roar
    const float Cut = 24.1f;        // hard cut to black
    const float End = 24.9f;

    const float Floor = -2.5f;
    static readonly Vector2 GeneralAt = new Vector2(-3.8f, Floor + 25f / 16f);
    static readonly Vector2 DobbsAt = new Vector2(-6.6f, Floor + 18f / 16f);
    static readonly Vector2 CupAt = GeneralAt + new Vector2(9f / 16f, 10f / 16f);

    // Screens on the wall art (top-left pixel), as in the art script
    static readonly Vector2Int[] SmallScreens = {
        new Vector2Int(88, 10), new Vector2Int(124, 10), new Vector2Int(88, 34),
        new Vector2Int(124, 34), new Vector2Int(232, 10), new Vector2Int(232, 34) };
    static readonly Vector2 BigScreen = WallPixel(194f, 30f);

    // The button close-up and the hangar sit far off from the room
    const float ButtonX = 300f;
    const float HangarX = 100f;
    static readonly Vector2 MechaAt = new Vector2(HangarX + 0.8f, Floor);
    const float MechaScale = 0.85f;
    static readonly Vector2 HeadOffset = new Vector2(-3.1f, 4.75f);
    static readonly Vector2 EyesAt = MechaAt + (HeadOffset + new Vector2(-0.31f, 0.19f)) * MechaScale;

    static readonly Color ScreenGreen = new Color(0.55f, 1f, 0.7f);
    static readonly Color AlarmRed = new Color(1f, 0.12f, 0.08f);
    static readonly Color Sodium = new Color(1f, 0.78f, 0.5f);
    static readonly Color Shadow = new Color(0.12f, 0.12f, 0.16f);

    static Vector2 WallPixel(float px, float py)
    {
        return new Vector2((px - 144f) / 16f, 4.5f - py / 16f);
    }

    static Sprite ArtSprite(string name)
    {
        return LoadSprite(Folder + "Art/" + name + ".png");
    }

    static Sprite[] Arts(params string[] names)
    {
        return names.Select(ArtSprite).ToArray();
    }

    static AudioClip Sound(string name)
    {
        return LoadAudio(Folder + "Audio/" + name + ".wav");
    }

    // =========================
    // BUILD
    // =========================

    [MenuItem("Contra/Cutscenes/AfterStage2/Build Shoot Scene")]
    public static void Build()
    {
        if (!SafeToBuild(ScenePath))
            return;

        StartFromIntro(ScenePath);

        Transform sets = Root("Sets");
        Transform cameras = Root("Cameras");

        // Keep the intro's sparks (for the tea) and smoke (for the
        // hangar door's dust) and one camera as a template
        Transform sparks = Child(sets, "B_Gate/Sparks");
        Transform smoke = Child(sets, "B_Gate/Smoke");
        foreach (Transform fx in new[] { sparks, smoke })
            fx.SetParent(null, true);

        ClearChildren(sets);

        Transform template = Child(cameras, "CM_Aerial");
        template.SetParent(null, true);
        ClearChildren(cameras);

        BuildRoom(sets, sparks);
        BuildButton(sets);
        BuildHangar(sets, smoke);

        Dictionary<string, CinemachineCamera> cams = BuildCameras(cameras, template);
        Object.DestroyImmediate(template.gameObject);

        SetUpOverlay();

        TimelineAsset timeline = NewTimeline(TimelinePath, End);
        PlayableDirector director = ResetDirector(timeline);

        AddShots(timeline, director, Root("Main Camera").GetComponent<CinemachineBrain>(),
            ("Room", 0, Monitor, cams["CM_Wide"]),
            ("Monitor", Monitor, Tea, cams["CM_Monitor"]),
            ("Tea", Tea, Order, cams["CM_Tea"]),
            ("Order", Order, Button, cams["CM_Order"]),
            ("Button", Button, Alarm, cams["CM_Button"]),
            ("Alarm", Alarm, NotNow, cams["CM_Alarm"]),
            ("Not Now", NotNow, Hangar, cams["CM_NotNow"]),
            ("Hangar", Hangar, End, cams["CM_Hangar"]));

        AddAnimationTrack(timeline, director, "Camera Moves",
            cameras.GetComponent<Animator>(), CameraClip().Save(Folder + "Animation/Cameras.anim"));
        AddAnimationTrack(timeline, director, "Props",
            sets.GetComponent<Animator>(), PropsClip().Save(Folder + "Animation/Props.anim"));
        AddAnimationTrack(timeline, director, "Overlay",
            Root("Overlay").GetComponent<Animator>(), OverlayClip().Save(Folder + "Animation/Overlay.anim"));
        AddAnimationTrack(timeline, director, "Lighting",
            Root("Lighting").GetComponent<Animator>(), LightingClip().Save(Folder + "Animation/Lighting.anim"));

        AddControl(timeline, director, "FX Tea", Child(sets, "A_Room/Tea").gameObject, Spit, Order, 5);
        AddControl(timeline, director, "FX Dust", smoke.gameObject, Door, Eyes, 7);

        AddSound(timeline);
        AddRecorder(timeline, "Cutscene_AfterStage2");

        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("🎬 Built " + ScenePath);
    }

    [MenuItem("Contra/Cutscenes/AfterStage2/Render Stills")]
    public static void RenderKeyStills()
    {
        RenderAt(0.7f, 2.1f, 4.5f, 5.3f, 5.7f, 6.6f, 7.2f, 7.7f, 9.9f, 11.6f, 12.2f, 12.6f,
                 14.5f, 17.0f, 18.6f, 20.5f, 21.6f, 22.4f, 23.0f, 23.9f);
    }

    public static void RenderAt(params float[] times)
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath);

        RenderStills(Root("Shoot").GetComponent<PlayableDirector>(), times, StillsFolder);
        Debug.Log("🖼️ Stills in " + StillsFolder);
    }

    // =========================
    // SET A: THE CONTROL ROOM
    // =========================

    static void BuildRoom(Transform sets, Transform sparks)
    {
        Transform a = AddGroup("A_Room", sets);

        AddSprite("Wall", a, ArtSprite("room_wall"), new Vector2(0f, Floor + 3.5f), 0);

        for (int i = 0; i < SmallScreens.Length; i++)
        {
            Vector2Int s = SmallScreens[i];
            AddSprite("Screen " + (i + 1), a, ArtSprite("feed_" + (i % 2) + "_0"),
                WallPixel(s.x + 16f, s.y + 10f), 2, unlit: true);
        }
        AddSprite("Big Screen", a, ArtSprite("big_0"), BigScreen, 2, unlit: true);

        AddSprite("Floor", a, ArtSprite("room_floor"), new Vector2(0f, Floor - 10f / 16f), 20);
        AddSprite("Console", a, ArtSprite("console"), new Vector2(3.6f, Floor + 15f / 16f), 5);
        AddSprite("Button", a, ArtSprite("button_small"), new Vector2(GeneralAt.x + 0.85f, -1.95f), 7);

        AddSprite("Dobbs", a, ArtSprite("dobbs"), DobbsAt, 6, tint: new Color(0.95f, 0.95f, 1f));
        AddSprite("Throne", a, ArtSprite("throne"), GeneralAt, 8);
        AddSprite("General", a, ArtSprite("general_sit"), GeneralAt, 9);

        // The alarm beacon on the ceiling
        AddSprite("Beacon", a, LoadSprite("Assets/Cutscenes/AfterStage1/Art/beacon_off.png"),
            new Vector2(0.5f, 4.25f), 3, unlit: true, scale: 2f);

        AddLight("Screen Glow", a, BigScreen + new Vector2(0f, -0.4f), ScreenGreen, 0.7f, 7f);
        AddLight("Screen Glow L", a, WallPixel(124f, 32f), ScreenGreen, 0.5f, 6f);
        AddLight("Alarm Spin", a, new Vector2(0.5f, 4.1f), AlarmRed, 0f, 14f, 50f, 180f, 0.25f, 0.5f);
        AddLight("Alarm Wash", a, new Vector2(-2f, 1f), AlarmRed, 0f, 12f, falloff: 0.3f);

        BuildTea(sparks, a, CupAt);
    }

    // The spit-take: the intro's sparks, as one brown spray to the right
    static void BuildTea(Transform sparks, Transform set, Vector2 position)
    {
        GameObject tea = Object.Instantiate(sparks.gameObject, set);
        tea.name = "Tea";
        tea.transform.localPosition = position;

        ParticleSystem ps = tea.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.duration = 1f;
        main.startDelay = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.0625f, 0.125f);
        main.gravityModifier = 1.4f;
        main.maxParticles = 64;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.3f, 0.1f), new Color(0.8f, 0.55f, 0.3f));

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30), new ParticleSystem.Burst(0.08f, 14) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(-20f, 90f, 0f);

        tea.SetActive(false);
    }

    // =========================
    // SET B: THE BUTTON, CLOSE
    // =========================

    static void BuildButton(Transform sets)
    {
        Transform b = AddGroup("B_Button", sets);

        AddSprite("Panel", b, ArtSprite("button_panel"), new Vector2(ButtonX, 0f), 0);
        AddSprite("Button", b, ArtSprite("button_up"), new Vector2(ButtonX, 0.25f), 3);
        AddSprite("Cover Closed", b, ArtSprite("cover_closed"), new Vector2(ButtonX, 0.35f), 5);
        AddSprite("Cover Open", b, ArtSprite("cover_open"), new Vector2(ButtonX, 2f), 5)
            .gameObject.SetActive(false);
        AddSprite("Fist", b, ArtSprite("fist"), new Vector2(ButtonX, 6f), 8);

        AddLight("Button Glow", b, new Vector2(ButtonX, 0.6f), AlarmRed, 0.5f, 3f);
    }

    // =========================
    // SET C: THE HANGAR AT NIGHT
    // =========================

    static void BuildHangar(Transform sets, Transform smoke)
    {
        Transform c = AddGroup("C_Hangar", sets);
        Vector2 anchor = new Vector2(HangarX, 1.5f);
        Vector2 doorAt = new Vector2(HangarX, Floor + 3.25f);

        AddParallax("Sky", c, LoadSprite("Assets/Cutscenes/AfterStage1/Art/sky_night.png"),
            new Vector2(HangarX - 1f, -1.5f), 1f, anchor, -100);
        AddParallax("Mountains", c, LoadSprite("Assets/Cutscenes/AfterStage1/Art/night_far.png"),
            new Vector2(HangarX, -3.3f), 0.85f, anchor, -90);

        AddSprite("Inside", c, ArtSprite("hangar_inside"), doorAt, -30);

        // The Mecha-Lion, asleep in the dark (BossFight art, posed as
        // in the boss scene)
        Transform mecha = AddGroup("Mecha", c, MechaAt);
        mecha.localScale = new Vector3(MechaScale, MechaScale, 1f);
        AddSprite("Body", mecha, LoadSprite("Assets/BossFight/Art/MechaBody.png"),
            new Vector2(0f, 1.75f), -28, tint: Shadow);
        SpriteRenderer head = AddSprite("Head", mecha, LoadSprite("Assets/BossFight/Art/MechaHead.png"),
            HeadOffset, -27, tint: Shadow);
        AddSprite("Eyes", head.transform, ArtSprite("mecha_eyes"), Vector2.zero, -26, unlit: true,
            tint: new Color(1f, 1f, 1f, 0f));

        // The door rolls up behind the lintel; a mask hides what has
        // gone up past the opening
        SpriteRenderer door = AddSprite("Door", c, ArtSprite("hangar_door"), doorAt, -24);
        door.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        SpriteMask mask = AddGroup("Door Mask", c, doorAt).gameObject.AddComponent<SpriteMask>();
        mask.sprite = ArtSprite("hangar_inside");

        AddSprite("Facade", c, ArtSprite("hangar_facade"), new Vector2(HangarX, Floor + 4.5f), -20);
        AddSprite("Ground", c, LoadSprite("Assets/Cutscenes/AfterStage1/Art/base_ground.png"),
            new Vector2(HangarX, Floor - 1.5f), 0);

        Sprite beaconOff = LoadSprite("Assets/Cutscenes/AfterStage1/Art/beacon_off.png");
        foreach ((string side, float x) in new[] { ("L", -6.5f), ("R", 6.5f) })
        {
            AddSprite("Beacon " + side, c, beaconOff, new Vector2(HangarX + x, 4.2f), -19, unlit: true, scale: 2f);
            AddLight("Alarm " + side, c, new Vector2(HangarX + x, 4.2f), AlarmRed, 0f, 4f, volume: 0.25f);
        }

        AddLight("Yard Light", c, new Vector2(HangarX, -4.5f), Sodium, 0f, 10f, 70f, 0f, 0.15f, 0.5f);
        AddLight("Eye Glow", c, EyesAt, AlarmRed, 0f, 2.5f, volume: 0.2f);

        smoke.SetParent(c, false);
        smoke.localPosition = new Vector2(HangarX, Floor + 0.1f);
        smoke.gameObject.SetActive(false);
    }

    // =========================
    // CAMERAS
    // =========================

    static readonly string[] CameraNames = {
        "CM_Wide", "CM_Monitor", "CM_Tea", "CM_Order", "CM_Button", "CM_Alarm", "CM_NotNow", "CM_Hangar" };

    static Dictionary<string, CinemachineCamera> BuildCameras(Transform cameras, Transform template)
    {
        var cams = new Dictionary<string, CinemachineCamera>();

        foreach (string name in CameraNames)
        {
            GameObject cam = Object.Instantiate(template.gameObject, cameras);
            cam.name = name;
            cams[name] = cam.GetComponent<CinemachineCamera>();
        }

        return cams;
    }

    static void Shot(Clip clip, string cam, Curve x, Curve y, Curve size, Curve shake)
    {
        clip.Move(cam, x, y, -10f);
        clip.Float(cam, typeof(CinemachineCamera), "Lens.OrthographicSize", size);
        clip.Float(cam, typeof(CinemachineBasicMultiChannelPerlin), "AmplitudeGain", shake);
    }

    static Clip CameraClip()
    {
        Clip clip = new Clip("Cameras");

        // The room: a slow push across the screens
        Shot(clip, "CM_Wide",
            new Curve(0f, -1.4f, Ease.Linear).To(Monitor, -1f),
            new Curve(0f, 1.25f, Ease.Linear).To(Monitor, 1.15f),
            new Curve(0f, 3.45f, Ease.Linear).To(Monitor, 3.3f),
            Curve.Constant(0.1f));

        // The big screen; a jolt on the hit
        Shot(clip, "CM_Monitor",
            new Curve(Monitor, BigScreen.x, Ease.Hold),
            new Curve(Monitor, BigScreen.y, Ease.Hold),
            new Curve(Monitor, 1.55f, Ease.Linear).To(Tea, 1.4f),
            new Curve(Monitor, 0.1f, Ease.Hold).To(Hit, 0.1f, Ease.Smooth).To(Hit + 0.05f, 1.2f).To(Hit + 0.4f, 0.1f));

        // Tea: a punch-in on the spit
        Shot(clip, "CM_Tea",
            new Curve(Tea, -3.3f, Ease.Linear).To(Spit, -3.35f, Ease.Hold).To(Order, -3.45f),
            new Curve(Tea, -0.55f, Ease.Linear).To(Spit, -0.5f, Ease.Hold).To(Order, -0.45f),
            new Curve(Tea, 1.55f, Ease.Linear).To(Spit - 0.01f, 1.5f, Ease.Hold).To(Spit, 1.3f).To(Order, 1.25f),
            new Curve(Tea, 0.1f, Ease.Hold).To(Spit, 2.2f, Ease.Smooth).To(Spit + 0.5f, 0.2f));

        // The order: a push in as he points
        Shot(clip, "CM_Order",
            new Curve(Order, -3.0f).To(Button, -3.4f),
            new Curve(Order, -0.5f).To(Button, -0.25f),
            new Curve(Order, 1.8f).To(Button, 1.35f),
            Curve.Constant(0.15f));

        // The button: a slow push; the slam shakes everything
        Shot(clip, "CM_Button",
            new Curve(Button, ButtonX, Ease.Hold),
            new Curve(Button, -0.35f, Ease.Hold),
            new Curve(Button, 2.6f, Ease.Linear).To(Slam - 0.01f, 2.4f, Ease.Hold).To(Slam, 2.2f).To(Alarm, 2.3f),
            new Curve(Button, 0.15f, Ease.Hold).To(Slam, 0.15f, Ease.Smooth).To(Slam + 0.04f, 5f).To(Slam + 0.7f, 0.4f));

        // The alarm: the room in red, Dobbs and the General
        Shot(clip, "CM_Alarm",
            new Curve(Alarm, -5.6f, Ease.Linear).To(NotNow, -5.2f),
            new Curve(Alarm, -0.2f, Ease.Linear).To(NotNow, -0.25f),
            new Curve(Alarm, 2.3f, Ease.Linear).To(NotNow, 2.15f),
            Curve.Constant(0.3f));

        // "Not now, Dobbs!": close, a shake on "Dobbs!"
        Shot(clip, "CM_NotNow",
            new Curve(NotNow, -3.55f, Ease.Hold),
            new Curve(NotNow, -0.3f, Ease.Hold),
            new Curve(NotNow, 1.4f, Ease.Linear).To(Gen02 + 0.95f, 1.3f, Ease.Hold).To(Gen02 + 1f, 1.15f).To(Hangar, 1.12f),
            new Curve(NotNow, 0.2f, Ease.Hold).To(Gen02 + 0.95f, 0.2f, Ease.Smooth).To(Gen02 + 1.05f, 2f).To(Gen02 + 1.6f, 0.2f));

        // The hangar: wide on the door, then in to the eyes
        Shot(clip, "CM_Hangar",
            new Curve(Hangar, HangarX, Ease.Linear).To(Eyes, HangarX - 0.6f).To(Roar, EyesAt.x + 0.2f).To(Cut, EyesAt.x),
            new Curve(Hangar, 1.4f, Ease.Linear).To(Eyes, 1.45f).To(Roar, EyesAt.y - 0.2f).To(Cut, EyesAt.y - 0.25f),
            new Curve(Hangar, 4.4f, Ease.Linear).To(Eyes, 3.3f).To(Roar, 2.2f).To(Cut, 2.0f),
            new Curve(Hangar, 0.15f, Ease.Hold).To(Door, 0.15f).To(Door + 0.3f, 0.5f)
                .To(Door + 2.6f, 0.45f).To(Door + 2.9f, 0.15f, Ease.Hold).To(Roar, 0.15f, Ease.Smooth)
                .To(Roar + 0.1f, 3.2f).To(Cut, 1.4f));

        return clip;
    }

    // =========================
    // PROPS
    // =========================

    static Clip PropsClip()
    {
        Clip clip = new Clip("Props");

        clip.Active("A_Room", (0f, true), (Button, false), (Alarm, true), (Hangar, false));
        clip.Active("B_Button", (0f, false), (Button, true), (Alarm, false));
        clip.Active("C_Hangar", (0f, false), (Hangar, true));

        AnimateRoom(clip);
        AnimateButton(clip);
        AnimateHangar(clip);

        return clip;
    }

    static void AnimateRoom(Clip clip)
    {
        const string a = "A_Room/";

        // Tea, the spit, the order; back to his tea while Dobbs talks
        clip.Sprites(a + "General", new SpriteKeys()
            .Set(0f, ArtSprite("general_sit"))
            .Set(Sip, ArtSprite("general_sip"))
            .Set(Sip + 1.4f, ArtSprite("general_sit"))
            .Set(Tea, ArtSprite("general_sip"))
            .Set(Spit, ArtSprite("general_spit"))
            .Set(Order, ArtSprite("general_shout"))
            .Set(Alarm, ArtSprite("general_reach"))
            .Set(Dobbs01 + 0.5f, ArtSprite("general_sit"))
            .Set(Dobbs01 + 1.1f, ArtSprite("general_sip"))
            .Set(NotNow, ArtSprite("general_shout")));

        // He shakes with rage on "Dobbs!"
        Curve shake = new Curve(0f, GeneralAt.x, Ease.Hold);
        for (float t = Gen02 + 0.95f; t < Gen02 + 1.5f; t += 0.08f)
            shake.To(t, GeneralAt.x + 1f / 16f, Ease.Hold).To(t + 0.04f, GeneralAt.x, Ease.Hold);
        clip.Move(a + "General", shake, Curve.Constant(GeneralAt.y));

        clip.Sprites(a + "Dobbs", new SpriteKeys()
            .Set(0f, ArtSprite("dobbs"))
            .Set(Dobbs01 - 0.15f, ArtSprite("dobbs_raised"))
            .Set(Hangar - 0.3f, ArtSprite("dobbs")));

        // The screens: the fight on a loop, static on the slam, then
        // WARNING everywhere and MECHA-LION WAKING on the big one
        Sprite[][] fights = {
            Arts("feed_0_0", "feed_0_1", "feed_0_2", "feed_0_3"),
            Arts("feed_1_0", "feed_1_1", "feed_1_2", "feed_1_3") };
        Sprite[] warn = Arts("warn_on", "warn_off");
        Sprite[] noise = Arts("static_0", "static_1");

        for (int i = 0; i < SmallScreens.Length; i++)
        {
            Sprite[] fight = fights[i % 2];
            int phase = i * 3 % 4;
            clip.Sprites(a + "Screen " + (i + 1), new SpriteKeys()
                .Cycle(0f, Slam + 0.1f, 0.32f + 0.03f * i, fight.Skip(phase).Concat(fight.Take(phase)).ToArray())
                .Cycle(Slam + 0.1f, Slam + 0.4f, 0.05f, noise)
                .Cycle(Slam + 0.4f + 0.05f * i, End, 0.3f, warn));
        }

        Sprite[] big = Arts("big_0", "big_1", "big_2", "big_3");
        clip.Sprites(a + "Big Screen", new SpriteKeys()
            .Cycle(0f, Monitor, 0.4f, big)
            .Set(Monitor, big[0]).Set(Fire, big[1]).Set(Hit, big[2]).Set(Down, big[3])
            .Cycle(UnitsDown, Slam + 0.1f, 0.3f, ArtSprite("big_down"), ArtSprite("big_down"), big[3])
            .Cycle(Slam + 0.1f, Slam + 0.4f, 0.05f, noise)
            .Cycle(Slam + 0.4f, End, 0.35f, ArtSprite("big_mecha"), ArtSprite("big_down")));

        // The alarm: the beacon spins, red washes the room
        SpriteKeys beacon = new SpriteKeys().Set(0f, LoadSprite("Assets/Cutscenes/AfterStage1/Art/beacon_off.png"))
            .Cycle(Slam + 0.3f, End, 0.3f, LoadSprite("Assets/Cutscenes/AfterStage1/Art/beacon.png"),
                LoadSprite("Assets/Cutscenes/AfterStage1/Art/beacon_off.png"));
        clip.Sprites(a + "Beacon", beacon);

        clip.Rotate(a + "Alarm Spin", new Curve(0f, 180f, Ease.Hold).To(Slam, 180f, Ease.Linear).To(End, 180f + 360f * 6f));
        clip.Float(a + "Alarm Spin", typeof(Light2D), "m_Intensity",
            new Curve(0f, 0f, Ease.Hold).To(Slam + 0.3f, 1.6f, Ease.Hold));

        Curve wash = new Curve(0f, 0f, Ease.Hold);
        for (float t = Slam + 0.3f; t < Hangar; t += 0.6f)
            wash.To(t, 1.2f).To(t + 0.3f, 0.35f);
        clip.Float(a + "Alarm Wash", typeof(Light2D), "m_Intensity", wash);

        foreach (string glow in new[] { "Screen Glow", "Screen Glow L" })
            clip.Float(a + glow, typeof(Light2D), "m_Intensity",
                new Curve(0f, 0.6f, Ease.Linear).To(Slam, 0.6f).To(Slam + 0.3f, 0.2f, Ease.Hold));
    }

    static void AnimateButton(Clip clip)
    {
        const string b = "B_Button/";

        clip.Active(b + "Cover Closed", (0f, true), (Cover, false));
        clip.Active(b + "Cover Open", (0f, false), (Cover, true));

        clip.Sprites(b + "Button", new SpriteKeys()
            .Set(0f, ArtSprite("button_up")).Set(Slam + 0.03f, ArtSprite("button_down")));

        // The fist: hangs, then comes down hard and stays there
        clip.Move(b + "Fist",
            Curve.Constant(ButtonX),
            new Curve(0f, 6f, Ease.Hold).To(Slam - 0.22f, 6f, Ease.Linear).To(Slam, 2.55f, Ease.Linear)
                .To(Slam + 0.06f, 1.95f).To(Slam + 0.5f, 2.05f));

        clip.Float(b + "Button Glow", typeof(Light2D), "m_Intensity",
            new Curve(0f, 0.5f, Ease.Hold).To(Slam, 4f).To(Slam + 0.6f, 1.2f));
    }

    static void AnimateHangar(Clip clip)
    {
        const string c = "C_Hangar/";
        float doorY = Floor + 3.25f;

        clip.Move(c + "Door", Curve.Constant(HangarX),
            new Curve(0f, doorY, Ease.Hold).To(Door, doorY, Ease.Linear).To(Door + 2.6f, doorY + 6.5f, Ease.Hold));

        // Light from the yard spills in as the door goes up
        clip.Float(c + "Yard Light", typeof(Light2D), "m_Intensity",
            new Curve(0f, 0f, Ease.Hold).To(Door, 0f).To(Door + 2.6f, 0.9f));

        // The eyes: on, off, on; then the head lights and it roars
        clip.Float(c + "Mecha/Head/Eyes", typeof(SpriteRenderer), "m_Color.a",
            new Curve(0f, 0f, Ease.Hold).To(Eyes, 1f, Ease.Hold).To(Eyes + 0.15f, 0f, Ease.Hold)
                .To(Eyes + 0.3f, 1f, Ease.Hold));
        clip.Float(c + "Eye Glow", typeof(Light2D), "m_Intensity",
            new Curve(0f, 0f, Ease.Hold).To(Eyes, 2f, Ease.Hold).To(Eyes + 0.15f, 0f, Ease.Hold)
                .To(Eyes + 0.3f, 1.6f).To(Roar, 2f).To(Roar + 0.15f, 3f).To(Cut, 2f));

        foreach (string part in new[] { "Mecha/Body", "Mecha/Head" })
        {
            foreach (string channel in new[] { "r", "g", "b" })
            {
                float lit = channel == "r" ? 0.6f : 0.45f;
                clip.Float(c + part, typeof(SpriteRenderer), "m_Color." + channel,
                    new Curve(0f, Shadow.r, Ease.Hold).To(Glow, Shadow.r).To(Roar, lit));
            }
        }

        clip.Sprites(c + "Mecha/Head", new SpriteKeys()
            .Set(0f, LoadSprite("Assets/BossFight/Art/MechaHead.png"))
            .Set(Roar, LoadSprite("Assets/BossFight/Art/MechaHeadRoar.png")));
        clip.Sprites(c + "Mecha/Head/Eyes", new SpriteKeys()
            .Set(0f, ArtSprite("mecha_eyes")).Set(Roar, ArtSprite("mecha_eyes_roar")));

        // The head lifts a little on the glow, rears on the roar
        clip.Move(c + "Mecha/Head", Curve.Constant(HeadOffset.x),
            new Curve(0f, HeadOffset.y, Ease.Hold).To(Glow, HeadOffset.y).To(Roar - 0.1f, HeadOffset.y + 0.2f)
                .To(Roar + 0.1f, HeadOffset.y + 0.45f).To(Cut, HeadOffset.y + 0.4f));

        // Warning beacons on the facade
        SpriteKeys beacon = new SpriteKeys()
            .Cycle(Hangar, End, 0.3f, LoadSprite("Assets/Cutscenes/AfterStage1/Art/beacon.png"),
                LoadSprite("Assets/Cutscenes/AfterStage1/Art/beacon_off.png"));
        Curve pulse = new Curve(0f, 0f, Ease.Hold);
        for (float t = Hangar; t < End; t += 0.6f)
            pulse.To(t, 2.2f).To(t + 0.3f, 0.2f);

        foreach (string side in new[] { "L", "R" })
        {
            clip.Sprites(c + "Beacon " + side, beacon);
            clip.Float(c + "Alarm " + side, typeof(Light2D), "m_Intensity", pulse);
        }
    }

    // =========================
    // OVERLAY AND LIGHTING
    // =========================

    static void SetUpOverlay()
    {
        Transform overlay = Root("Overlay");

        Child(overlay, "Caption/Place").GetComponent<TMP_Text>().text = "ROYAL CONTROL ROOM";
        Child(overlay, "Caption/Time").GetComponent<TMP_Text>().text = "DAY 9  -  23:58";
        Child(overlay, "Caption").GetComponent<CanvasGroup>().alpha = 0f;
        Child(overlay, "Title").GetComponent<CanvasGroup>().alpha = 0f;
    }

    static Clip OverlayClip()
    {
        Clip clip = new Clip("Overlay");

        clip.Float("Fade", typeof(Image), "m_Color.a",
            new Curve(0f, 1f).To(0.6f, 0f, Ease.Hold).To(Cut, 1f, Ease.Hold));

        clip.Float("Flash", typeof(Image), "m_Color.a",
            new Curve(0f, 0f, Ease.Hold).To(Slam, 0.8f, Ease.Linear).To(Slam + 0.25f, 0f, Ease.Hold)
                .To(Roar, 0.35f, Ease.Linear).To(Roar + 0.3f, 0f, Ease.Hold));

        clip.Float("Caption", typeof(CanvasGroup), "m_Alpha",
            new Curve(0f, 0f, Ease.Hold).To(Caption - 0.05f, 1f, Ease.Hold).To(4.2f, 1f).To(4.6f, 0f, Ease.Hold));
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

        // The hangar is darker than the room
        clip.Float("Global Light 2D", typeof(Light2D), "m_Intensity",
            new Curve(0f, 1f, Ease.Hold).To(Hangar, 0.8f, Ease.Hold));

        // The impact look on the slam and the roar, a touch on the spit
        clip.Float("SlamFX", typeof(Volume), "weight",
            new Curve(0f, 0f, Ease.Hold).To(Spit, 0.3f).To(Spit + 0.4f, 0f, Ease.Hold)
                .To(Slam, 1f).To(Slam + 0.6f, 0f, Ease.Hold)
                .To(Roar, 0.8f).To(Roar + 0.8f, 0.2f).To(Cut, 0f, Ease.Hold));

        return clip;
    }

    // =========================
    // SOUND
    // =========================

    static void AddSound(TimelineAsset timeline)
    {
        AddAudio(timeline, "Music", (0, Sound("mus_afterstage2"), 0.8f));
        AddAudio(timeline, "Voice",
            (Gen01, Sound("vo_gen_01"), 0.9f),
            (Dobbs01, Sound("vo_dobbs_01"), 0.9f),
            (Gen02, Sound("vo_gen_02"), 0.9f));
        AddAudio(timeline, "Ambience",
            (0, Sound("amb_room"), 0.5f),
            (Hangar, Sound("amb_hangar"), 0.55f));
        AddAudio(timeline, "SFX 1",
            (Caption, Sound("sfx_type_1"), 0.6f),
            (Caption + 1.1f, Sound("sfx_type_2"), 0.6f),
            (Sip + 0.25f, Sound("sfx_sip"), 0.6f),
            (Monitor, Sound("sfx_static"), 0.5f),
            (Fire, Sound("sfx_cctv"), 0.6f));
        AddAudio(timeline, "SFX 2",
            (Spit, Sound("sfx_spit"), 0.8f),
            (Spit + 0.3f, Sound("sfx_cup"), 0.6f),
            (Cover, Sound("sfx_cover"), 0.7f),
            (Slam, Sound("sfx_slam"), 0.9f),
            (Slam + 0.1f, Sound("sfx_static"), 0.4f));
        AddAudio(timeline, "SFX 3",
            (Slam + 0.3f, Sound("sfx_alarm"), 0.45f));
        AddAudio(timeline, "SFX 4",
            (Door, Sound("sfx_door"), 0.8f),
            (Eyes, Sound("sfx_eyes"), 0.7f),
            (Glow, Sound("sfx_servos"), 0.6f),
            (Roar, Sound("sfx_roar"), 0.95f));
    }
}
