# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Fault Line** (Arabic title "صدع") — a portrait Android hex-sliding picture-restoration puzzle game, Unity **6000.6.0f1**. It is a port of an existing **web build**: the puzzle engine, text, save format and sounds were carried over 1:1 from it, and challenge links / daily puzzles must stay compatible with the web game (`Config.ChallengeSite`). Arabic is the default language (RTL); English is secondary.

All game code lives in `Assets/FaultLine/`. `Assets/MaxSdk`, `Assets/ExternalDependencyManager`, `Assets/PlayServicesResolver` are third-party (AppLovin MAX + Google resolver) — don't edit them.

## Build / setup

No CLI build scripts, tests, or asmdefs. Everything runs from the Editor menu **Fault Line ▸** (`Assets/FaultLine/Editor/FaultLineSetup.cs`):

- **1 - Set up project for Android** — sets identity/bundle ID, IL2CPP, ARM64+ARMv7, min API 24, portrait, input handling "Both", texture/audio import settings for `Resources/*` folders, creates `Assets/FaultLine/Main.unity` (empty) and adds it to build settings, and toggles the `FAULTLINE_MAX` define depending on whether the MaxSdk assembly is present. Re-run it after importing/removing AppLovin MAX.
- **2 - Build test APK** → `Builds/FaultLine.apk`; **3 - Build Google Play bundle** → `Builds/FaultLine.aab` (requires a custom keystore).
- **Bump version** — increments `bundleVersionCode` and patch version; run before each Play upload.

Manual step required for Arabic glyph joining: Project Settings ▸ UI Toolkit ▸ *Enable Advanced Text Generator* (`TextSetup.cs` also tries to set it via reflection, with `Resources/ui.uss` as fallback).

Headless builds would go through `Unity.exe -batchmode -quit -projectPath . -executeMethod FaultLine.EditorTools.FaultLineSetup.BuildApk` (dialogs in that code assume interactive Editor).

## Architecture

**No scene setup, no prefabs, no UXML.** `App.Boot()` (`[RuntimeInitializeOnLoadMethod]`) spawns a `FaultLine` GameObject in any scene and builds the entire UI in code with UI Toolkit (`UIDocument` + runtime-created `PanelSettings`, reference 390×844). Do not add scene objects or inspector wiring — extend things in code.

- **`App`** (`Scripts/App.cs`) — singleton `App.I`. Owns screen switching (`Show(id)`, `GoBack()` / Android back), overlay stack (`Overlay`, `Sheet`, `Confirm`, `Toast`), the mock test-ad UI, safe-area handling, deep links/clipboard challenge detection, and its **own timer/tween system** (`Delay`, `Every`, `Anim`) driven from `Update` using realtime. Use these instead of coroutines. `Rebuild()` tears down and rebuilds all screens on language/theme change, so screens must be fully reconstructible from `Build()`.
- **Screens** derive from `Scr` (`Build`, `OnShow`, `OnHide`, `Tick`, `Back`): `menu`, `levels`, `settings` (in `LevelsScreen.cs`), `game`, `home`.
- **`U`** (`Scripts/UI/UIKit.cs`) — terse UI helper/theme library (`U.E`, `U.L`, `U.Btn`, `U.Row`, `U.Pad`, colors `U.Bg/Ink/Gold…`, fonts). `U.Row` mirrors for RTL by default. Also `Mesh2D`/`Batch` for custom-drawn meshes.
- **Core** (`Scripts/Core/`)
  - `Engine.cs` — deterministic hex-board puzzle engine, pure C# (no Unity). **Must stay bit-identical to the web build**: seeds/scrambles/move encoding feed challenge links and daily puzzles. `Move(a, v, side, k)` = axis, row value, side, shift.
  - `Content.cs` — loads `Resources/content.json` (all text `T.ar/T.en`, `LEVELS`, `ALBUMS`, `ROOMS`, `FURN`, `FACTS`, balance constants `K`, …). Text lookup via `Content.T(key)` / `Content.F(key, args)` (`{0}` placeholders). Add player-facing strings to content.json in **both** languages rather than hardcoding.
  - `Json.cs` — minimal custom JSON (`JObj`, numbers are `double`).
  - `Save.cs` — single JSON blob in PlayerPrefs key `faultline_v1`, same shape as the web build's localStorage save. Store ints as `double`. Call `Save.Persist()` after mutation.
  - `Platform.cs` — `Sfx` (pre-rendered clips in `Resources/sfx`), `Haptics` (Android vibrator via JNI), `Config` (bundle ID, MAX ad unit IDs — still placeholders, URLs), `Ads` facade (`#if FAULTLINE_MAX` real AppLovin, else in-app mock ad overlay).
- **Game** (`Scripts/Game/`) — `Game` holds one attempt's state/rules (kinds: Level, Daily, Challenge); `BoardView` is a custom `VisualElement` rendering the board, optionally with live video tiles from `StreamingAssets/live/<pic>.mp4`.
- **Meta** (`Scripts/Meta/Meta.cs`) — albums, events, streaks, stars/crowns, gold rewards, and `Challenge` link encoding/parsing (base64 JSON in `#c=` URL fragment).

Dates for dailies/streaks use Kuwait time (`Util.KwDate`, UTC+3), matching the web build.

Assets are loaded by path from `Assets/FaultLine/Resources/` (`pics/`, `icons/`, `home/`, `tex/`, `sfx/`, `fonts/`) via `Content.Tex/Pic/Icon` — keep file names in sync with keys in content.json.
