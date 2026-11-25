# Park Master 3D (Unity)

A 3D hyper-casual puzzle game built with **Unity 6 (6000.3.21f1)** and **URP**, inspired by *Park Master*. Players draw real-time paths for color-coded vehicles to navigate obstacles, collect coins, and park safely in matching slots without colliding.

---

## Showcase

| 1. In-Game Gameplay Demo | 2. Unity Editor & Scene Overview |
| :---: | :---: |
| Real-time path drawing, vehicle movement, coin pickups, and victory flow. | Scene hierarchy, `Resources/Levels` prefabs, Inspector setup, and scripts. |
| [![Gameplay Demo](https://img.youtube.com/vi/QSecaZ4t8sw/hqdefault.jpg)](https://www.youtube.com/shorts/QSecaZ4t8sw) | [![Editor Demo](https://img.youtube.com/vi/jtv_oFVWSqc/hqdefault.jpg)](https://www.youtube.com/watch?v=jtv_oFVWSqc) |
| [Watch on YouTube](https://www.youtube.com/shorts/QSecaZ4t8sw) | [Watch on YouTube](https://www.youtube.com/watch?v=jtv_oFVWSqc) |

Local video files are also available in [Docs/gameplay.mp4](Docs/gameplay.mp4) and [Docs/unity-editor-scene-demo.mp4](Docs/unity-editor-scene-demo.mp4).

---

## Core Features

* **Real-Time Path Drawing (`LineDrawer`):** Supports both Mouse and Touch via Unity's **New Input System**, projecting screen input onto the road plane with configurable edge padding, road-bounds clamping, and target snapping to matching parking slots.
* **Modular Vehicle Architecture:** Each vehicle (`CarController`) is decoupled into single-responsibility components:
  * `CarMovement` — waypoint traversal and speed control
  * `CarPathHandle` — `LineRenderer` management and path distance tracking
  * `CarVisual` — color palette synchronization across materials and UI
  * `CarAnimation` — DOTween-powered idle, drive, and return-to-start animations
  * `CarCollision` — crash detection and failure triggers
* **Economy & Progression:**
  * **5 Playable Levels** loaded dynamically from `Resources/Levels` with star ratings and best path-length tracking.
  * **Car Shop (`CarShopManager` & `CarDatabase`):** `ScriptableObject`-driven vehicle catalog with unlock/selection persistence.
  * **Coin & Key Rewards (`CoinManager`):** Animated UI coin pickups, chest/key progression, and `ISaveService`-backed persistence (`SaveSystem`).
* **Object Pooling (`ObjectPool`):** Reusable pooling with prewarming and automatic `ParticleSystem` reset for coin pickups and victory VFX.
* **Custom Unity Editor Tools (`Assets/Scripts/Editor`):**
  * `PlayerPrefsViewerWindow` (`Tools/Park Master/PlayerPrefs Viewer`) for inspecting and resetting progression data in-editor.
  * Automated FX & UI setup generators (`CreateCoinPickupFX`, `CreateWinEffectPrefab`, `SetupParkMasterUI`).

---

## Technical Highlights & Architecture

* **Zero-GC Input & Raycasting:** Pre-allocated `RaycastHit[]` and `List<RaycastResult>` caches in `LineDrawer` avoid per-frame heap allocations during continuous drag input.
* **Assembly Definitions (`.asmdef`):** Codebase is separated into `ParkMaster.Runtime`, `ParkMaster.Editor`, and `ParkMaster.Tests.EditMode` for faster incremental compilation and clean dependency boundaries.
* **Decoupled Persistence (`ISaveService`):** Centralized `SaveKeys` and `SaveSystem` (`ISaveService` + `PlayerPrefsSaveService`) replace scattered `PlayerPrefs` calls and enable deterministic unit testing.
* **EditMode Unit Tests:** NUnit tests in `Assets/Tests/EditMode` covering `SaveSystem`, `CarDatabase`, `ColorPalette`, and `PathMathUtility`.

---

## Project Structure

```text
Assets/
├── Plugins/Demigiant/DOTween/   # DOTween animation library
├── Prefabs/                     # Cars, Effects, Level, ParkingSlot & StartPoint prefabs
├── Resources/
│   ├── CarDatabase.asset        # ScriptableObject vehicle catalog
│   └── Levels/                  # Level_01 .. Level_05 prefabs
├── Scenes/                      # GamePlay.unity
├── Scripts/
│   ├── Car/                     # CarController, CarMovement, CarVisual, CarAnimation, ...
│   ├── Coin/                    # CoinItem, CoinSpinner
│   ├── Data/                    # SaveSystem (ISaveService, PlayerPrefsSaveService, SaveKeys)
│   ├── Drawing/                 # LineDrawer, PathMathUtility, StartPointRing
│   ├── Editor/                  # Custom Editor Windows & FX/UI generators
│   ├── Managers/                # GameManager, LevelManager, CoinManager, CarShopManager, ObjectPool
│   └── UI/                      # LevelSelectUI, LevelScrollManager, CarMenuUI, LevelCompleteUI
└── Tests/
    └── EditMode/                # NUnit EditMode unit tests
```

---

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/hienvukhac/Park-Master.git
   ```
2. Open the project in **Unity 6000.3.21f1** (Universal Render Pipeline).
3. Open `Assets/Scenes/GamePlay.unity` and press **Play**.
4. *(Optional)* Run EditMode tests via **Window → General → Test Runner → EditMode → Run All**.

---

## Controls

* **Mouse / Touch Drag:** Draw a path from any vehicle (or its start ring) to a matching-color parking slot.
* **Release:** Vehicles drive along their drawn paths automatically.
