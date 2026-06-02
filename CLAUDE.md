# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NeuroFlex is a VR cognitive rehabilitation application built in Unity 2022.3.28f1 targeting Meta Quest (Oculus) headsets. It presents patients with physical-cognitive dual-task exercises: players physically stand up from a seated position while simultaneously solving math problems (addition/subtraction) by grabbing numbered objects in VR environments.

## Unity & Build

- **Unity version:** 2022.3.28f1
- **Target platform:** Android (Meta Quest via `com.meta.xr.sdk.all` 68.0.1 and `com.unity.xr.oculus` 4.5.1)
- Build and run through the Unity Editor. No CLI build commands are configured.
- Deploy to headset via Android Build Settings or Meta Quest Link.

## Scene Flow

```
Start → MiniMental → SelectEnvironment → City | NatureScene → FinalScene
```

- `Start`: initial screen, shows welcome subtitles/tutorial via `SubtitleManager`
- `MiniMental`: cognitive screening scene
- `SelectEnvironment`: patient selects City or Nature environment; sets `TipoEscena` in `APIManager.data`
- `City` / `NatureScene`: main gameplay — patient completes zones with number-grabbing + math questions
- `FinalScene`: shows results and session ID after data POSTs to AWS API

Scene transitions are managed by `SceneController.LoadScene(sceneName)`, which fades out via `FadeOVR` before loading.

## Key Architecture

### The "Utils" GameObject
All persistent managers attach to a single GameObject named **"Utils"** that carries:
- `Spawner` — activates/destroys spawn points in zones
- `ScoreManager` — tracks `scoreNumbers` (objects grabbed) and `scoreQuestions` (correct answers)
- `APIManager` — holds `ExtractData` struct and POSTs it to AWS API Gateway
- `ExtractDataCollector` — gathers metrics from other components and populates `APIManager.data`
- `GameTimer` — elapsed time display
- `Texts` — localization wrapper (Spanish, `es` locale forced on start)

`Spawner` and `AudioManager` use `DontDestroyOnLoad`.

### Gameplay Loop (City/Nature scenes)
1. Player walks into a **Zone** trigger → `Zone.OnTriggerEnter` calls `spawner.activeSpawn(zone, numbers[])`, placing 3 numbered objects at randomized spawn points (tagged `Spawn_1`, `Spawn_2`, `Spawn_3`)
2. Player physically grabs number objects → `ScoreAdder.AddScore()` increments `scoreNumbers`, records capture time and precision (inner/outer collider distance)
3. Player exits Zone trigger → `Zone.OnTriggerExit` calls `activeQuest()`, showing a math question via `QuestionManager`
4. `QuestionManager.isCorrectAlternative()` checks answer, records response time, then re-enables player movement
5. At **EndZone**: aggregates all metrics, POSTs to API, displays session ID

### Stand-Up Detection (`Inputs.cs`)
The physical rehabilitation mechanic: after a number spawns at height > 1.35m (`spawnUp = true`), `Inputs.Update()` starts a 13-second timer. If `headPlayer.transform.position.y > 1.35f` before timeout, `responseTimePlayerUp` is recorded. This measures sit-to-stand response time.

### Data Collection (`APIManager.ExtractData`)
Metrics sent to `https://f13h4cz6id.execute-api.sa-east-1.amazonaws.com/data`:
- `TiempoRespuestaPararse` — sit-to-stand time
- `Precision` — average grab precision (%)
- `TiempoActivoTarea` — total active task time
- `CantAciertasTotales` — correct question answers
- `ObjetosInteractuadosCorrectamente` — numbers grabbed
- `TiempoRespuestaPregunta1/2/3` — per-question response times
- `TiempoCapturarNumero` — average time to grab a number
- `TiempoTutorial` — tutorial duration
- `TipoEscena` — 1=Nature, 2=City

`ExtractDataCollector.SendDataToAPI()` retries up to 3 times with 2s delay. On success, parses `session_id` from response and fires `OnSessionIdReceived` event (displayed in `EndZone`).

### Localization
Uses Unity Localization package. Tables: `AdditionQuest`, `SubtractionQuest`, `Welcome`, `Tutorial`. Locale is hardcoded to `"es"` (Spanish) in `Texts.Start()`. Table assets are in `Assets/Settings/`.

### OVR Input
Primary button: `OVRInput.Button.One` (A button) advances subtitles and starts movement. Hand triggers grab number objects. `Input.GetKeyDown(KeyCode.Space)` is the keyboard fallback for editor testing.

## Important Tags
- `Player` — the XR rig
- `Spawn_1`, `Spawn_2`, `Spawn_3` — spawn point children within Zone GameObjects
- `LeftHand`, `RightHand` — hand controllers (used by `ScoreAdder` precision calculation)
- `MainCamera` — cameras that receive `FadeOVR` component

## Third-Party Assets
- `Assets/SyntyStudios/` — Polygon City environment
- `Assets/NatureStarterKit2/` — nature environment
- `Assets/Krivodeling/` — additional assets
- `Assets/Oculus/` — Meta XR SDK utilities
