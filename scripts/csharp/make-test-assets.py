#!/usr/bin/env python3
"""Generates a tiny asset tree so the C# port can be smoke-tested without the art submodule.

Usage: python3 scripts/csharp/make-test-assets.py build/test-assets
"""

import json
import os
import sys

root = sys.argv[1] if len(sys.argv) > 1 else "build/test-assets"
data = os.path.join(root, "assets", "data")

os.makedirs(os.path.join(data, "songs", "test"), exist_ok=True)
os.makedirs(os.path.join(data, "characters"), exist_ok=True)
os.makedirs(os.path.join(data, "stages"), exist_ok=True)

metadata = {
    "version": "2.2.3",
    "songName": "Test Song",
    "artist": "Smoke Test",
    "charter": "CI",
    "timeChanges": [{"t": 0, "bpm": 120, "bt": [4, 4, 4, 4]}],
    "playData": {
        "songVariations": ["default"],
        "difficulties": ["easy", "normal", "hard"],
        "characters": {"player": "bf", "girlfriend": "gf", "opponent": "dad"},
        "stage": "mainStage",
        "noteStyle": "funkin",
        "ratings": {"easy": 1, "normal": 3, "hard": 5},
    },
}

chart = {
    "version": "2.2.3",
    "scrollSpeed": {"easy": 0.9, "normal": 1.4, "hard": 2.0},
    "events": [{"t": 0, "e": "Zoom Camera", "v": {"zoom": 1.0, "duration": 1}}],
    "notes": {
        "normal": [
            {"t": 0, "d": 0},
            {"t": 500, "d": 1},
            {"t": 1000, "d": 2, "l": 250},
            {"t": 1500, "d": 3},
            {"t": 1500, "d": 5},
            {"t": 2000, "d": 0, "l": 500},
            {"t": 2500, "d": 2},
        ],
        "easy": [{"t": 0, "d": 0}, {"t": 1000, "d": 2}],
        "hard": [{"t": 0, "d": 0}, {"t": 250, "d": 1}, {"t": 500, "d": 2}, {"t": 750, "d": 3}],
    },
}

dialogue = {
    "song": "test",
    "dialogue": [
        "dad: So you think you can keep up?",
        {"speaker": "bf", "text": "Beep bop!"},
    ],
}

characters = {
    "bf": {
        "name": "Boyfriend",
        "assetPath": "characters/BOYFRIEND",
        "scale": 1,
        "animations": [{"name": "idle", "prefix": "BF idle dance"}],
    },
    "dad": {
        "name": "Daddy Dearest",
        "assetPath": "characters/DADDY_DEAREST",
        "scale": 1,
        "animations": [{"name": "idle", "prefix": "DADDY idle"}],
    },
    "gf": {
        "name": "Girlfriend",
        "assetPath": "characters/GF_assets",
        "scale": 1,
        "animations": [{"name": "idle", "prefix": "GF Dancing Beat"}],
    },
}

stage = {
    "version": "1.0.3",
    "name": "Main Stage",
    "cameraZoom": 1.05,
    "characters": {
        "bf": {"position": [770, 450], "cameraOffsets": [-100, -100]},
        "dad": {"position": [100, 100], "cameraOffsets": [100, -100]},
        "gf": {"position": [400, 130], "cameraOffsets": [0, 0]},
    },
    "props": [],
}

week = {
    "version": "1.0.1",
    "name": "Test Week",
    "titleAsset": "",
    "songs": ["test"],
    "background": "#F9CF51",
}


def write(path, payload):
    with open(os.path.join(root, "assets", "data", path), "w", encoding="utf-8") as handle:
        json.dump(payload, handle, indent=2)
        handle.write("\n")


write("songs/test/test-metadata.json", metadata)
write("songs/test/test-chart.json", chart)
write("songs/test/dialogue.json", dialogue)
for char_id, char in characters.items():
    write(f"characters/{char_id}.json", char)
write("stages/mainStage.json", stage)
os.makedirs(os.path.join(data, "weeks"), exist_ok=True)
write("weeks/testweek.json", week)

print(f"Wrote test assets to {os.path.abspath(root)}")
