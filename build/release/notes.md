# Friday Night Funkin' — C# Port, version 0.10 (prerelease)

This is the first prerelease of the C# port of Friday Night Funkin', built on **.NET 8 + MonoGame**
(the C# analogue of HaxeFlixel). The original Haxe source is untouched in `source/` and remains the
reference implementation; the port lives in `src/` and `tests/`.

## What's in this prerelease

**Two-layer architecture**

* `Funkin.Core` — engine-independent ports of the game's logic:
  * `Conductor` — BPM and time-signature changes, step/beat/measure signals, time↔step conversion,
    instrumental/format/audio-visual offsets, `forceBPM`.
  * `PlaySession` — the note simulation: spawning, 160 ms hit windows, ghost tapping, botplay,
    sustains and hold drops, mines, custom note kinds, health, combo, score and tallies.
  * `Scoring` — PBOT1 (sigmoid), Week 7 and Legacy windows, ranks and completion.
  * Chart format — metadata/chart JSON, `t/d/l/k/p` notes, `t/e/v` events, scroll speeds, `ms`/`float`/`ticks`.
  * Data registries — characters, stages, weeks, albums, note kinds, dialogues, all with mod overlays.
  * Save data — scores, tallies, week completion, options; atomic write with a backup copy.
  * Mods — folder and zip mods, `_polymod_meta.json`, Polymod-compatible override order.
* `Funkin.Game` — the MonoGame front-end:
  * A compact Flixel layer: `FlxG`, `FlxState` (+ substates), `FlxSprite`, animation controller,
    Sparrow atlas parsing, `FlxCamera` (scroll/zoom/shake/follow), `FlxSound`, `FlxText`.
  * OGG streaming through NVorbis into a streaming audio instance, with sample-accurate playback
    position, seeking, looping and vocal-track drift correction.
  * States: Init, Title, MainMenu, Story, Freeplay, Options, Credits, pre-song Conversation, Play,
    Pause, Game Over, Results.
  * Gameplay view: stage props with parallax, dancing/singing characters, strumlines, hold notes,
    note splashes, judgement popups, health bar with icons, score/accuracy HUD, camera zoom events.

**Verification**

* 43 xUnit tests covering timing, judging, scoring, chart parsing, notes and holds, save round-trips,
  note kinds and mod asset overriding.
* A headless smoke test (`scripts/csharp/smoke-test.sh`) that plays a complete song with Botplay —
  data loading → dialogue → countdown → notes → scoring → results → saving — and runs in CI.

## Downloads

Framework-dependent builds; you need the **.NET 8 runtime** and the game's assets checked out into an
`assets/` folder next to the executable ([FunkinCrew/Funkin.assets](https://github.com/FunkinCrew/Funkin.assets)).
Without the assets the game still runs, using coloured placeholders for art and silence for audio.

* `funkin-csharp-0.10.0-linux-x64.zip`
* `funkin-csharp-0.10.0-win-x64.zip`

```bash
./Funkin                     # or Funkin.exe on Windows
./Funkin --root=/path/to/funkin   # explicit game root containing assets/
./Funkin --smoke-song=tutorial    # automated playthrough with Botplay
```

## Known gaps (see `docs/PORTING.md` for the full matrix)

This is a prerelease: the gameplay loop, its data and its scoring are ported, while several large
subsystems are not yet:

* The chart editor and stage editor.
* HScript mod scripting (Polymod scripts, scripted characters/states).
* HaxeUI option widgets, mobile UI, FlxAnimate (Adobe Animate atlas) rendering, multi-atlas characters.
* Video playback, Newgrounds API, Discord RPC, achievements, the asset preloader, native extensions.
* Shaders, note-splash spritesheet animations and rank/album artwork — visually simplified for now.

## Controls

Arrows/WASD = notes · Enter/Space = accept/pause · Escape = back · B = botplay · Ctrl+R = restart · F11 = fullscreen.
Gamepad: D-pad/face buttons for notes, Start to pause.
