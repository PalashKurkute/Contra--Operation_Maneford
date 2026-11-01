# Operation Maneford

A pixel-art, Contra-style run-and-gun made in Unity. You play Rook, shooting the mind-control collars off an army of lions to take down General Maneford. It has three levels, a Mecha-Lion boss fight, a helicopter chase, cutscenes and dialogue.

Inspired by Contra (Konami, 1987). A fan tribute, not affiliated with Konami.

## Open the project

You need:

- **Unity 6000.5.5f1**, installed through Unity Hub. Use this exact version, because a different one will upgrade the project files.
- **Git**, available on your PATH. Unity needs it to download one package from a Git URL (`com.coplaydev.unity-mcp`).
- **A short folder path**, such as `C:\Coding\Contra`. Deep paths hit the Windows 260-character limit and make package imports fail.

Then:

1. Clone the repo:
   ```
   git clone git@github.com:PalashKurkute/DevJournal-.git
   ```
2. In Unity Hub, choose **Add > Add project from disk** and select the cloned folder (the one containing `Assets` and `ProjectSettings`).
3. Open it. The first import rebuilds the `Library` folder and downloads packages, so it takes several minutes.
4. If Unity asks you to install TextMesh Pro essentials, you can skip it. They are already in `Assets/TextMesh Pro`.

## Play it

Open `Assets/Scenes/MainMenu.unity` and press Play. Starting from another scene works for testing that level, but the full story only runs from the main menu.

All 18 scenes are already listed in **File > Build Profiles**, in play order, and the project settings (URP, input, tags and layers) are in the repo. There is nothing to set up by hand.

## Controls

| Action | Keys |
| --- | --- |
| Move | A / D or arrow keys |
| Aim (8 directions) | W / S |
| Jump | Space |
| Drop through a platform | Down + Space |
| Fire | J |
| Dash | Shift |
| Duck under shots | S while standing still |
| Pause | Esc |
| Skip a cutscene or dialogue | Hold Enter |
| Helicopter | WASD or arrows to fly, hold J to fire |

## Where things are

| Path | Contents |
| --- | --- |
| `Assets/Scenes` | Levels, the menu and credits; story scenes are in `Story/` |
| `Assets/Scripts` | Game code |
| `Assets/Cutscenes` | Cutscene scenes, art, audio and the final videos in `Video/` |
| `Assets/Dialogue` | Dialogue data and portraits |
| `Tools` | Optional Python and PowerShell scripts that generated some art, audio and voice. You don't need them to run the game |
| `STORY_AND_GAME_STRUCTURE.md` | Story, dialogue and stage design |

## Working together

- Commit the `.meta` file along with any asset you add, move or rename.
- Scene and prefab files don't merge well, so agree who is editing a scene before you both change it.
- Unity rewrites `Assets/Fonts/PressStart2P-Regular SDF.asset` and `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` when it imports or builds. Unless you changed them on purpose, discard those edits with `git restore` rather than committing them.
- `Library`, `Temp`, `Logs`, `UserSettings` and `Builds` are generated or local and stay out of Git.
