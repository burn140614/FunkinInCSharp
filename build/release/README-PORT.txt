Friday Night Funkin' - C# Port (MonoGame)
=======================================

Version 0.10.0 (prerelease)

Requirements
------------
* .NET 8 runtime (these builds are framework-dependent).
* The game's asset repository checked out into an `assets/` folder next to this
  binary: https://github.com/FunkinCrew/Funkin.assets
  Without it the port still starts and plays, but all art and audio are
  placeholders.

Running
-------
  Linux : ./Funkin
  Windows: Funkin.exe

Useful switches
---------------
  --root=<folder>         Use a specific game root (the folder that has assets/).
  --smoke-song=<songId>   Boot straight into a song with Botplay enabled.
  --smoke-timeout=<secs>  Abort the automated playthrough after N seconds.
  --verbose               Verbose logging.

Controls
--------
  Arrows / WASD         Notes
  Enter / Space         Accept, pause (in gameplay)
  Escape / Backspace    Back
  B                     Toggle Botplay
  Ctrl+R                Restart the song
  F11                   Fullscreen

Gamepad: D-pad or face buttons for notes, Start to pause, A/B to confirm/back.

This is a port in progress; see docs/PORTING.md in the repository for the full
feature-parity matrix.
