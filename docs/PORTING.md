# Porting Friday Night Funkin' to C#

This document describes the C# port that lives in `src/` and `tests/`, how it maps onto the original
HaxeFlixel codebase in `source/`, what works today, and what is still missing.

Both games live side by side in this repository:

| Path | Contents |
| --- | --- |
| `source/` | The original Haxe/HaxeFlixel game (unchanged; kept as the reference implementation). |
| `src/Funkin.Core/` | Engine-independent game logic: charts, Conductor, scoring, save data, registries. |
| `src/Funkin.Game/` | The MonoGame front-end: Flixel-style display layer, audio, states, gameplay view. |
| `tests/Funkin.Core.Tests/` | xUnit tests for the ported logic (43 tests). |

## Why this architecture

The original game is ~122,000 lines of Haxe spread over 523 files, and a large part of it is
engine plumbing (Flixel, openfl, Polymod, HScript, HaxeUI, Lime). Rather than reimplement all of
that, the port splits the game into two layers:

1. **`Funkin.Core`** – a straight port of the parts of the game that are pure logic and data. This is
   where fidelity matters most (note timing, judging, scoring, save format) and it is fully testable
   without a window, a GPU or audio hardware.
2. **`Funkin.Game`** – a compact MonoGame implementation of the Flixel API surface the game actually
   uses (`FlxG`, `FlxState`, `FlxSprite`, `FlxAnimationController`, `FlxCamera`, `FlxSound`, `FlxText`),
   plus the game's states and views rendered on top of `Funkin.Core`.

This means gameplay behaviour is verified by unit tests, while the renderer stays replaceable.

## Feature parity

Legend: ✅ ported · 🟡 partial / simplified · ❌ not ported yet

### Core systems

| Original (`source/funkin/...`) | C# (`src/Funkin.Core/...`) | Status |
| --- | --- | --- |
| `Conductor.hx` | `Play/Conductor.cs` | ✅ BPM/time-signature changes, step/beat/measure signals, time↔step conversions, offsets, `forceBPM`. |
| `play/scoring/Scoring.hx` | `Play/Scoring.cs` | ✅ PBOT1 / Week 7 / Legacy windows, sigmoid scoring, tallies, ranks. |
| `play/notes/NoteKind.hx` | `Play/Notes/NoteKind.cs` | ✅ Data-driven note kinds (`alt-animation` built in), mines, per-kind score/health modifiers. |
| `data/song/SongData.hx` | `Data/Song/*.cs` | ✅ Metadata, chart, notes (`t/d/l/k/p`), events (`t/e/v`), scroll speeds, time formats. |
| `data/character/CharacterData.hx` | `Data/Character/*.cs` | ✅ Animation data, offsets, scales, health icon and death data, registry with mod support. |
| `data/stage/StageData.hx` | `Data/Stage/*.cs` | ✅ Props (including colour props), character placements, parallax scroll factors, camera zoom. |
| `data/story/level/LevelData.hx` | `Data/Story/LevelData.cs` | ✅ Weeks, song lists, visibility, backgrounds. |
| `data/freeplay/album/AlbumData.hx` | `Data/Freeplay/AlbumData.cs` | ✅ Album metadata + registry. |
| `play/cutscene/dialogue/Conversation.hx` | `Data/Dialogue/ConversationData.cs` | ✅ JSON + legacy `speaker: text` parsing. |
| `save/Save.hx` | `Save/*.cs` | 🟡 Scores, tallies, week completion, the options used by the port, atomic writes + backup. The chart/stage editor save properties are not ported. |
| `input/Controls.hx` | `Input/Controls.cs` | 🟡 Keyboard + gamepad note binds, menu binds. Rebinding UI and `ControlsUtilities` are not ported. |
| `modding/` (Polymod) | `Modding/*.cs` | 🟡 Mod discovery, `_polymod_meta.json`, zipped mods, asset override order. HScript scripting is not ported. |
| `Paths.hx` | `Paths/FunkinPaths.cs` | ✅ `images/`, `sounds/`, `music/`, `songs/<id>/Inst.ogg`, `Voices-*.ogg`, data enumeration, mod overlay. |

### Gameplay

| Original | C# | Status |
| --- | --- | --- |
| `PlayState.hx` | `Game/States/PlayState.cs` + `Play/PlaySession.cs` | ✅ Countdown, note spawning, hit windows, ghost tapping, botplay, sustains and hold drops, health, combo, score, tallies, events, pause, game over, results, story progression. |
| `Strumline.hx` / `StrumNote.hx` | `Play/StrumNote.cs` | 🟡 Static/press/confirm states, placeholder artwork fallback. Note-splash spritesheet animation is replaced by a fading quad. |
| `BaseCharacter.hx` | `Play/CharacterSprite.cs` | 🟡 Idle/dance alternate, sing animations with `-alt` variants, `danceEvery`/`singTime`, pixel/scale/flip. Multi-atlas and Animate Atlas rendering are not ported. |
| `Stage.hx` | `Play/StageSprite.cs` | 🟡 Props, animations, beat-synced props, parallax, camera focus offsets. Stage scripts and per-prop shaders are not ported. |
| `HealthIcon` / health bar | `Play/HealthBar.cs` | 🟡 Divided bar with icons that bounce on the beat; icon switching/loss animation simplified. |
| `FunkinSound`/`VoicesGroup` | `Audio/FlxSound.cs`, `Audio/VocalsPlayer.cs` | 🟡 OGG streaming via NVorbis, seek, loop, vocal drift correction, player vocal mute on miss. Playback rate/pitch shifting is not implemented. |
| `PlayState.hx` camera logic | `PlayState` + `FlxCamera` | 🟡 Follows the singing side, zoom events, beat bobbing, screen shake. Character-specific zoom curves are simplified. |

### Menus and UI

| Original | C# | Status |
| --- | --- | --- |
| `InitState.hx` | `Game/States/InitState.cs` | ✅ Boot sequence: paths, mods, save, registries, song cache. |
| `ui/title/TitleState.hx` | `Game/States/TitleState.cs` | 🟡 Title + prompt (no animated logo cutscene). |
| `ui/mainmenu/MainMenuState.hx` | `Game/States/MainMenuState.cs` | ✅ Story / Freeplay / Options / Credits / Exit. |
| `ui/story/StoryMenuState.hx` | `Game/States/StoryMenuState.cs` | 🟡 Week list, song list panel, week progression. No capsule/score UI art. |
| `ui/freeplay/FreeplayState.hx` | `Game/States/FreeplayState.cs` | 🟡 Song list, difficulty selector, best score/rank/accuracy. No album art carousel, stickers, or DJ. |
| `ui/options/OptionsState.hx` | `Game/States/OptionsState.cs` | 🟡 Downscroll, ghost tapping, botplay, camera zoom, score/accuracy display, framerate, volume, note and A/V offsets, scroll override, save reset. |
| `ui/credits/CreditsState.hx` | `Game/States/CreditsState.cs` | 🟡 Scrolling text instead of the animated credits. |
| `play/PauseSubState.hx` | `Game/States/PauseState.cs` | ✅ Resume / restart / options / quit. |
| `play/GameOverSubState.hx` | `Game/States/GameOverState.cs` | 🟡 Retry / quit (no death animation playback). |
| `play/ResultState.hx` | `Game/States/ResultState.cs` | 🟡 Rank, score, accuracy, tallies, next-song progression. No rank art or clearing animation. |
| Dialogue cutscene | `Game/States/ConversationState.cs` | 🟡 Typewriter text, speaker names, skip. No portraits/voice beeps. |

### Not ported

These are large subsystems of the original that are **not** part of this port yet:

- The **chart editor** (`ui/debug/charting`, ~7,700 lines) and **stage editor** (`ui/debug/stageeditor`).
- **HScript modding** (Polymod scripting, `ScriptEvent` dispatch, scripted characters/states).
- **HaxeUI**-based option widgets, the mobile UI, and the FlxAnimate (Adobe Animate texture atlas) renderer.
- Video playback, Newgrounds API integration, Discord RPC, achievements, `FunkinPreloader` asset
  packing, native extensions (Android/iOS/Windows) and the `hxcpp` VM profiler.
- FlxAnimate/multi-atlas characters, shaders, and the note-splash/health-icon spritesheet animations.

Anything above that is listed as 🟡 works, but with simplified visuals.

## Running

```bash
# Prerequisites: .NET 8 SDK, and the assets submodule (git submodule update --init)
dotnet build FunkinCSharp.sln
dotnet test tests/Funkin.Core.Tests/Funkin.Core.Tests.csproj
dotnet run --project src/Funkin.Game
```

The game looks for `assets/` next to the executable, then walks up towards the repository root.
You can override that with `--root=<folder>`.

### Headless smoke test

The port can play a whole song without a human, which is what CI runs:

```bash
./scripts/csharp/smoke-test.sh
# or manually
python3 scripts/csharp/make-test-assets.py build/test-assets
dotnet run --project src/Funkin.Game -- --root="$(pwd)/build/test-assets" --smoke-song=test --smoke-timeout=90
```

The headless smoke test (`scripts/csharp/smoke-test.sh`) is what CI runs on every push; the workflow
definition lives in `docs/ci/csharp-build.yml` (copy it to `.github/workflows/` to enable it in your
own fork, since the automation token used here cannot write workflow files).

`make-test-assets.py` writes a tiny synthetic song (chart, dialogue, characters, stage, week) so the
whole pipeline — data loading, countdown, dialogue, note spawning, botplay hits, scoring, results and
saving — is exercised without needing the art repository. It also means the port degrades gracefully:
missing spritesheets become coloured placeholders instead of crashes.

## Design notes / gotchas

- **Song position** comes from `FlxSound.Time`, which is computed from submitted samples minus the
  audio still queued in `DynamicSoundEffectInstance`. With no audio file present, `PlayState` drives
  the Conductor from the frame clock so charts still play (used by the smoke test).
- **The simulation is separate from the view.** `PlaySession` owns notes, judging and scoring; the
  renderer only reads `GetNoteYOffset()` so the two can never disagree about timing.
- **Timing units** are milliseconds everywhere after chart load; the chart's `timeFormat` (`ms`,
  `float`, `ticks`) is normalised in `SongDifficulty`.
- **Asset resolution** checks the most recently loaded mod first, then the base game, matching
  Polymod's override order. Textures/atlases are cached in `TextureCache`.
- **Save file** lives in `<game root>/saves/funkin-save.json` with a `.tmp` write and a backup copy.

## Changelog of the port

### 0.10.1
- Saved settings are applied at boot: master volume, mute, target framerate and window title.
- Mods are loaded after the save file so the player's enabled-mod list is honoured.
- Note kinds drive the character's `-alt` sing animation through `animationSuffix`.
- Health icons react to who is winning (player icon highlighted above 50% health).
- `--help` / `--version` command-line switches.

### 0.10.0
- First prerelease: core logic, MonoGame front-end, menus, gameplay, results and headless smoke test.
