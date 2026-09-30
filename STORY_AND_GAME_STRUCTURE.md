# OPERATION MANEFORD - Story, Dialogue & Game Structure (v2)

Title: **OPERATION MANEFORD** (a Contra-style run-and-gun; renamed from the working title "King of the Forest")

> **Instructions for Claude (read first)**
> This file is the single source of truth for the story, characters, dialogue, and stage flow. When the user asks for story changes, edit this file first, then update the Unity project to match.
>
> **Working rules**
> 1. This is the user's own game. You may edit, add, or restructure any scenes, scripts, and assets as needed.
> 2. Don't worry about what is or isn't implemented yet. Treat this file as the target design and update the project to match it.
> 3. Read the existing scripts in `Assets/Scripts` (player movement, shooting, enemy health, bullets) and reuse or refactor them where useful, keeping the style consistent.
> 4. Keep `.meta` files with everything you create or move.
> 5. Everything story-related is data-driven: dialogue lives in assets (ScriptableObjects or JSON), not hard-coded in scripts.
> 6. Keep the game playable after each step. Build in the order in "Build Order" at the bottom.
> 7. Commit to Git after each working step.
> 8. Anything unspecified (art, sound, menus, exact numbers) can use sensible defaults and placeholders.

---

## 1. Design Lessons (why the story is built this way)

- Contra-style stories work when they are **gung-ho, quick, and confident**. Reviews of recent Contra story modes praise the unashamed action-movie tone but criticize cutscenes that drag. So: **short scenes (6 to 10 lines max), strong jokes, no exposition dumps.**
- Contra-style games also let allies and enemies **trade words mid-stage**. We do the same with short in-level overlay lines (radio chatter, intercom announcements) so the story never stops the action.
- Immersion in 2D comes from **voice, not graphics**: each character sounds different, has running gags, and reacts to what the player just did. Callbacks pay off later.
- Give the player a **reason to care** about the enemies. Here: the lions are victims, which makes the villain worse and the ending land.
- All characters, names, and lines here are original. Do not import characters or plot from any Contra game.

## 2. Premise

The Maneford Wildlife Reserve went silent nine days ago. Rangers vanished, communications died, and the animals started "acting organized." Elite operative **Rook** is dropped in with a wildlife vet, **Pip**, guiding him by radio.

They discover that **General Leopold "The King" Maneford**, a decorated officer who feels the world never gave him a throne, has taken over the reserve. He is fitting the reserve's lions with **mind-control collars** and building a private army around them, because "soldiers complain, and lions don't."

The twist the player feels: **the lions are victims.** Every lion Rook defeats has its collar shatter and runs free. The boss, a giant **Mecha-Lion**, is built around a recording of the reserve's alpha lion, **Sultan**, who is raised by the ranger **Tala**, who is locked up in the General's base. Freeing Sultan and the forest is the real mission.

## 3. Cast

| ID | Name | Role | Voice and personality | Text color | Radio? |
|---|---|---|---|---|---|
| `ROOK` | Rook | Player hero (callsign) | Deadpan, short sentences, dry one-liners. Secretly soft-hearted about animals but insists he is "not a cat person." | white | no |
| `PIP` | Dr. Priya "Pip" Nair | Wildlife vet, mission control by radio | Fast-talking, over-caffeinated, dramatic, brilliant. Calls the lions "patients" and scolds Rook for "traumatizing" them. | cyan | yes (static effect) |
| `TALA` | Tala Reyes | Reserve ranger, rescued after Stage 2, joins the team | Calm, blunt, fiercely protective. Few words, each one lands. Raised Sultan from a cub. | green | no (after rescue, in the helicopter) |
| `GEN` | General Leopold "The King" Maneford | Main villain | Grand speeches, speaks of himself in third person when angry ("The King does not retreat!"), obsessed with royal metaphors and his own budget. Gets petty when interrupted. | gold | via PA and in person |
| `DOBBS` | Corporal Dobbs | Comic relief, defects | Nervous, over-shares, desperate to be liked. Started as a lion feeder and keeps getting longer job titles. | orange | via PA at first |
| `PA` | Base Intercom | Environmental voice | Used for announcements read by Dobbs or the General. | gray | n/a |
| `SULTAN` | Sultan | Alpha lion (no dialogue, only a roar) | Appears in the boss recording and the epilogue. | - | - |

Portrait emotions for each speaker: `neutral`, `angry`, `smug`, `surprised`, `panic`, and `soft` (used for Rook, Tala, and Pip in emotional beats). Use placeholder colored boxes until art exists.

### Running gags and callbacks (keep these consistent)
1. **"Not a cat person."** Rook says it repeatedly while clearly caring. Final payoff in the epilogue.
2. **"Patients."** Pip calls lions patients. Rook starts using the word without noticing, in the chase.
3. **Dobbs' job title** gets longer every time it is said, until it is absurd.
4. **The General's budget:** he keeps citing how much the Mecha-Lion cost (it grows each time).
5. **"Where is Dobbs?"** The General yells it in the chase because Dobbs defected and the General cannot fly properly, and the fuel gauge (Dobbs' job) is broken.

## 4. Emotional Arc (one line per stage)

1. **Stage 1 (lions):** Mystery and shock. The player realizes the lions are being controlled and starts shooting to free them, not kill them.
2. **Stage 2 (soldiers):** Anger and comedy. The soldiers are the ones responsible. The base intercom provides humor.
3. **After Stage 2:** Hope. Tala is rescued and the team forms.
4. **Boss (Mecha-Lion):** Personal. The General has turned Sultan's roar into a weapon.
5. **Chase:** Release. A fast, funny, high-energy finale.
6. **Epilogue:** Heart. Sultan and the lions are free. Rook's secret is out.

## 5. Game Flow (Stage Structure)

| # | Unity scene name | Type | Description | Before | After |
|---|---|---|---|---|---|
| 0 | `Cutscene_Intro` | Video | Opening with Pip's voiceover | - | `Dialogue_Landing` |
| 0b | `Dialogue_Landing` | Dialogue | Rook lands, meets Pip on the radio | `Cutscene_Intro` | Stage 1 |
| 1 | `Stage 1` | Platformer | Lions in packs. Defeated lions' collars shatter, and they run free | `Dialogue_Landing` | `Cutscene_AfterStage1` |
| 1b | `Cutscene_AfterStage1` | Video | The freed lion, the General reacts | `Stage 1` | `Dialogue_AfterStage1` |
| 1c | `Dialogue_AfterStage1` | Dialogue | Rook and Pip find the collar | `Cutscene_AfterStage1` | `Dialogue_BeforeStage2` |
| 1d | `Dialogue_BeforeStage2` | Dialogue | The base gate and the banner | `Dialogue_AfterStage1` | Stage 2 |
| 2 | `Stage 2` | Platformer | Soldiers at the General's base, intercom chatter | `Dialogue_BeforeStage2` | `Cutscene_AfterStage2` |
| 2b | `Cutscene_AfterStage2` | Video | The General's control room, he wakes the Mecha-Lion | `Stage 2` | `Dialogue_AfterStage2` |
| 2c | `Dialogue_AfterStage2` | Dialogue | Dobbs surrenders, Tala is freed | `Cutscene_AfterStage2` | `Dialogue_PreBoss` |
| 3 | `BossFight_MechaLion` | Boss fight | Mecha-Lion, 2 phases | `Dialogue_PreBoss` | `Cutscene_HelicopterEscape` |
| 3b | `Cutscene_HelicopterEscape` | Video | The General flees by helicopter | `BossFight_MechaLion` | `Dialogue_PreChase` |
| 4 | `Stage_HelicopterChase` | Auto-scroll shooter | Chase the General's helicopter | `Dialogue_PreChase` | `Cutscene_Ending` |
| 4b | `Cutscene_Ending` | Video | The crash, sunrise | `Stage_HelicopterChase` | `Dialogue_Epilogue` |
| 5 | `Dialogue_Epilogue` | Dialogue | The ridge at sunrise | `Cutscene_Ending` | `Credits` |
| 6 | `Credits` | UI | The end, credits, return to menu | - | Main menu |

**Rule: every stage ends with a video cutscene** (Stage 1, Stage 2, the boss fight, and the chase), followed by the related static dialogue scene(s) where there is one. The game also opens with `Cutscene_Intro`.

Stage 2 may not exist in the project yet. If so, create it (or a simple placeholder with soldier enemies). Scene names can be changed to match the project.

### 5.1 Levels

The flow above is grouped into **three levels**, like the stages of the original game. Every scene is its own Unity scene, and the game always moves forward to the next one: after each cutscene or dialogue, the next scene loads. A level opens with a "LEVEL n" title card (shown during the fade into its first scene).

| Section | Title card | Scenes, in order |
|---|---|---|
| Prologue | - | `Cutscene_Intro` |
| Level 1 | LEVEL 1 - THE FOREST | `Dialogue_Landing`, `Stage 1`, `Cutscene_AfterStage1`, `Dialogue_AfterStage1` |
| Level 2 | LEVEL 2 - THE KING'S BASE | `Dialogue_BeforeStage2`, `Stage 2`, `Cutscene_AfterStage2`, `Dialogue_AfterStage2` |
| Level 3 | LEVEL 3 - THE KING | `Dialogue_PreBoss`, `BossFight_MechaLion`, `Cutscene_HelicopterEscape`, `Dialogue_PreChase`, `Stage_HelicopterChase` |
| Ending | - | `Cutscene_Ending`, `Dialogue_Epilogue`, `Credits` |

- `MainMenu` comes first (build index 0): Start Game, Level Select, Quit. A level unlocks in Level Select once the player reaches it. `Credits` returns to the main menu.
- A playable stage ends when its goal is done (Stage 2: defeat enough soldiers), the player reaches the exit, and STAGE CLEAR shows. Then the stage's video cutscene plays. A game over retries the same stage.
- **Stage 1 ends on the spot:** freeing the 10th lion is the end of the level. That collar break plays in a moment of slow motion, STAGE CLEAR shows, and the game goes straight into `Cutscene_AfterStage1` (no walk to the exit). In Unity this is `clearOnGoal` on the stage's `EnemyKillManager`.
- In Unity this order lives in the `GameFlow` asset (`Assets/Resources/GameFlow.asset`), and `SceneFlow` follows it. To reorder scenes or rename a level, edit that asset (and this section).

## 6. Dialogue System Spec

### 6.1 Data model
- `DialogueLine`: `speakerId`, `emotion`, `text`, optional `side` (left/right), optional `isRadio` (override), optional effects.
- `DialogueSequence`: ordered list of lines plus `nextSceneName`.
- Stored as assets or JSON in `Assets/Dialogue/Data/`, one sequence per entry in sections 8 and 9.
- Reusable `DialogueScene` prefab: background `Image`, left and right portrait `Image`s, name label, TextMeshPro text box.
- Reusable `DialogueOverlay` (small box at the bottom of the screen) for in-level lines that do not pause the game.

### 6.2 Immersion features (implement all, they are cheap)
- **Typewriter** at about 40 characters per second. Press a key to finish the line instantly, press again to advance.
- **Speaker blips:** a short, different pitch of "blip" sound per character while text types out.
- **Portrait life:** the speaking portrait does a tiny bob, the listening portrait dims slightly. Portraits swap by `emotion`.
- **Radio effect (`PIP` and radio lines):** show an "[RADIO]" tag, a faint static flicker on the box, and a quiet static sound at the start of the line.
- **Inline text commands** (parsed from the text):
  - `{pause=0.5}` waits 0.5 s mid-line
  - `{shake}` shakes the screen and the box briefly
  - `{sfx=roar}` plays a named sound
  - TextMeshPro rich text such as `<b>`, `<i>` and `<color=#FFD700>` for emphasis
- **Speaker colors** as in the cast table, applied to the name label.
- **Pacing rules:** no more than 10 lines per scene and no line over about 140 characters. Prefer short exchanges, interruptions (lines ending in "..."), and reactions.
- **Overlay lines** pause nothing: they fade in at the bottom, show for a few seconds (or until the line finishes plus about 2 s), then fade out. Multiple lines queue.
- **Skip:** hold Enter to skip a whole dialogue scene.

### 6.3 Optional stretch: reply choices (build last, only if time remains)
In 2 or 3 dialogue scenes, let the player choose between two replies for Rook (for example "Deadpan" vs "Soft"). The choice changes only the next line's reaction and one line in the epilogue (a callback). Store the choices in a simple `StoryFlags` dictionary.

### 6.4 Storytelling in the levels (environment, no dialogue needed)
- Stage 1 shows broken cages, torn ranger tents, and a sign reading "Lion Feeding Schedule." Ranger name tags lie on the ground.
- Stage 2 has propaganda posters of the General in a lion-mane helmet, a "Royal Treasury" crate, and a half-finished statue of him on a lion.
- Defeated lions: the collar **shatters with a spark and a chime**, the lion shakes its head, and runs off screen. No gore. The enemy "death" is a release.

## 7. Cutscene Types

### 7.1 Video cutscenes (5 total)
Played with the `VideoPlayer` on a full-screen `RawImage`/`RenderTexture`. Skippable by holding Enter. Use placeholders (a still image with text) until final video files exist.

| ID | Planned content | Voiceover / on-screen text |
|---|---|---|
| `Cutscene_Intro` | Aerial shot of the forest at dusk, quiet. A distant roar. A chained gate. A parachute opens. Title card: OPERATION MANEFORD. | PIP (voiceover): "Day nine. The Maneford Reserve stopped answering our calls. We sent drones. They came back with... questions. So now we're sending Rook." |
| `Cutscene_AfterStage1` | The last collar in the pack shatters. The freed lion shakes its head, looks back at Rook for a beat, then runs into the forest. In the distance, searchlights sweep over a fenced military base with a giant banner of the General's face. *As made (24.4 s):* a forest clearing at blue hour, close on the sparking collar; it breaks; a two-shot as the lion looks back at Rook and runs off; a far roar as searchlights rise over the hills. Night: the compound seen from the forest edge with Rook in silhouette (caption ROYAL MANEFORD COMPOUND / DAY 9 - 21:40); a push in on the banner while he speaks; the alarm, the gate rolls open and the soldiers march out towards the forest; hard cut to black. | GEN (voice from a speaker): "He freed my lions? Fine. Release the soldiers." |
| `Cutscene_AfterStage2` | A control room full of monitors showing Rook beating the soldiers. The General sips tea, spills it, and presses a big red button labeled "DO NOT PRESS (ROYAL)." A hangar door opens and the Mecha-Lion's glowing eyes light up. In the background, Dobbs raises his hand. | GEN: "Wake the Mecha-Lion." / DOBBS: "Sir, about the fuel gauge..." / GEN: "Not now, Dobbs!" |
| `Cutscene_HelicopterEscape` | The Mecha-Lion collapses and explodes. The General climbs out of the wreckage with singed hair and his helmet askew, runs for a helicopter, and lifts off. A second helicopter (unlocked, no pilot) sits nearby. Rook and Tala run to it. | GEN: "This is not over! The King will return! {pause} After a short break!" |
| `Cutscene_Ending` | Dawn. The General's helicopter sputters, spins, and crashes into a field. Rook and Tala walk up, guns raised; he crawls out with the mane helmet crooked and kneels. Rook lowers his gun. As the first sunlight reaches them, Tala lifts the lion-mane helmet off his head. Fade to white: sunrise on a ridge, Rook sitting among the freed lions; one lies down beside him and he rests his hand on its mane. Far off, a great lion stands on a rock (Sultan, named in the epilogue). | GEN: "Go on, then. The King... is ready." / ROOK: "Nobody's finishing anything." / ROOK: "They were never your army. They were just lions." / GEN: "They were the only subjects... who ever bowed." / TALA: "It's over, Leopold." |

## 8. Dialogue Scripts (static scenes)

Format: `SPEAKER (emotion): "text"` with optional inline commands.

### 8.1 `Dialogue_Landing` - Background: forest edge at dusk, a parachute caught in a tree
1. PIP (surprised) [RADIO]: "Rook! You're down! Say something so I know you're alive!"
2. ROOK (neutral): "Alive."
3. PIP (neutral) [RADIO]: "That's it? That's all I get?"
4. ROOK (neutral): "Also cold."
5. PIP (panic) [RADIO]: "Okay! Mission brief, fast. Nine days ago the whole reserve went silent. Rangers gone, lights gone, and the animals started acting... organized."
6. ROOK (neutral): "Organized how?"
7. PIP (neutral) [RADIO]: "Formations. Patrols. One lion held a door open for another lion."
8. ROOK (neutral): "Right."
9. PIP (soft) [RADIO]: "If something furry comes at you, please try to discourage it. Gently."
10. ROOK (neutral): "I'll discourage it. Loudly."

### 8.2 `Dialogue_AfterStage1` - Background: a clearing, a shattered collar glinting on the ground
1. ROOK (neutral): "Found something. A collar. It's cracked open."
2. PIP (surprised) [RADIO]: "Scan it! Hold it up... oh no. That's a neural control chip. Rook, those lions weren't aggressive. They were being driven."
3. ROOK (soft): "They didn't know what they were doing."
4. PIP (soft) [RADIO]: "No. They were scared the whole time."
5. ROOK (angry): "Then whoever did this is going to have a very bad day."
6. PIP (surprised) [RADIO]: "There's a name tag on the collar. 'Reserve Ranger Unit 3, T. Reyes.' Somebody raised this one."
7. ROOK (neutral): "Where is she?"
8. PIP (neutral) [RADIO]: "My guess? Wherever the lions came from. There's a base east of you."
9. ROOK (neutral): "Not a cat person. But I'm going."

### 8.3 `Dialogue_BeforeStage2` - Background: the General's base gate with a giant banner of his face in a lion-mane helmet
1. ROOK (neutral): "That's a big banner."
2. PIP (surprised) [RADIO]: "That's... his face. With a lion mane. On a helmet. He made a helmet. I'm filing this under 'concerning.'"
3. ROOK (neutral): "Who is he?"
4. PIP (neutral) [RADIO]: "General Leopold Maneford. Decorated. Retired. Apparently unretired. He used to run this reserve as a hobby."
5. ROOK (neutral): "Hobby?"
6. PIP (soft) [RADIO]: "He liked lions. Looks like the lions did not like him back."

### 8.4 `Dialogue_AfterStage2` - Background: a locked lion kennel at the base, Dobbs with hands up
1. DOBBS (panic): "Don't shoot! Please! I'm, I'm Corporal Dobbs, Assistant Under-Secretary of Royal Lion Logistics and, uh, Fuel Gauge Maintenance, Second Class!"
2. ROOK (neutral): "That's a lot of titles."
3. DOBBS (panic): "I only started as a feeder! Then I got promoted! Every week! I don't know why!"
4. ROOK (angry): "Where's Tala Reyes?"
5. DOBBS (surprised): "Who? Oh! The ranger! She's in the kennel, the big one, he said she 'knows too much about lions'! I keep telling him it's called 'having a job'!"
6. ROOK (neutral): "Open it."
7. DOBBS (panic): "Yes! Right now! Please don't tell the General I helped!"
8. TALA (neutral): "Took you long enough."
9. ROOK (neutral): "Traffic."
10. TALA (soft): "The General took my alpha, Sultan. He's using his roar to power something. I'm not leaving until Sultan's free."
11. PIP (surprised) [RADIO]: "A ranger! A real ranger! Rook, do you know how long I've been talking to just you?"
12. ROOK (neutral): "I'm right here."

*(11 and 12 are the max length for a scene. Trim to 10 lines if the scene feels long.)*

### 8.5 `Dialogue_PreBoss` - Background: the arena, a giant shadow, a spotlight
1. GEN (smug): "Behold! The Mecha-Lion! King of the forest, king of the beasts, king of ME!"
2. ROOK (neutral): "It's a robot lion."
3. GEN (angry): "It is a MAGNIFICENT robot lion! Four defense budgets! Ten years of my life!"
4. TALA (angry): "That roar. That's Sultan's voice. You recorded him."
5. GEN (smug): "I borrowed it. A king needs a voice."
6. TALA (angry): "A king needs a kingdom. You have a fence."
7. GEN (angry): "{shake}Silence! The King is ROARING!"
8. PIP (neutral) [RADIO]: "Rook, whatever you do, do not let him do a speech."
9. ROOK (neutral): "Too late."

### 8.6 `Dialogue_PreChase` - Background: the cockpit of the commandeered helicopter, the General's helicopter lights in the distance
1. TALA (neutral): "I can fly planes. This is a helicopter. These are different things."
2. ROOK (neutral): "I'll fly."
3. TALA (surprised): "Can you?"
4. ROOK (neutral): "No."
5. TALA (neutral): "...Great."
6. DOBBS (panic) [RADIO]: "Uh, hi! It's Dobbs! I'm in custody but they let me use the radio! The General's helicopter, the fuel gauge is broken! It's been on empty since Tuesday! Nobody told him!"
7. PIP (surprised) [RADIO]: "So if you can just keep him busy..."
8. ROOK (neutral): "...he runs out of gas."
9. TALA (soft): "Then let's keep him busy."

### 8.7 `Dialogue_Epilogue` - Background: a ridge at sunrise, lions in silhouette
1. TALA (soft): "There. Look."
2. ROOK (soft): "Is that..."
3. TALA (soft): "Sultan."
4. SULTAN: {sfx=roar}
5. PIP (soft) [RADIO]: "All my patients. All of them. Oh, I'm going to cry on a radio."
6. ROOK (neutral): "It's just the wind."
7. PIP (neutral) [RADIO]: "You said you weren't a cat person."
8. ROOK (neutral): "I'm not."
9. TALA (smug): "You're crying, Rook."
10. ROOK (neutral): "...It's the wind."
11. DOBBS (neutral): "Excuse me, do I still, uh, have to feed them?"
12. TALA (neutral): "Yes. Forever."

*(Optional if Choice flags exist: if the player chose the "Soft" reply earlier, Rook's last line becomes: ROOK (soft): "Okay. A little.")*

## 9. In-Level Overlay Lines (no pausing, queue and fade)

### 9.1 Stage 1 (lions)
- **Start:** PIP [RADIO]: "Movement ahead. Four legs. Big. Please don't shoot it in the face." / ROOK: "Where else would I shoot it?" / PIP [RADIO]: "...Fair."
- **First lion's collar shatters:** PIP [RADIO]: "The collar broke! It's running off! Rook, that's a win! Gold star!" / ROOK: "I don't want a gold star."
- **A big pack appears (about the middle of the level):** ROOK: "More." / PIP [RADIO]: "They're flanking you. Somebody trained them to flank."
- **Near the end of the level (loudspeaker, first villain contact; at the 7th lion freed, since the 10th ends the level):** GEN (PA): "Intruder. You are trespassing in the King's forest." / ROOK: "Whose forest?" / GEN (PA): "THE KING'S." / ROOK: "Noted."

### 9.2 Stage 2 (soldiers, base intercom, triggered at points in the level)
- **Start:** DOBBS (PA): "Attention all personnel. This is Royal Herald Dobbs, Assistant Under-Secretary of Lion Logistics, with a message from His Majesty."
- **Midway:** GEN (PA): "Intruders are not allowed. Lions are fine. Soldiers are fine. Dobbs, what was the third thing?" / DOBBS (PA): "...Budget, sir?" / GEN (PA): "Yes! BUDGET!"
- **After a tough wave:** PIP [RADIO]: "Rook, you're doing great. Everyone here is very, very upset. That's good."
- **Near the end:** DOBBS (PA): "Hello, this is Dobbs, Assistant Under-Secretary of Lion Logistics, Fuel Gauge Maintenance, and, uh, Vice Herald? I think they want me to say that the General is angry now."

### 9.3 Mecha-Lion boss
- **Fight start:** GEN (smug): "Roar, my beauty! ROAR!" {sfx=roar}
- **Phase 1, a missile volley:** GEN (smug): "Behold the Royal Missile Mane!"
- **Phase 2 trigger (50% health):** GEN (panic): "Not the mane! I just had it polished! That mane cost one hundred and eighty thousand!"
- **Phase 2, the charge telegraph:** TALA (angry): "It's charging! Jump the shockwave!"
- **Defeated:** GEN (panic): "NOOOO! Four defense budgets! FIVE, if you count the paint!"

### 9.4 Helicopter chase (callouts help the player and add character)
- **Start:** TALA: "Rockets! Left side!" / ROOK: "I see them." / TALA: "Do you?"
- **Drones appear:** TALA: "Drones! Six o'clock!" / PIP [RADIO]: "Those drones have collars too. Please don't be mean to them. But, also, shoot them."
- **At 60% General health:** GEN (angry): "You cannot catch the King! The King flies like the wind!"
- **At 30% General health:** GEN (panic): "DOBBS! Where is Dobbs?! Somebody take the controls!" / PIP [RADIO]: "He defected, sir." / GEN (panic): "WHAT?!"
- **Rook's slip (any point after 30%):** ROOK: "Hit the patient again." / PIP [RADIO]: "...Did you just say 'patient'?" / ROOK: "No."
- **At 0%:** GEN (panic): "{shake}The fuel gauge is BROKEN! This helicopter was a RENTAL!"

## 10. Boss Fight Spec - `BossFight_MechaLion`

- A flat side-view arena using existing forest assets. Fixed camera with barriers at both ends. The player uses the existing movement and shooting.
- The boss is built from separate GameObjects: body, head/mane, and a cockpit visual for the General. Use placeholder sprites until art exists.
- A boss health bar at the top. Values are tunable fields in the Inspector.
- Unlike the stages, the player can take 3 hits here, shown as medals top-left. After a hit the player blinks and can't be hurt for 1.6 s. The 3rd hit is game over.
- Only the boss's hurtboxes hurt: trigger boxes on the hull and the face, smaller than the art. The player passes through the boss's solid colliders, which are only for bullets. Standing under the chin is safe, except at the moment it lands.

**Phase 1 - "Roaring tank" (100% to 50%)**
- Pounce: a 0.9 s crouch telegraph, then a jump landing on the player's position. The landing crushes everything under it; dodge by running under the jump. No shockwaves yet.
- Mane missile spread: 3 projectiles in a fan after a 0.7 s mane flash. Crouch under them at mid range; up close they come down too steeply.
- Pick attacks at random, never the same one more than twice in a row.

**Phase 2 - "The mane is on fire" (50% to 0%)**
- At 50% health: a brief screen shake, armor pieces fly off, sparks, and the Phase 2 overlay line.
- About 15% faster with shorter cooldowns.
- New attack: a charge across the arena with a 1.2 s flashing telegraph.
- Landings now send shockwaves both ways, 0.4 s after impact: one from the back feet, one from past the snout. Jump them.
- Keeps the pounce and the missile spread (5 projectiles).

**Defeat:** explosion effect, a 1 s freeze, the defeat line, then load `Cutscene_HelicopterEscape`.

**Implementation notes**
- `BossController` with a state machine: `Intro`, `Phase1`, `Phase2`, `Dead`.
- `BossHealth` firing `OnPhaseChange` and `OnDeath` events used by the UI and the overlay system.
- Reuse the existing bullet and damage conventions so player bullets hurt the boss with minimal changes.
- Every attack must be telegraphed (fair and readable).

## 11. Helicopter Chase Spec - `Stage_HelicopterChase`

- A side-scrolling auto-scrolling shooter with parallax layers (sky, distant trees, nearer trees).
- The player controls the helicopter: up, down, left, right within the screen, with a machine gun on a held fire button.
- The General's helicopter stays on the right, moving on a sine-wave path, with a health bar at the top.
- Attacks (alternating): a 3-rocket volley aimed at the player, and 2 to 3 lion-chip drones to shoot down.
- Duration 60 to 90 seconds. Tunable values. Player health plus a game over screen with restart.
- At 0 health: slow motion for about 1 s, the helicopter spins out of the screen, then load `Cutscene_Ending`.
- The overlay lines in 9.4 trigger at the listed health thresholds and events.

## 12. Build Order for Claude Code

Do these in order, keep the project compiling and playable after each step, and confirm each step with the user before moving on.

1. **Setup:** Create folders (`Assets/Dialogue`, `Assets/Cutscenes`, `Assets/BossFight`, `Assets/HelicopterStage`), a `SceneFlow` manager with the scene order from section 5, and add all scenes to Build Settings.
2. **Dialogue system:** `DialogueLine`, `DialogueSequence`, the `DialogueScene` prefab, typewriter and inline commands, speaker blips, portrait bob, radio effect. Create the data for section 8 with placeholder art.
3. **Overlay system:** the `DialogueOverlay` with queueing, plus trigger components that fire lines from section 9 at scene start, on events, or on health thresholds.
4. **Lion collar release effect** in Stage 1: shatter VFX, chime, the lion runs off screen instead of a normal death.
5. **Video cutscene player** with skip, using placeholders for the 5 video cutscenes (intro, after Stage 1, after Stage 2, helicopter escape, ending).
6. **Mecha-Lion boss** per section 10 using the overlay lines in 9.3.
7. **Helicopter chase** per section 11 using the overlay lines in 9.4.
8. **Epilogue and Credits** scenes, then a full flow test from `Stage 1` to the credits (add a debug key to skip stages).
9. **Polish if time allows:** screen shake, extra sound effects, real portraits and backgrounds, reply choices (6.3).

## 13. Notes

- Video cutscenes (5) use placeholders until real clips exist. Each video scene should be reusable from one `VideoCutscene` scene or prefab configured with a clip and a next scene name.
- If a line is too long on screen, shorten it or split it, but keep the speaker's voice (see the cast table).
